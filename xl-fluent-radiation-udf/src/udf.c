/* THIS IS A STARTING DRAFT, CHECK HERE;

https://ansyshelp.ansys.com/public/account/secured?returnurl=
//Views/Secured/corp/v252/en/flu_udf/flu_udf_ModelSpecificDEFINE.html

*/

#include "udf.h"
#include "materials.h"

DEFINE_WSGGM_ABS_COEFF(user_wsggm_abs_coeff, c, t, xi, p_t, s, soot_conc, Tcell, nb, ab_wsggm, ab_soot)
{
    Material *m = THREAD_MATERIAL(t);
    int ico2 = mixture_specie_index(m, "co2");
    int ih2o = mixture_specie_index(m, "h2o");
    real CO2_molf, H2O_molf;
    real k2, k3, k4;

    CO2_molf = xi[ico2];
    H2O_molf = xi[ih2o];

    // Update coefficients here
    // TODO check how to use the awts here, as what is done in the sample
    // below is not compatible with Bordbar (or is it?). Also notice that
    // we should do some caching, as all coefficients are computed at once
    // (or split the evaluation per gray-gas in the library).
    //
    // void wsgg_coefs(
    //     double T,
    //     double P,
    //     double x_h2o,
    //     double x_co2,
    //     double fvsoot,
    //     double *kabs,
    //     double *awts
    //     );

    switch (nb)
    {
        case 0 :  /*  First gray gas*/
        {
            *ab_wsggm = 0;
        }
        break;

        case 1 :  /*  Second gray gas*/
        {
            k2   = 0.1;
            *ab_wsggm = (k2 * (H2O_molf + CO2_molf)) * p_t;
        }
        break;

        case 2 :  /*  Third gray gas*/
        {
            k3   =  7.1;
            *ab_wsggm = (k3 * (H2O_molf + CO2_molf)) * p_t;
        }
        break;

        case 3 :  /*  Fourth gray gas*/
        {
            k4   =  60.0;
            *ab_wsggm = (k4 * (H2O_molf + CO2_molf)) * p_t;
        }
    }

    *ab_soot =  0.1;
}