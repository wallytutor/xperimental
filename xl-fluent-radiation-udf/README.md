# Standalone C89 WSGG Model

Implements Weighted Sum of Gray Gases (WSGG) radiation properties model into a standalone, pure **C89 compliant module** with zero dependencies outside the standard math library.

## 📁 Source Files

All the source files are located in the [src/](src/) directory.

## Library

- [wsgg_radlib_bordbar_2020.h](src/wsgg_radlib_bordbar_2020.h) - Public C89 header exposing the clean function signatures.

- [wsgg_radlib_bordbar_2020.c](src/wsgg_radlib_bordbar_2020.c) — Highly-optimized implementation using Horner's method and embedding coefficients directly.

- [wsgg_radlib.py](src/wsgg_radlib.py) - Python interface for evaluating the model using ctypes.

## Examples

- [main.c](src/main.c) - Simple illustrative C program showing direct linkage and execution.

- [udf_sample.c](src/udf_sample.c) - Fluent UDF for WSGG dummy model (for checking compilation and linking).

- [wsgg_radlib_udf.c](src/wsgg_radlib_udf.c) - Fluent UDF for WSGG model.

## Build and Compile

- [Makefile](src/Makefile) - Linux build script.

- [build.ps1](build.ps1) - PowerShell script for compiling shared DLL libraries using Mingw64 GCC.

## Test

- [wsgg_check.py](wsgg_check.py) - Sample Python script for evaluating the model.

---

## 📋 Environment

- Python 3.12
- uv
- gcc (on Windows, use MinGW64)

### Create Virtual Environment

It is recommended to use `uv` for creating a virtual environment. The following commands show how to create a virtual environment and install the required packages:

```bash
# Create environment
uv venv --python 3.12 .venv

# Activate in Windows
. .venv/Scripts/activate

# Activate in Linux
source .venv/bin/activate

# Install tools
uv pip install gmsh numpy matplotlib ruamel-yaml
```

---

## 🚀 Direct C Example Execution Output

```
========================================================
   WSGGRadlibBordbar2020 C89 Model Call Illustration
========================================================

Inputs:
  Optical Path length (L):   1.00 m
  Gas Temperature (T):       1000.00 K
  Total Pressure (P):        101325.0 Pa (1.000 atm)
  H2O Mole Fraction (x_h2o): 0.180
  CO2 Mole Fraction (x_co2): 0.080
  Soot Vol Fraction (fv):    0.00e+00

Output Emissivity:           0.183685774525871

Intermediate Band-by-Band Coefficients:
  Band |   Absorption Coefficient [1/m]   |   Fractional Weight [-]
  -----|----------------------------------|-----------------------
    0  |          0.000000000000000e+00   |      2.700875528124971
    1  |          1.742166701236471e-02   |      0.274939183163228
    2  |          1.926201637035025e-01   |      0.288178187122667
    3  |          1.578204068390441e+00   |      0.234163841751569
    4  |          1.723962360219242e+01   |      0.090577827738800

========================================================
```

---

## 📊 Validation Results

*Work in progress*

- [ ] Reproduce *Fig. 2* from [Bordbar (2014)](https://doi.org/10.1016/j.combustflame.2014.03.013)

- [ ] Reproduce *Fig. 2* from [Bordbar (2021)](https://doi.org/10.1016/j.ijheatmasstransfer.2021.121207) (tentative, as it is the previous version of the model that is implemented here).

---

## 🛠️ Build and Compilation

The C89 code compiles cleanly and warning-free with strict standard-conforming flags `-Wall -Wextra -ansi -pedantic -O2 -fPIC`.

- 💻 Windows (PowerShell) compilation: run `.\build.ps1` for compiling with the Mingw64 GCC compiler `gcc.exe`.

- 🐧 Linux compilation: just use the classical `make` and everything should work with a C89 compliant compiler.

This builds:

- `wsgg_radlib.dll` or `libwsgg_radlib.so` — Standalone shared library.

- `wsgg_app.exe` or `wsgg_app` — Direct C illustration program.

## 🧩 Integrating to Fluent

Mesh is generated using gmsh by running `python geometry.py` from within the environment.

## Useful Links

- [Fluent WSGG Model](https://ansyshelp.ansys.com/public/account/secured?returnurl=////Views/Secured/corp/v242/en/flu_th/flu_th_mod_var_abs.html)

> Important: The WSGGM is implemented in a gray approach. If the WSGGM is used with a non-gray model, the absorption coefficient will be the same in all bands. Use DEFINE_GRAY_BAND_ABS_COEFF to change the absorption coefficient per band or per gray gas.

- [Fluent UDF Documentation](https://ansyshelp.ansys.com/public/account/secured?returnurl=//Views/Secured/corp/v252/en/flu_udf/flu_udf_ModelSpecificDEFINE.html)

- [CFD Online - WSGG in Fluent](https://www.cfd-online.com/Forums/fluent-udf/120780-weighted-sum-gray-gas-model-wsggm-fluent.html)
