/* wsgg.c */

#include "wsgglib.h"
#include <math.h>

#define T_MIN 300.0
#define T_MAX 2400.0
#define T_RED 1200.0
#define P_TOL 1.0e-10
#define MR_LIM_CO2 0.01
#define MR_LIM_H2O 4.0
#define MR_LIM_INF 1.0e+08

/*
 * Model Coefficients database extracted from majordome/data/wsgg.yaml
 */

/* cCoef[4][5][5] - Gray gas polynomial coefficients for mixture weights */
static const double C_COEF[4][5][5] = {
    /* Gray gas 0 */
    {
        {  7.412956e-001, -5.244441e-001,  5.822860e-001, -2.096994e-001,  2.420312e-002 },
        { -9.412652e-001,  2.799577e-001, -7.672319e-001,  3.204027e-001, -3.910174e-002 },
        {  8.531866e-001,  8.230754e-002,  5.289430e-001, -2.468463e-001,  3.109396e-002 },
        { -3.342806e-001,  1.474987e-001, -4.160689e-001,  1.697627e-001, -2.040660e-002 },
        {  4.314362e-002, -6.886217e-002,  1.109773e-001, -4.208608e-002,  4.918817e-003 }
    },
    /* Gray gas 1 */
    {
        {  1.552073e-001, -4.862117e-001,  3.668088e-001, -1.055508e-001,  1.058568e-002 },
        {  6.755648e-001,  1.409271e+000, -1.383449e+000,  4.575210e-001, -5.019760e-002 },
        { -1.125394e+000, -5.913199e-001,  9.085441e-001, -3.334201e-001,  3.842361e-002 },
        {  6.040543e-001, -5.533854e-002, -1.733014e-001,  7.916083e-002, -9.893357e-003 },
        { -1.105453e-001,  4.646634e-002, -1.612982e-003, -3.539835e-003,  6.121277e-004 }
    },
    /* Gray gas 2 */
    {
        {  2.550242e-001,  3.805403e-001, -4.249709e-001,  1.429446e-001, -1.574075e-002 },
        { -6.065428e-001,  3.494024e-001,  1.853509e-001, -1.013694e-001,  1.302441e-002 },
        {  8.123855e-001, -1.102009e+000,  4.046178e-001, -8.118223e-002,  6.298101e-003 },
        { -4.532290e-001,  6.784475e-001, -3.432603e-001,  8.830883e-002, -8.415221e-003 },
        {  8.693093e-002, -1.306996e-001,  7.414464e-002, -2.029294e-002,  2.010969e-003 }
    },
    /* Gray gas 3 */
    {
        { -3.451994e-002,  2.656726e-001, -1.225365e-001,  3.001508e-002, -2.820525e-003 },
        {  4.112046e-001, -5.728350e-001,  2.924490e-001, -7.980766e-002,  7.996603e-003 },
        { -5.055995e-001,  4.579559e-001, -2.616436e-001,  7.648413e-002, -7.908356e-003 },
        {  2.317509e-001, -1.656759e-001,  1.052608e-001, -3.219347e-002,  3.386965e-003 },
        { -3.754908e-002,  2.295193e-002, -1.600472e-002,  5.046318e-003, -5.364326e-004 }
    }
};

/* dCoef[4][5] - Gray gas polynomial coefficients for baseline absorption */
static const double D_COEF[4][5] = {
    {  3.404288e-002,  6.523048e-002, -4.636852e-002,  1.386835e-002, -1.444993e-003 },
    {  3.509457e-001,  7.465138e-001, -5.293090e-001,  1.594423e-001, -1.663261e-002 },
    {  4.570740e+00,  2.168067e+00, -1.498901e+00,  4.917165e-001, -5.429990e-002 },
    {  1.098169e+002, -5.092359e+001,  2.343236e+001, -5.163892e+000,  4.393889e-001 }
};

/* BCO2[4][5] - CO2-rich blend polynomial coefficients */
static const double BCO2[4][5] = {
    {  8.425766e-001, -1.442229e+000,  1.286974e+000, -5.202712e-001,  7.581559e-002 },
    { -3.023864e-002,  5.264245e-001, -6.209696e-001,  2.704755e-001, -4.090690e-002 },
    {  1.070243e-001, -1.989596e-001,  3.101602e-001, -1.737230e-001,  3.081180e-002 },
    {  3.108972e-002,  1.981489e-001, -2.543676e-001,  1.061331e-001, -1.498231e-002 }
};

/* BH2O[4][5] - H2O-rich blend polynomial coefficients */
static const double BH2O[4][5] = {
    {  7.129509e-001, -1.378353e+000,  1.555028e+000, -6.636291e-001,  9.773674e-002 },
    {  1.589917e-001,  5.635578e-002,  2.666874e-001, -2.040335e-001,  3.742408e-002 },
    { -1.196373e-001,  1.349665e+000, -1.544797e+000,  6.397595e-001, -9.153650e-002 },
    {  3.078250e-001, -6.003555e-001,  4.441261e-001, -1.468813e-001,  1.824702e-002 }
};

/* KCO2[5] - Constant absorption coefficients for pure CO2 limit */
static const double KCO2[5] = { 0.0, 3.388079e-002, 4.544269e-001, 4.680226e+000, 1.038439e+002 };

/* KH2O[5] - Constant absorption coefficients for pure H2O limit */
static const double KH2O[5] = { 0.0, 7.703541e-002, 8.242941e-001, 6.854761e+000, 6.593653e+001 };


