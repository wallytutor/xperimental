# -*- coding: utf-8 -*-

import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
""" Path to the current script directory. """

sys.path.insert(0, str(HERE / "src"))
from wsgg_radlib import WsggRadlib

# Define test cases: (L, T, P, x_h2o, x_co2, fvsoot)
test_cases = [
    # Standard case
    (1.0, 1000.0, 101325.0, 0.18, 0.08, 0.0),
    # High pressure, soot, intermediate Mr
    (2.0, 1500.0, 202650.0, 0.10, 0.15, 1e-6),
    # Low temperature, CO2-rich domain boundary
    (0.5, 500.0, 101325.0, 0.05, 0.05, 0.0),
    # High temperature, H2O-rich domain boundary
    (1.0, 2000.0, 101325.0, 0.25, 0.02, 2.5e-6),
    # Extreme temperature clipping (low)
    (1.0, 250.0, 101325.0, 0.15, 0.10, 0.0),
    # Extreme temperature clipping (high)
    (1.0, 2600.0, 101325.0, 0.15, 0.10, 0.0),
    # Extremely low CO2-rich mixture (Ml < 0.01)
    (1.0, 1200.0, 101325.0, 0.20, 0.001, 0.0),
    # Pure H2O limit (Ml > 4.0)
    (1.0, 1200.0, 101325.0, 0.30, 0.0, 0.0),
]

wsgg = WsggRadlib(HERE)

for idx, (L, T, P, x_h2o, x_co2, fvsoot) in enumerate(test_cases, 1):
    # Evaluate C model via ctypes
    eps_c = wsgg.emissivity(L, T, P, x_h2o, x_co2, fvsoot)
    kabs_c, awts_c = wsgg.coefficients(T, P, x_h2o, x_co2, fvsoot)

    print(
        f"Case {idx}: L={L}, T={T}, P={P:.1f}, x_h2o={x_h2o}, x_co2={x_co2}, fv={fvsoot}"
    )

    print("    C  kabs:", kabs_c)
    print("    C  awts:", awts_c)
    print("-" * 80)
