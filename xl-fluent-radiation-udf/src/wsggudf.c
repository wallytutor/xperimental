/****************************************************************************\

Important
=========
- The macros *as written* are compatible with incompressible flow, as we are
  adding a base reference pressure (see use of P_REFERENCE below). In a
  compressible case could use the pressure directly in the ideal gas law.

- Here we have hardcoded molecular weights, consider getting the same values
  from mixture SCM file if consistency is important to you (not really here).

\****************************************************************************/

#include "udf.h"
#include "materials.h"

#define P_REFERENCE 101325.0
#define R_UNIVERSAL 8.314462

#define MOLEC_WEIGHT_CO2 0.044010
#define MOLEC_WEIGHT_H2O 0.018015

#define NUM_GRAY_GASES 5

#define UDM_KABS 0
#define UDM_AWTS (NUM_GRAY_GASES + UDM_KABS)
#define UDM_MW   (NUM_GRAY_GASES + UDM_AWTS)

static int last_evaluated_iter = -1;

DEFINE_EXECUTE_ON_LOADING(wsgg_load,libname)
{
    Message("\nLoading WSGG UDF library: %s\n", libname);
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

DEFINE_ADJUST(wsgg_eval,domain)
{
    int current_iter;

    #if !RP_HOST
    current_iter = N_ITER;

    if (current_iter != last_evaluated_iter)
    {
        Thread *t;
        cell_t c;

        thread_loop_c(t, domain)
        {
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
                    real mw_mix = rho * T * R_UNIVERSAL / (p + P_REFERENCE);

                    // Mole fractions from mass fractions
                    real x_h2o = C_YI(c, t, ih2o) * mw_mix / MOLEC_WEIGHT_H2O;
                    real x_co2 = C_YI(c, t, ico2) * mw_mix / MOLEC_WEIGHT_CO2;

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

                    C_UDMI(c, t, UDM_MW) = mw_mix;
                }
                end_c_loop(c, t)
            }
        }

        last_evaluated_iter = current_iter;
    }
    #endif
}

DEFINE_WSGGM_ABS_COEFF(wsgg_abs_coeff,c,t,xi,p_t,s,soot_conc,T,nb,kabs,ksoot)
{
    *kabs = C_UDMI(c, t, UDM_KABS + nb);
    *ksoot = 0.0;
}

DEFINE_EMISSIVITY_WEIGHTING_FACTOR(wsgg_emiss_weighting,c,t,T,nb,awts)
{
    *awts = C_UDMI(c, t, UDM_AWTS + nb);
}
