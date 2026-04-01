# -*- coding: utf-8 -*-
import cantera as ct
import majordome as mj
import matplotlib.pyplot as plt
import numpy as np
import sympy as sp

from inspect import signature
from IPython.display import display, Markdown
from textwrap import dedent


class SymbolicTracker:
    """ Utility class to track symbolic variables. """
    SYMBOLS = []

    @classmethod
    def clear(cls):
        cls.SYMBOLS.clear()

    @classmethod
    def sym(cls, name, **kwargs) -> sp.Symbol:
        if name in [s.name for s in cls.SYMBOLS]:
            raise ValueError(f"Symbol '{name}' already exists.")

        symbol = sp.symbols(name, **kwargs)
        cls.SYMBOLS.append(symbol)
        return symbol

    @classmethod
    def lambdify(cls, expr, symbols=None, use_all=True):
        if symbols is None and not use_all:
            symbols = sorted(list(expr.free_symbols), key=lambda x: x.name)

        if use_all:
            symbols = cls.SYMBOLS

        return sp.lambdify(symbols, expr, modules="numpy")


class SolidMaterial:
    """ Solid material properties. """
    def __init__(self):
        self._r = SymbolicTracker.sym("rho_s")
        self._c = SymbolicTracker.sym("c_ps")
        self._k = SymbolicTracker.sym("k_s")

    @property
    def density(self) -> sp.Symbol:
        """ Density of solid material [kg/m^3]. """
        return self._r

    @property
    def specific_heat_capacity(self) -> sp.Symbol:
        """ Specific heat capacity of solid material [J/kg-K]. """
        return self._c

    @property
    def thermal_conductivity(self) -> sp.Symbol:
        """ Thermal conductivity of solid material [W/m-K]. """
        return self._k

    def specific_energy(self, delta_T) -> sp.Symbol:
        """ Specific energy of solid material [J/m³]. """
        return self._r * self._c * delta_T  # type: ignore


class OperatingFluid:
    """ Operating fluid properties. """
    def __init__(self):
        self._h_c = SymbolicTracker.sym("h_g_c", positive=True)
        self._h_h = SymbolicTracker.sym("h_g_h", positive=True)
        self._r   = SymbolicTracker.sym("rho_g", positive=True)
        self._mu  = SymbolicTracker.sym("mu_g", positive=True)

    @property
    def density_inlet(self) -> sp.Symbol:
        """ Density of operating fluid at inlet [kg/m³]. """
        return self._r

    @property
    def enthalpy_cold(self) -> sp.Symbol:
        """ Enthalpy of cold fluid [J/kg]. """
        return self._h_c

    @property
    def enthalpy_hot(self) -> sp.Symbol:
        """ Enthalpy of hot fluid [J/kg]. """
        return self._h_h

    @property
    def specific_energy_inlet(self) -> sp.Symbol:
        """ Specific energy of operating fluid at inlet [J/m³]. """
        return self._r * (self._h_h - self._h_c)  # type: ignore

    @property
    def dynamic_viscosity(self):
        """ Dynamic viscosity of operating fluid [Pa-s]. """
        return self._mu


class HexagonalCell:
    """ Hexagonal cell *building block* of the thermocline model. """
    def __init__(self):
        self._D_h = SymbolicTracker.sym("D_h", positive=True)
        self._m_h = SymbolicTracker.sym("m_h", positive=True)

        # Half the distance between holes [m]
        l = (1 + self._m_h) * self._D_h / 2  # type: ignore

        # Height of hexagonal cell [m]
        h_h = sp.sqrt(l**2 - (l/2)**2)

        # Area of triangle formed by center of holes and symmetry [m^2]
        a_t = h_h * l / 2

        # Area of hexagonal cell [m^2]
        self._a_c = 6 * a_t

        # Area of holes in hexagonal cell [m^2]
        self._a_h = sp.pi * self._D_h**2 / 4  # type: ignore

        # Area of solid material in hexagonal cell [m^2]
        self._a_s = self._a_c - self._a_h  # type: ignore

    @property
    def solid_cross_section(self):
        """ Cross-section of solid material in hexagonal cell [m²]. """
        return self._a_s

    @property
    def fluid_cross_section(self):
        """ Cross-section of holes in hexagonal cell [m²]. """
        return self._a_h

    @property
    def surface_to_volume_ratio(self):
        """ Ratio of exchange area per unit volume [1/m]. """
        return sp.pi * self._D_h / self._a_s

    def get_surface_to_volume_ratio(self):
        expr = self.surface_to_volume_ratio
        return sp.lambdify((self._D_h, self._m_h), expr, "numpy")


