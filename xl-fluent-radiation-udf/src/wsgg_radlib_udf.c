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
#define UDM_KABS 0
#define UDM_AWTS NUM_GRAY_GASES
#define UDM_MW   NUM_GRAY_GASES + UDM_AWTS

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

    Set_User_Memory_Name(UDM_KABS + 0, "Abs. coef. of gray gas 0");
    Set_User_Memory_Name(UDM_KABS + 1, "Abs. coef. of gray gas 1");
    Set_User_Memory_Name(UDM_KABS + 2, "Abs. coef. of gray gas 2");
    Set_User_Memory_Name(UDM_KABS + 3, "Abs. coef. of gray gas 3");
    Set_User_Memory_Name(UDM_KABS + 4, "Abs. coef. of gray gas 4");
    Set_User_Memory_Name(UDM_AWTS + 0, "Weight of gray gas 0");
    Set_User_Memory_Name(UDM_AWTS + 1, "Weight of gray gas 1");
    Set_User_Memory_Name(UDM_AWTS + 2, "Weight of gray gas 2");
    Set_User_Memory_Name(UDM_AWTS + 3, "Weight of gray gas 3");
    Set_User_Memory_Name(UDM_AWTS + 4, "Weight of gray gas 4");
    Set_User_Memory_Name(UDM_MW,       "Mixture molecular weight");
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
                    // - rho is in kg/m^3
                    // - p is in Pascal (relative!!!!)
                    // - T is in K
                    // - mw_mix will be in kg/mol
                    real p_ref = 101325.0;
                    real mw_mix = rho * T * 8.314462 / (p + p_ref);

                    // Mole fractions from mass fractions
                    // XXX hardcoded molecular weights, consider getting
                    // the same values from mixture SCM file!
                    real x_h2o = C_YI(c, t, ih2o) * mw_mix / 0.018015;
                    real x_co2 = C_YI(c, t, ico2) * mw_mix / 0.044010;

                    // Call the WSGG coefficient library
                    wsgg_coefs(T, p, x_h2o, x_co2, 0.0, kabs, awts);

                    // Cache the results directly inside the cell's UDM slots
                    C_UDMI(c, t, UDM_KABS + 0) = kabs[0];
                    C_UDMI(c, t, UDM_KABS + 1) = kabs[1];
                    C_UDMI(c, t, UDM_KABS + 2) = kabs[2];
                    C_UDMI(c, t, UDM_KABS + 3) = kabs[3];
                    C_UDMI(c, t, UDM_KABS + 4) = kabs[4];

                    C_UDMI(c, t, UDM_AWTS + 0) = awts[0];
                    C_UDMI(c, t, UDM_AWTS + 1) = awts[1];
                    C_UDMI(c, t, UDM_AWTS + 2) = awts[2];
                    C_UDMI(c, t, UDM_AWTS + 3) = awts[3];
                    C_UDMI(c, t, UDM_AWTS + 4) = awts[4];

                    // Cache mixture molecular weight
                    C_UDMI(c, t, UDM_MW) = mw_mix;
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

DEFINE_WSGGM_ABS_COEFF(
    user_wsggm_abs_coeff,
    c,
    t,
    xi,
    p_t,
    s,
    soot_conc,
    Tcell,
    nb,
    ab_wsggm,
    ab_soot
)
{
    Material *m = THREAD_MATERIAL(t);
    int ico2 = mixture_specie_index(m, "co2");
    int ih2o = mixture_specie_index(m, "h2o");

    // Partial pressures
    real p_ref = 101325.0;
    real p_x = (p_t + p_ref) * (xi[ico2] + xi[ih2o]);

    // Absorption coefficients
    *ab_wsggm = C_UDMI(c, t, UDM_KABS + nb) * p_x;

    // Soot absorption (set to zero for now)
    *ab_soot =  0.0;
}

// ---------------------------------------------------------------------------
// EMISSIVITY WEIGHTING FACTORS
// ---------------------------------------------------------------------------

DEFINE_EMISSIVITY_WEIGHTING_FACTOR(
    user_wsggm_emiss_weighting,
    c,
    t,
    T,
    nb,
    emissivity_weighting_factor
)
{
    *emissivity_weighting_factor = C_UDMI(c, t, UDM_AWTS + nb);
}

// ---------------------------------------------------------------------------
// EOF
// ---------------------------------------------------------------------------