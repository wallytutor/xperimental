# -*- coding: utf-8 -*-
import sys
from pathlib import Path
from ctypes import CDLL, c_double, POINTER
import numpy as np
from numpy.typing import NDArray


class WsggRadlib:
    """ Provides an API for Bordbar's WSGG model.

    Parameters
    ----------
    lib_dir : Path
        Path to the directory containing the shared library.
    """

    __slots__ = ("_lib",)

    def __init__(self, lib_dir: Path) -> None:
        lib_path = self._get_library_path(lib_dir)
        self._lib = self._load_library(lib_path)

    @staticmethod
    def _get_library_path(lib_dir: Path) -> Path:
        """ Manage OS dependent library path. """
        if sys.platform.lower().startswith("win"):
            lib_path = lib_dir / "wsgg_radlib.dll"
        else:
            lib_path = lib_dir / "libwsgg_radlib.so"

        if not lib_path.exists():
            raise FileNotFoundError(f"No such {lib_path}")

        return lib_path

    @staticmethod
    def _load_library(lib_path: Path) -> None:
        """ Load library and define function signatures. """
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