/*
 * Helper to evaluate a polynomial of degree 4 using Horner's Method.
 */
static double evaluate_polynomial(const double *coef, double x) {
    double result = 0.0;
    int i;
    for (i = 4; i >= 0; --i) {
        result = result * x + coef[i];
    }
    return result;
}

/*
 * Evaluates the C coefficient polynomials, handling the clear band balancing dynamically.
 */
static double get_ccoef_val(int iband, int j, double Mr) {
    if (iband > 0) {
        return evaluate_polynomial(C_COEF[iband - 1][j], Mr);
    } else {
        /* Clear band (index 0) balanced dynamically: 1.0 - sum(P_i(Mr)) */
        double sum = 0.0;
        int i;
        for (i = 0; i < 4; ++i) {
            sum += evaluate_polynomial(C_COEF[i][j], Mr);
        }
        return 1.0 - sum;
    }
}


static double evaluate_mr_base(double P_h2o, double P_co2)
{
    double Mr = P_h2o / (P_co2 + P_TOL);
    return Mr;
}

static double evaluate_ml_base(double Mr)
{
    double Ml = Mr < MR_LIM_INF ? Mr : MR_LIM_INF;
    return Ml;
}

static double evaluate_mr_bound(double Mr)
{
    if (Mr < MR_LIM_CO2) Mr = MR_LIM_CO2;
    if (Mr > MR_LIM_H2O) Mr = MR_LIM_H2O;
    return Mr;
}

static double evaluate_Tr_bound(double T)
{
    if (T < T_MIN) T = T_MIN;
    if (T > T_MAX) T = T_MAX;
    return T;
}

/*
 * Public API
 */

void wsgg_coefs(
        double T,
        double P,
        double x_h2o,
        double x_co2,
        double fvsoot,
        double *kabs,
        double *awts
    ) {
    double P_atm = P / 101325.0;
    double P_h2o = P_atm * x_h2o;
    double P_co2 = P_atm * x_co2;

    double Mr = evaluate_mr_base(P_h2o, P_co2);
    double Ml = evaluate_ml_base(Mr);

    double Mr_clipped = evaluate_mr_bound(Mr);
    double T_clipped = evaluate_Tr_bound(T);
    double Tr = T_clipped / T_RED;

    int iband;

    for (iband = 0; iband < 5; ++iband) {
        /* 1. Evaluate baseline absorption and weight */
        double kabs_val = 0.0;
        double awts_val = 0.0;

        if (iband > 0) {
            kabs_val = evaluate_polynomial(D_COEF[iband - 1], Mr_clipped) * (P_co2 + P_h2o);
        } else {
            kabs_val = 0.0;
        }

        {
            double Tr_pow = 1.0;
            int j;
            for (j = 0; j < 5; ++j) {
                double c_val = get_ccoef_val(iband, j, Mr_clipped);
                awts_val += c_val * Tr_pow;
                Tr_pow *= Tr;
            }
        }

        /* 2. Apply CO2-rich correction */
        if (Ml < MR_LIM_CO2) {
            double f = (MR_LIM_CO2 - Ml) / MR_LIM_CO2;
            double kco2_pfac = KCO2[iband] * P_co2;

            kabs_val = f * kco2_pfac + (1.0 - f) * kabs_val;

            {
                double bco2_val = 0.0;
                if (iband > 0) {
                    bco2_val = evaluate_polynomial(BCO2[iband - 1], Tr);
                } else {
                    double bco2_sum = 0.0;
                    int i;
                    for (i = 0; i < 4; ++i) {
                        bco2_sum += evaluate_polynomial(BCO2[i], Tr);
                    }
                    bco2_val = 1.0 - bco2_sum;
                }
                awts_val = f * bco2_val + (1.0 - f) * awts_val;
            }
        }

        /* 3. Apply H2O-rich correction */
        if (Ml > MR_LIM_H2O) {
            double f = (MR_LIM_INF - Ml) / (MR_LIM_INF - MR_LIM_H2O);
            double kh2o_pfac = KH2O[iband] * P_h2o;

            kabs_val = f * kabs_val + (1.0 - f) * kh2o_pfac;

            {
                double bh2o_val = 0.0;
                if (iband > 0) {
                    bh2o_val = evaluate_polynomial(BH2O[iband - 1], Tr);
                } else {
                    double bh2o_sum = 0.0;
                    int i;
                    for (i = 0; i < 4; ++i) {
                        bh2o_sum += evaluate_polynomial(BH2O[i], Tr);
                    }
                    bh2o_val = 1.0 - bh2o_sum;
                }
                awts_val = f * awts_val + (1.0 - f) * bh2o_val;
            }
        }

        /* 4. Add soot volume fraction contribution */
        if (fvsoot > 0.0) {
            kabs_val += 1817.0 * fvsoot * (Tr * T_RED);
        }

        kabs[iband] = kabs_val;
        awts[iband] = awts_val;
    }
}

double wsgg_emissivity(
    double L,
    double T,
    double P,
    double x_h2o,
    double x_co2,
    double fvsoot
)
{
    double kabs[5];
    double awts[5];

    wsgg_coefs(T, P, x_h2o, x_co2, fvsoot, kabs, awts);

    {
        double eps = 0.0;
        int i;
        for (i = 0; i < 5; ++i) {
            eps += awts[i] * (1.0 - exp(-kabs[i] * L));
        }
        return eps;
    }
}
