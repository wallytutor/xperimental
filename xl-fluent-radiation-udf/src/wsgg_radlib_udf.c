// udf.c
#include "udf.h"
#include "materials.h"

// TODO check how to use the awts here, as what is done in the sample
// below is not compatible with Bordbar (or is it?). Also notice that
// we should do some caching, as all coefficients are computed at once
// (or split the evaluation per gray-gas in the library).
//
// Also check DEFINE_EMISSIVITY_WEIGHTING_FACTOR for awts, but there
// is no mention to WSGG there...

// ---------------------------------------------------------------------------
// DEFINITIONS
// ---------------------------------------------------------------------------

// Define how many gray gases your WSGG model uses
#define NUM_GRAY_GASES 5

// UDM Slot Assignments (Indices 0 to 3)
#define UDM_GAS_0 0
#define UDM_GAS_1 1
#define UDM_GAS_2 2
#define UDM_GAS_3 3
#define UDM_GAS_4 4

// ---------------------------------------------------------------------------
// GLOBALS
// ---------------------------------------------------------------------------

static int last_evaluated_iter = -1;

// ---------------------------------------------------------------------------
// LOADING
// ---------------------------------------------------------------------------

DEFINE_EXECUTE_ON_LOADING(report_version, libname)
{
    Message("\nLoading WSGG UDF library\n");
    Message("Number of gray gases: %d\n", NUM_GRAY_GASES);

    Set_User_Memory_Name(UDM_GAS_0, "Abs. coef. of gray gas 0");
    Set_User_Memory_Name(UDM_GAS_1, "Abs. coef. of gray gas 1");
    Set_User_Memory_Name(UDM_GAS_2, "Abs. coef. of gray gas 2");
    Set_User_Memory_Name(UDM_GAS_3, "Abs. coef. of gray gas 3");
    Set_User_Memory_Name(UDM_GAS_4, "Abs. coef. of gray gas 4");
}

// ---------------------------------------------------------------------------
// MAIN MODEL CALL
// ---------------------------------------------------------------------------

DEFINE_ADJUST(evaluate_wsgg_model, domain)
{
    int current_iter;

    // The host doesn't do cell calculations, only nodes do.
    #if !RP_HOST
    // Safely retrieve current iteration to prevent accidental double-execution.
    current_iter = N_ITER;

    if (current_iter != last_evaluated_iter)
    {
        Thread *t;
        cell_t c;

        /* Loop through all cell threads in the domain */
        thread_loop_c(t, domain)
        {
            /* Only process threads that actually hold fluid/solid cells */
            if (FLUID_THREAD_P(t))
            {
                begin_c_loop(c, t)
                {
                    // Allocate memory for model coefficients
                    real kabs[5]  = {0.0, 0.0, 0.0, 0.0, 0.0};
                    real awts[5]  = {0.0, 0.0, 0.0, 0.0, 0.0};

                    // Retrieve local field variables needed for WSGG
                    Material *m = THREAD_MATERIAL(t);
                    int ico2 = mixture_specie_index(m, "co2");
                    int ih2o = mixture_specie_index(m, "h2o");
                    real rho = C_R(c, t);
                    real T = C_T(c, t);
                    real p = C_P(c, t);

                    // Mixture molecular weight from ideal gas law
                    // TODO check what units fluent is actually using here
                    // for setting the correct gas constant...
                    real mw_mix = rho * T * 8.314462 / (p + 1e-20);

                    // Mole fractions from mass fractions
                    // XXX hardcoded molecular weights, consider getting
                    // the same values from mixture SCM file!
                    real x_h2o = C_YI(c, t, ih2o) * mw_mix / 18.015;
                    real x_co2 = C_YI(c, t, ico2) * mw_mix / 44.01;

                    // Call the WSGG coefficient library
                    wsgg_coefs(T, p, x_h2o, x_co2, 0.0, kabs, awts);

                    // Cache the results directly inside the cell's UDM slots
                    C_UDMI(c, t, UDM_GAS_0) = kabs[0];
                    C_UDMI(c, t, UDM_GAS_1) = kabs[1];
                    C_UDMI(c, t, UDM_GAS_2) = kabs[2];
                    C_UDMI(c, t, UDM_GAS_3) = kabs[3];
                    C_UDMI(c, t, UDM_GAS_4) = kabs[4];
                }
                end_c_loop(c, t)
            }
        }

        // Mark this iteration as completed
        last_evaluated_iter = current_iter;
        // Message("\n[UDF] WSGG coefficients cached for iteration %d.\n", current_iter);
    }
    #endif
}

// ---------------------------------------------------------------------------
// WSGGM ABSORPTION COEFFICIENTS FUNCTION
// ---------------------------------------------------------------------------

DEFINE_WSGGM_ABS_COEFF(user_wsggm_abs_coeff, c, t, xi, p_t, s, soot_conc,
    Tcell, nb, ab_wsggm, ab_soot)
{
    Material *m = THREAD_MATERIAL(t);
    int ico2 = mixture_specie_index(m, "co2");
    int ih2o = mixture_specie_index(m, "h2o");

    // Partial pressures
    real p_x = p_t * (xi[ico2] + xi[ih2o]);

    switch (nb)
    {
        case 0:
            *ab_wsggm = C_UDMI(c, t, UDM_GAS_0) * p_x;
            break;

        case 1:
            *ab_wsggm = C_UDMI(c, t, UDM_GAS_1) * p_x;
            break;

        case 2:
            *ab_wsggm = C_UDMI(c, t, UDM_GAS_2) * p_x;
            break;

        case 3:
            *ab_wsggm = C_UDMI(c, t, UDM_GAS_3) * p_x;
            break;

        case 4:
            *ab_wsggm = C_UDMI(c, t, UDM_GAS_4) * p_x;
            break;
    }

    // Soot absorption (set to zero for now)
    *ab_soot =  0.0;
}

// ---------------------------------------------------------------------------
// EMISSIVITY WEIGHTING FACTORS
// ---------------------------------------------------------------------------

// DEFINE_EMISSIVITY_WEIGHTING_FACTOR(user_wsggm_emiss_weighting, c, t, xi, p_t, s, soot_conc,
//     Tcell, nb, ab_wsggm, ab_soot, awts)
// {
//     // We take our pre-calculated weights from the .udf library
//     *awts = C_UDMI(c, t, UDM_GAS_0 + nb);
// }

// ---------------------------------------------------------------------------
// EOF
// ---------------------------------------------------------------------------