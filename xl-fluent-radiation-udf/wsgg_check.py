# -*- coding: utf-8 -*-

import sys
from pathlib import Path
from typing import Any

import matplotlib.pyplot as plt
import numpy as np
from ruamel.yaml import YAML

HERE = Path(__file__).resolve().parent
""" Path to the current script directory. """

sys.path.insert(0, str(HERE / "src"))
from wsgg_radlib import WsggRadlib


def format_array(arr: np.ndarray) -> str:
    """ Format a numpy array for printing. """
    return "[" + ", ".join([f"{x:.4e}" for x in arr]) + "]"


def evaluate_test_case(wsgg: WsggRadlib, data: dict[str, Any]):
    """ Evaluate a single test case. """
    L      = data['L']
    T      = data['T']
    P      = data['P']
    x_h2o  = data['x_h2o']
    x_co2  = data['x_co2']
    fvsoot = data['fvsoot']

    # Evaluate C model via ctypes
    eps_c = wsgg.emissivity(L, T, P, x_h2o, x_co2, fvsoot)
    kabs_c, awts_c = wsgg.coefficients(T, P, x_h2o, x_co2, fvsoot)

    print(f"Case - {data['description']}")
    print(f"    L      : {L}")
    print(f"    T      : {T}")
    print(f"    P      : {P}")
    print(f"    x_h2o  : {x_h2o}")
    print(f"    x_co2  : {x_co2}")
    print(f"    fvsoot : {fvsoot}")
    print("")
    print(f"    eps    : {eps_c:.6f}")
    print(f"    kabs   : {format_array(kabs_c)}")
    print(f"    awts   : {format_array(awts_c)}")
    print("-" * 80)


def evaluate_validation(wsgg: WsggRadlib):
    """ Sample validation space and display the results."""
    fig, ax = plt.subplots(nrows=1, ncols=2, figsize=(12, 8))

    def scan_flue(T, L, *, M):
        x_co2 = 1 / (1 + M)
        return wsgg.emissivity(L, T, 101325.0, M*x_co2, x_co2, 0.0)

    dry_flue = np.vectorize(lambda T, L: scan_flue(T, L, M=1/8))
    wet_flue = np.vectorize(lambda T, L: scan_flue(T, L, M=1/1))

    L = np.asarray([0.01, 0.1, 0.5, 1.0, 3.0, 5.0, 10.0, 20.0, 60.0])
    T = np.arange(400, 2401, 100)
    sample = np.meshgrid(T, L)

    for n, eps in enumerate(dry_flue(*sample)):
        ax[0].plot(T, eps, label=f"{L[n]} atm.m")

    for n, eps in enumerate(wet_flue(*sample)):
        ax[1].plot(T, eps, label=f"{L[n]} atm.m")

    ax[0].grid(True, linestyle=":", alpha=1)
    ax[1].grid(True, linestyle=":", alpha=1)

    ax[0].set_title("Dry flue M=1/8")
    ax[1].set_title("Wet flue M=1")

    ax[0].set_xlabel("Temperature [K]")
    ax[1].set_xlabel("Temperature [K]")

    ax[0].set_ylabel("Total emissivity")
    ax[1].set_ylabel("Total emissivity")

    ax[0].set_xlim(400, 2400)
    ax[1].set_xlim(400, 2400)
    ax[0].set_ylim(0, 1)
    ax[1].set_ylim(0, 1)

    ax[0].set_xticks(np.arange(400, 2401, 400))
    ax[1].set_xticks(np.arange(400, 2401, 400))

    ax[0].set_yticks(np.arange(0, 1.01, 0.1))
    ax[1].set_yticks(np.arange(0, 1.01, 0.1))

    ax[0].legend(loc=1, fontsize=9)
    ax[1].legend(loc=1, fontsize=9)

    plt.show()


def main():
    """ Main function. """
    test_cases = YAML().load(open(HERE / "wsgg_check.yaml"))
    wsgg = WsggRadlib(HERE)

    for idx, data in enumerate(test_cases, 1):
        evaluate_test_case(wsgg, data)

    evaluate_validation(wsgg)


if __name__ == "__main__":
    main()
