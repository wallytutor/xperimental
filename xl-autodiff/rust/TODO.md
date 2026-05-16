# To-Do

## Walter's letter to Gemini

Right now this draf is really a mess; we have too many ideas and lack of focus as I need a tool working and very complete in a short time frame. Now we need to organize the next steps to deliver a working tool. Please write a plan with the next steps to achieve this goal in TODO.md. For doing so please keep the following information in mind:

- We cannot have a serious tool if compound data has a single temperature range for functions. Most databases fit data in ranges to better represent measurements.

- Having a single parameterization (currently Maier-Kelly) is not enough. Most gas phase databases use NASA7 and NASA9 polynomials, while NIST WebBook of chemistry summarizes a great amount of Shomate data. I will at some point ask you to convert the whole [condensed](https://github.com/Cantera/cantera/blob/main/data/nasa_condensed.yaml) and [gas](https://github.com/Cantera/cantera/blob/main/data/nasa_gas.yaml) phase data into the tool's format(s). Also notice that most CALPHAD databases report straight Gibbs energies, we need to think how to incorporate that as well (remember that this is a CALPHAD project!).

- The current solver is obscure, I honestly don't understand it. We will need to provide references from literature and a clear explanation of how it works. I would prefer a solid solver than a fast and obscure one. Also, you did not use the autodiff library, which is what this project is all about!

- Propose next steps to achieve the goal. Remember that currently I need to have a solver for the system CAS (Ca-Al-Si-O) plus H2O/CO2 gases at high temperature and atmospheric pressure.

## Gemini's response to Walter's letter

Dear Walter,

You are completely right. The current solver was a stopgap heuristic, and hardcoding a single parameterization without temperature ranges will not scale to a serious CALPHAD tool. We need to pivot to a rigorous, autodiff-driven architecture. Here is the concrete roadmap to deliver a solid, literature-backed solver for the CAS + H2O/CO2 system:

### Phase 1: Robust Data Architecture (Parameterizations & Ranges)
**Objective**: Support standard CALPHAD and thermochemical database formats (NASA7/9, Shomate, direct Gibbs).

- **Refactor `Substance` Data Model**: Replace `raw_coefs` with a piecewise structure `Vec<TemperatureRange>` where each range defines a `T_min` and `T_max`.

- **Implement Parameterization Enums/Traits**: Create variants for `MaierKelley`, `Shomate`, `NASA7`, `NASA9`, and `GibbsPolynomial` (the standard CALPHAD format).

- **Data Ingestion**: Prepare parsers for Cantera YAML formats (`nasa_condensed.yaml` and `nasa_gas.yaml`) to automatically convert and ingest the data into our new Rust structures.

### Phase 2: Formal Gibbs Energy Minimization using Autodiff

**Objective**: Replace the current obscure LP solver with a mathematically rigorous method that actively utilizes your `autodiff` engine.

- **Adopt an Established Algorithm**: Implement a rigorous constrained optimization solver. The standard approach is the **White-Johnson-Dantzig (WJD)** method or a primal-dual **Interior Point Method** for non-linear Gibbs free energy minimization. We will cite specific literature (e.g., *Smith & Missen, Chemical Reaction Equilibrium Analysis* or similar thermodynamic standards) to keep the mechanics fully transparent.

- **Integrate Autodiff (`Dual`)**: This is where the project shines. Instead of hardcoding partial derivatives, we will pass the total system Gibbs energy function through the `autodiff` engine to automatically retrieve exact exact gradients (chemical potentials, $\mu_i$) and the Hessian matrix. This ensures Newton-Raphson steps are perfectly accurate and robust.

### Phase 3: The CAS (Ca-Al-Si-O) + H2O/CO2 System

**Objective**: Deliver your required physical chemical system at high temperatures ($P = 1 \text{ atm}$).

- **Database Expansion**: Add thermodynamic data for Silica (SiO2) and ternary/quaternary minerals (Wollastonite, Gehlenite, Anorthite, Grossular, etc.) to fully close the CAS system.

- **Benchmarking & Validation**: Run the new autodiff solver strictly against the CAS + H2O/CO2 system and validate the phase diagrams / equilibrium outputs against known literature.

### Immediate Action Items

1. **Redesign `thermo.rs`**: Define the Enums for `NASA7/9`, `Shomate`, and `GibbsPolynomial` along with their temperature bounds.

2. **Update Evaluation Logic**: Modify `cp`, `enthalpy`, `entropy`, and `gibbs` to select the correct range and parameterization at runtime, ensuring they accept `Dual` inputs.

3. **Scaffold the Autodiff Solver**: Remove `evaluate_local_equilibrium` and lay the groundwork for the Newton-based Gibbs minimizer.

I will sleep not but will let you working on the immediate action items. Please implement the enums/structs for the required thermodynamic data parameterizations and check the logic of selection. For each type, please add corresponding tests for checking correctness of the data. For the Cantera YAML I was maybe too vague: I don't want a parser, but I want you to convert the referenced YAML files into the tool's format(s): write code from the values you have in the database. In all cases, if functionalities are reusable, organize them into library functions in the appropriate module. All species should allow tabulation in JANAF format for ease of verification by humans.