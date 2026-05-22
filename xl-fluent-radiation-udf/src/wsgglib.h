/* wsgglib.h */
#ifndef WSGGLIB_H
#define WSGGLIB_H

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Calculates the total emissivity integrated over the optical path.
 *
 * Parameters:
 *   L      - Optical path length for emissivity calculation [m].
 *   T      - Gas temperature [K].
 *   P      - Total gas pressure [Pa].
 *   x_h2o  - Mole fraction of water vapor [-].
 *   x_co2  - Mole fraction of carbon dioxide [-].
 *   fvsoot - Volume fraction of soot [-] (default is 0.0).
 *
 * Returns:
 *   The total integrated gas emissivity (dimensionless, usually in [0, 1]).
 */
double wsgg_emissivity(
    double L,
    double T,
    double P,
    double x_h2o,
    double x_co2,
    double fvsoot
);

/**
 * Helper function exposing the intermediate weighted sum of gray gases (WSGG)
 * coefficients (absorption coefficients and weights) for debugging, validation,
 * or advanced solvers.
 *
 * Parameters:
 *   T      - Gas temperature [K].
 *   P      - Total gas pressure [Pa].
 *   x_h2o  - Mole fraction of water vapor [-].
 *   x_co2  - Mole fraction of carbon dioxide [-].
 *   fvsoot - Volume fraction of soot [-].
 *   kabs   - Output array for the 5 gas absorption coefficients [1/m] (must be size 5).
 *   awts   - Output array for the 5 gray gas fractional weights [-] (must be size 5).
 */
void wsgg_coefs(
    double T,
    double P,
    double x_h2o,
    double x_co2,
    double fvsoot,
    double *kabs,
    double *awts
);

#ifdef __cplusplus
}
#endif

#endif /* WSGGLIB_H */