class ThermoclineDescription:
    """ Thermocline model based on target operating conditions. """
    def __init__(self):
        self._H_t = SymbolicTracker.sym("H_t", positive=True)
        self._P_c = SymbolicTracker.sym("P_c", positive=True)
        self._P_d = SymbolicTracker.sym("P_d", positive=True)
        self._h_t = SymbolicTracker.sym("h_t", positive=True)
        self._T_h = SymbolicTracker.sym("T_h", positive=True)
        self._T_c = SymbolicTracker.sym("T_c", positive=True)

    @property
    def height(self):
        """ Height of thermocline structure [m]. """
        return self._h_t

    @property
    def power_charging(self):
        """ Target nominal operating charging power [W]. """
        return self._P_c

    @property
    def power_discharging(self):
        """ Target nominal operating discharging power [W]. """
        return self._P_d

    @property
    def stored_energy(self):
        """ Target total stored energy [J]. """
        return self._H_t

    @property
    def temperature_hot(self):
        """ Hot working temperature [K]. """
        return self._T_h

    @property
    def temperature_cold(self):
        """ Cold working temperature [K]. """
        return self._T_c

    @property
    def temperature_difference(self):
        """ Temperature difference between hot and cold operation [K]. """
        return self._T_h - self._T_c  # type: ignore

    @property
    def specific_energy(self):
        """ Specific energy of thermocline model [J/m]. """
        return self._H_t / self._h_t  # type: ignore


def total_cross_sectional_area(solid, thermocline):
    """ Total cross-sectional area of solid material in thermocline [m²]. """
    hv = solid.specific_energy(thermocline.temperature_difference)
    return thermocline.specific_energy / hv  # type: ignore


def cells_per_cross_section(A_t, cell_block):
    """ Number of hexagonal cells per cross section of thermocline. """
    return A_t / cell_block.solid_cross_section  # type: ignore


def power_per_cell(thermocline, N, mode="charging"):
    """ Power per hexagonal cell [W]. """
    match mode.lower():
        case "charging":
            return thermocline.power_charging / N  # type: ignore
        case "discharging":
            return thermocline.power_discharging / N  # type: ignore
        case _:
            raise ValueError(f"Invalid mode: {mode}")


def fluid_injection_velocity(P_n, fluid, cell_block):
    """ Fluid velocity through holes in hexagonal cell [m/s]. """
    return P_n / (fluid.specific_energy_inlet * cell_block.fluid_cross_section)  # type: ignore


def reynolds_number(U_g, cell_block, fluid):
    """ Reynolds number for fluid flow through holes in hexagonal cell. """
    D = sp.sqrt(4 * cell_block.fluid_cross_section / sp.pi)  # type: ignore
    return fluid.density_inlet * U_g * D / fluid.dynamic_viscosity  # type: ignore


def swamee_jain_friction_factor(Re, epsilon, D):
    """ Calculate friction factor using the Swamee-Jain equation. """
    return 0.25 / (np.log10(epsilon / (3.7 * D) + 5.74 / Re**0.9))**2


def darcy_weisbach_pressure_drop(L, D, rho, v, f=0.05):
    """ Calculate pressure drop using the Darcy-Weisbach equation."""
    return f * (L / D) * (rho * v**2) / 2  # type: ignore


def air_properties(T):
    air = ct.Solution("airish.yaml")
    air.X = "O2:0.21, N2:0.79"
    air.TP = T, ct.one_atm
    return air.density_mass, air.enthalpy_mass, air.viscosity


def format_evaluation(fn, name, expression, args, units="",
                      formatter=lambda x: f"{x:.3g}"):
    expr  = sp.latex(sp.simplify(expression))

    try:
        value = formatter(fn(*args))
    except Exception as e:
        raise ValueError(f"Required symbols: {signature(fn)}; {e}")

    return display(Markdown(dedent(f"""\
        $$
        {name} = {expr} = {value}\\:\\mathrm{{{units}}}
        $$
        """
    )))


@mj.MajordomePlot.new(size=(5, 4), xlabel="Diameter of holes (cm)",
                      ylabel="Surface-to-volume ratio (1/m)")
def plot_surface_to_volume_ratio(cell_block, *, plot):
    func = cell_block.get_surface_to_volume_ratio()
    D_samples = np.linspace(0.010, 0.050, 100)
    m_samples = np.arange(0.5, 3.0+0.001, 0.5)

    _, ax = plot.subplots()
    ax = ax[0]

    for m in m_samples:
        ratio_sv = func(D_samples, m)
        ax.plot(100*D_samples, ratio_sv, label=f"m={m:.1f}")

    ax.legend(loc=1, fontsize="small")
