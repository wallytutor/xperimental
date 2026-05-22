# -*- coding: utf-8 -*-

import sys
from pathlib import Path
from ctypes import CDLL, c_double, POINTER

import numpy as np
from numpy.typing import NDArray

_LIB = None
""" Stores shared library at module level. """


def _load_lib() -> None:
    """ Loads shared C library and defines function signatures. """
    global _LIB

    ext = ".dll" if sys.platform.startswith("win") else ".so"
    lib_path = Path(__file__).parent.resolve() / f"wsgglib{ext}"

    if not lib_path.exists():
        raise FileNotFoundError(f"No such {lib_path}")

    _LIB = CDLL(str(lib_path))

    _LIB.wsgg_emissivity.argtypes = [
        c_double,           # L [m]
        c_double,           # T [K]
        c_double,           # P [Pa]
        c_double,           # x_h2o [-]
        c_double,           # x_co2 [-]
        c_double,           # fvsoot [-]
    ]
    _LIB.wsgg_emissivity.restype = c_double

    _LIB.wsgg_coefs.argtypes = [
        c_double,           # T [K]
        c_double,           # P [Pa]
        c_double,           # x_h2o [-]
        c_double,           # x_co2 [-]
        c_double,           # fvsoot [-]
        POINTER(c_double),  # kabs [m^-1]
        POINTER(c_double),  # awts [-]
    ]
    _LIB.wsgg_coefs.restype = None


def __dir__() -> list[str]:
    """ Returns list of objects in this module. """
    return ["WSGG"]


class WSGG:
    """ Wrapper for Bordbar's 2014 WSGG model.

    Raises
    ------
    FileNotFoundError
        If the shared library is not found.
    """
    __slots__ = ("_lib",)

    def __init__(self) -> None:
        if _LIB is None:
            _load_lib()

        self._lib = _LIB

    def emissivity(self,
            L: float,
            T: float,
            P: float,
            x_h2o: float,
            x_co2: float,
            fvsoot: float
        ) -> float:
        """ Evaluate total emissivity of gas over path.

        Parameters
        ----------
        L: float
            Optical path for emissivity calculation [m].
        T: float
            Gas temperature [K].
        P: float
            Gas total pressure [Pa].
        x_h2o: float
            Mole fraction of water [-].
        x_co2: float
            Mole fraction of carbon dioxide [-]
        fvsoot: float
            Volume fraction soot [-].

        Returns
        -------
        float
            Total emissivity integrated over optical path.
        """
        return self._lib.wsgg_emissivity(L, T, P, x_h2o, x_co2, fvsoot)

    def coefficients(self,
            T: float,
            P: float,
            x_h2o: float,
            x_co2: float,
            fvsoot: float
        ) -> tuple[NDArray[np.float64], NDArray[np.float64]]:
        """ Evaluate model coefficients, see `emissivity` for details. """
        kabs_arr = (c_double * 5)()
        awts_arr = (c_double * 5)()

        self._lib.wsgg_coefs(T, P, x_h2o, x_co2, fvsoot, kabs_arr, awts_arr)

        kabs = np.frombuffer(kabs_arr, dtype=np.float64)
        awts = np.frombuffer(awts_arr, dtype=np.float64)

        return kabs, awts
