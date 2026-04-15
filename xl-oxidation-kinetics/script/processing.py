# -*- coding: utf-8 -*-
from matplotlib import pyplot as plt
from scipy.optimize import curve_fit
import numpy as np
import pandas as pd


def model(T, a, b):
    """ Parametric model of emissivity in terms of temperature. """
    return a * (1 - np.exp(-T / b))


def fit_model(df, p0=None):
    """ Fit parametric emissivity model to data. """
    x = df["T"].to_numpy()
    y = df["eps"].to_numpy()
    bounds = ([0, 1.0e-10], float("inf"))
    popt, _ = curve_fit(model, x, y, p0=p0, bounds=bounds)
    return popt


def emissivity(t, T, tc, tm, popto, popts):
    """ Emissivity of sheet in terms of temperature. """
    p = np.exp(-(t / tc) ** tm)
    return p * model(T, *popts) + (1 - p) * model(T, *popto)


def heat_capacity(T):
    """ Heat capacity of steel in terms of temperature. """
    LT = (+7.726812135693e+02, -3.061763527505e+00, +9.713590095324e-03,
          -1.177081491234e-05, +5.368288667701e-09)
    HT = (+5.830451419897e+02, -1.943420512064e-01, +2.581569711817e-04,
          -6.320141833273e-08, +1.932134676071e-13)

    cp = np.piecewise(T, [T < 1123, T >= 1123], [
        lambda T: T * (T * (T * (LT[4] * T + LT[3]) + LT[2]) + LT[1]) + LT[0],
        lambda T: T * (T * (T * (HT[4] * T + HT[3]) + HT[2]) + HT[1]) + HT[0]
    ])
    return cp


def plot_emissivity():
    """ Plot emissivity model and data. """
    T = np.linspace(0, 1300, 1301) + 273.15
    t = np.linspace(0, 400, 401)

    tc = 150
    tm = 3

    dfo = pd.read_csv("data/oxide.csv", names=["T", "eps"])
    dfs = pd.read_csv("data/sheet.csv", names=["T", "eps"])
    dfo["T"] += 273.15
    dfs["T"] += 273.15

    popto = fit_model(dfo, p0=(0.1, 500))
    popts = fit_model(dfs, p0=(0.1, 900))

    epso = model(T, *popto)
    epss = model(T, *popts)

    print(popto, popts)

    plt.close("all")
    plt.style.use("classic")
    plt.figure(figsize=(12, 6))

    plt.subplot(121)
    plt.plot(T, epso, label="Oxide")
    plt.plot(T, epss, label="Steel")
    plt.plot(dfo["T"], dfo["eps"], ".", label="_none_")
    plt.plot(dfs["T"], dfs["eps"], ".", label="_none_")
    plt.xlabel("Temperature [$K$]")
    plt.ylabel("Emissivity [-]")
    plt.legend(loc="best")
    plt.grid(linestyle=":")

    plt.subplot(122)
    for Tk in (np.arange(200, 1201, 200) + 273.15):
        epst = emissivity(t, Tk, tc, tm, popto, popts)
        plt.plot(t, epst, label=F"{Tk:.0f} $K$")
    plt.xlabel("Thickness [$nm$]")
    plt.ylabel("Emissivity [-]")
    plt.legend(loc="best")
    plt.grid(linestyle=":")

    plt.tight_layout()
    plt.savefig("media/emissivity.png", dpi=300)

    # popto = [  0.9350831    512.28971691]
    # popts = [3.08938352e-01 8.40044087e+02]


def plot_heat_capacity():
    """ Plot heat capacity model. """
    T = np.linspace(0, 1300, 1301) + 273.15

    plt.close("all")
    plt.style.use("classic")

    plt.plot(T, heat_capacity(T))
    plt.xlabel("Temperature [$K$]")
    plt.ylabel("Heat capacity [$J\\,kg^{-1}\\,K^{-1}$]")
    plt.grid(linestyle=":")

    plt.tight_layout()
    plt.savefig("media/heat-capacity.png", dpi=300)


def postprocess():
    """ Run postprocessing tasks. """
    names = ["time", "thickness", "temperature"]
    df = pd.read_csv("../results.csv", names=names)
    t = df["time"].to_numpy()
    T = df["temperature"].to_numpy()
    l = df["thickness"].to_numpy() * 1_000_000_000

    plt.close("all")
    plt.style.use("classic")
    fig, ax1 = plt.subplots(figsize=(7, 6))
    ax2 = ax1.twinx()

    ax1.plot(t, T, "r", label="Strip temperature")
    ax2.plot(t, l, "b", label="Oxide thickness")

    ax1.set_xlabel("Time [s]")
    ax1.set_ylabel("Temperature [K]")
    ax2.set_ylabel("Thickness [nm]")
    ax1.grid(linestyle=":")
    ax2.set_ylim(0, 600)
    ax1.legend(loc="upper left")
    ax2.legend(loc="lower right")

    fig.tight_layout()
    fig.savefig("postprocess.png", dpi=300)


if __name__ == "__main__":
    postprocess()
