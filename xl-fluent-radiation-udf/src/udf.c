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

// void wsgg_coefs(
//     double T,
//     double P,
//     double x_h2o,
//     double x_co2,
//     double fvsoot,
//     double *kabs,
//     double *awts
//     );


// ---------------------------------------------------------------------------
// DEFINITIONS
// ---------------------------------------------------------------------------

// Define how many gray gases your WSGG model uses
#define NUM_GRAY_GASES 4

// UDM Slot Assignments (Indices 0 to 3)
#define UDM_GAS_0 0
#define UDM_GAS_1 1
#define UDM_GAS_2 2
#define UDM_GAS_3 3

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
                    // 1. Retrieve local field variables needed for WSGG
                    real T = C_T(c, t); // Cell Temperature
                    real p = C_P(c, t); // Cell Pressure if needed

                    // 2. Placeholders for your heavy simultaneous math output
                    real kappa_0 = 0.0;
                    real kappa_1 = 0.0;
                    real kappa_2 = 0.0;
                    real kappa_3 = 0.0;

                    // [INSERT YOUR HEAVY WSGG MATHEMATICS HERE]
                    // Evaluate polynomials using T, species concentrations, etc.
                    // mapping directly to kappa_0, kappa_1, etc.

                    // Example dummy math scaling with local temperature
                    kappa_0 = 0.01 * T;
                    kappa_1 = 0.02 * T;
                    kappa_2 = 0.05 * T;
                    kappa_3 = 0.12 * T;

                    // 3. Cache the results directly inside the cell's UDM slots
                    C_UDMI(c, t, UDM_GAS_0) = kappa_0;
                    C_UDMI(c, t, UDM_GAS_1) = kappa_1;
                    C_UDMI(c, t, UDM_GAS_2) = kappa_2;
                    C_UDMI(c, t, UDM_GAS_3) = kappa_3;
                }
                end_c_loop(c, t)
            }
        }
        // Mark this iteration as completed
        last_evaluated_iter = current_iter;
        Message("\n[UDF] WSGG coefficients cached for iteration %d.\n", current_iter);
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
    real CO2_molf, H2O_molf;
    real k0, k1, k2, k3;

    CO2_molf = xi[ico2];
    H2O_molf = xi[ih2o];

    switch (nb)
    {
        case 0:
        {
            k0 = C_UDMI(c, t, UDM_GAS_0);
            *ab_wsggm = k0 * (H2O_molf + CO2_molf) * p_t;
        }
        break;

        case 1:
        {
            k1 = C_UDMI(c, t, UDM_GAS_1);
            *ab_wsggm = k1 * (H2O_molf + CO2_molf) * p_t;
        }
        break;

        case 2:
        {
            k2 = C_UDMI(c, t, UDM_GAS_2);
            *ab_wsggm = k2 * (H2O_molf + CO2_molf) * p_t;
        }
        break;

        case 3:
        {
            k3 = C_UDMI(c, t, UDM_GAS_3);
            *ab_wsggm = k3 * (H2O_molf + CO2_molf) * p_t;
        }
    }

    *ab_soot =  0.1;
}

// ---------------------------------------------------------------------------
// EOF
// ---------------------------------------------------------------------------