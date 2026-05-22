# -*- coding: utf-8 -*-

import sys
from pathlib import Path
from ruamel.yaml import YAML

HERE = Path(__file__).resolve().parent
""" Path to the current script directory. """

sys.path.insert(0, str(HERE / "src"))
from wsgg_radlib import WsggRadlib


def format_array(arr):
    """ Format a numpy array for printing. """
    return "[" + ", ".join([f"{x:.4e}" for x in arr]) + "]"


def evaluate_test_case(wsgg, data):
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


def main():
    """ Main function. """
    test_cases = YAML().load(open(HERE / "wsgg_check.yaml"))
    wsgg = WsggRadlib(HERE)

    for idx, data in enumerate(test_cases, 1):
        evaluate_test_case(wsgg, data)


if __name__ == "__main__":
    main()
