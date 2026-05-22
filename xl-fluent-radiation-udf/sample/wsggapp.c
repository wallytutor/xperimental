/*
 * wsggapp.c
 *
 * Simple application program illustrating how to call the
 * wsgg_emissivity function from C.
 */

#include <stdio.h>
#include "wsgglib.h"

int main(void) {
    double L = 1.0;
    double T = 1000.0;
    double P = 101325.0;
    double x_h2o = 0.18;
    double x_co2 = 0.08;
    double fvsoot = 0.0;

    double kabs[5];
    double awts[5];
    double eps;
    int i;

    printf("========================================================\n");
    printf("   WSGGRadlibBordbar2020 C89 Model Call Illustration\n");
    printf("========================================================\n\n");

    /* 1. Calculate and print total emissivity */
    eps = wsgg_emissivity(L, T, P, x_h2o, x_co2, fvsoot);

    printf("Inputs:\n");
    printf("  Optical Path length (L):   %.2f m\n", L);
    printf("  Gas Temperature (T):       %.2f K\n", T);
    printf("  Total Pressure (P):        %.1f Pa (%.3f atm)\n", P, P / 101325.0);
    printf("  H2O Mole Fraction (x_h2o): %.3f\n", x_h2o);
    printf("  CO2 Mole Fraction (x_co2): %.3f\n", x_co2);
    printf("  Soot Vol Fraction (fv):    %.2e\n", fvsoot);
    printf("\n");
    printf("Output Emissivity:           %.15f\n\n", eps);

    /* 2. Retrieve intermediate WSGG coefficients */
    wsgg_coefs(T, P, x_h2o, x_co2, fvsoot, kabs, awts);

    printf("Intermediate Band-by-Band Coefficients:\n");
    printf("  Band |   Absorption Coefficient [1/m]   |   Fractional Weight [-]\n");
    printf("  -----|----------------------------------|-----------------------\n");
    for (i = 0; i < 5; ++i) {
        printf("    %d  |   %28.15e   |   %20.15f\n", i, kabs[i], awts[i]);
    }
    printf("\n========================================================\n");

    return 0;
}
