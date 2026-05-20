# -*- coding: utf-8 -*-
"""
validate.py

Loads the compiled C shared library and compares its outputs against
the reference python class WSGGRadlibBordbar2020 over various physical states.
"""

import sys
from pathlib import Path
from ctypes import CDLL, c_double, POINTER
import numpy as np
import pandas as pd

HERE = Path(__file__).resolve().parent
""" Path to the current script directory. """


class WsggInterface:
    """Provides an API for Bordbar's WSGG model."""

    __slots__ = ("_lib",)

    def __init__(self, lib_dir: Path) -> None:
        lib_path = self._get_library_path(lib_dir)
        self._lib = self._load_library(lib_path)

    @staticmethod
    def _get_library_path(lib_dir: Path) -> Path:
        if sys.platform.lower().startswith("win"):
            lib_path = lib_dir / "wsgg_radlib.dll"
        else:
            lib_path = lib_dir / "libwsgg_radlib.so"

        if not lib_path.exists():
            raise FileNotFoundError(f"No such {lib_path}")

        return lib_path

    @staticmethod
    def _load_library(lib_path: Path) -> None:
        """Define function signatures for the loaded library."""
        lib = CDLL(str(lib_path))

        # double wsgg_emissivity(
        #     double L, double T, double P,
        #     double x_h2o, double x_co2, double fvsoot);
        lib.wsgg_emissivity.argtypes = [
            c_double,
            c_double,
            c_double,
            c_double,
            c_double,
            c_double,
        ]
        lib.wsgg_emissivity.restype = c_double

        # void wsgg_coefs(
        #     double T, double P, double x_h2o,
        #     double x_co2, double fvsoot,
        #     double *kabs, double *awts);
        lib.wsgg_coefs.argtypes = [
            c_double,
            c_double,
            c_double,
            c_double,
            c_double,
            POINTER(c_double),
            POINTER(c_double),
        ]
        lib.wsgg_coefs.restype = None

        return lib

    def emissivity(
        self, L: float, T: float, P: float, x_h2o: float, x_co2: float, fvsoot: float
    ) -> float:
        return self._lib.wsgg_emissivity(L, T, P, x_h2o, x_co2, fvsoot)

    def coefficients(
        self, T: float, P: float, x_h2o: float, x_co2: float, fvsoot: float
    ) -> tuple[np.ndarray, np.ndarray]:
        kabs_arr = (c_double * 5)()
        awts_arr = (c_double * 5)()

        self._lib.wsgg_coefs(T, P, x_h2o, x_co2, fvsoot, kabs_arr, awts_arr)
        kabs = np.array(kabs_arr)
        awts = np.array(awts_arr)
        return kabs, awts


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

wsgg = WsggInterface(HERE)

for idx, (L, T, P, x_h2o, x_co2, fvsoot) in enumerate(test_cases, 1):
    # Evaluate C model via ctypes
    eps_c = wsgg.emissivity(L, T, P, x_h2o, x_co2, fvsoot)
    kabs_c, awts_c = wsgg.coefficients(T, P, x_h2o, x_co2, fvsoot)

    print(
        f"Case {idx}: L={L}, T={T}, P={P:.1f}, x_h2o={x_h2o}, x_co2={x_co2}, fv={fvsoot}"
    )

    # print("    C  kabs:", kabs_c)
    # print("    C  awts:", awts_c)
    print("-" * 80)
