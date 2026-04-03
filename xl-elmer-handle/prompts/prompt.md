You are a scientific programmer who is an expert in [Elmer](https://github.com/ElmerCSC/elmerfem) and you read all the [manuals](https://github.com/ElmerCSC/elmerfem-manuals).

- Now you will create a C# (net10.0) library to generate/write Elmer input files (.sif) wil a well formatted structure. The library should allow users to create and manipulate the different sections of the .sif file programmatically, making it easier to generate complex input files for Elmer simulations. The library should be able to handle various sections of the .sif file, such as Header, Simulation, Constants, Material, Body, Solver, Equation, Initial Condition, and Boundary Condition. Each section should be represented as a class with properties corresponding to the parameters in the .sif file. The library should also include methods to serialize these classes into the correct .sif format. Althout it is not case-sensitive, the library should maintain a consistent case style for the generated .sif files (e.g., using PascalCase for section names and parameters). Additionally, the library should provide functionality to save the generated .sif file to disk.

- As different Elmer solvers have different properties, the Solver class will be a base for different specializations. They all print their header as "Solver <n>" and share the base properties, such as when the solver executes (which shall be an enumeration type). For now the only specialized sub-type will be HeatSolve, and the post processing providers SaveLine and SaveData. Each class will check if the required material properties are available, as a single general material class exists; materials must support constant properties, MATC expressions, Lua expressions, user libraries, and tabular data in terms of a variable. In the main handle of a project there will be functions for calling ElmerGrid for file conversion and partitioning. conversion from/to will be represented by an enumeration with the allowed values.

- Auxiliary classes for linear/nonlinear solver control are available and inject entries in the body of solvers. any solver may accept them, but some solvers as HeatSolve will not run without it, so check for its presence before writing file.

- Putting in practice: we should by now be able to reproduce the generation of "sample-1/model.sif" programmatically, and the generated file should be identical to the original one, except for the comments which cannot be machine-generated. Write an example program (that I will execute as .NET interactive from Jupyter) that generates the same .sif file programmatically using the library you created.

- Regarding the setters of material properties, I don't think the design using setters/getters exposed to python is good or safe. It is better to have specific methods for setting each property with the different supported types, so that we can check for the presence of required properties in the specialized solvers. Please modify the library accordingly. For example:

```python
# This is bad:
molten.Density = MaterialPropertyValue.IncludeFile(var, "data/rho.dat")

# This is good: the list provides the variables over which the data is
# tabulated. In general there is just one, but it could be more.
molten.SetDensityFile("data/rho.dat", ["Temperature"])
```

- The "Sections.cs" will grow quickly and soon will become unmanageable. Split it into multiple files for each section under a subdirectory "Sections" before the next step. For tabular properties entered dynamically, modify the API as follows:

```python
# This is bad and does not work yet, do not try to fix it!
molten.YoungsModulus = MaterialPropertyValue.Tabular(
    "Temperature",
    [
        TabulatedMaterialPoint(298.15, 417.0e9),
        TabulatedMaterialPoint(1873.15, 417.0e9),
        TabulatedMaterialPoint(1900.0, 100.0e9),
        TabulatedMaterialPoint(3000.0, 100.0e9),
    ],
    interpolation="",  # "" → emits just "Real" (no interpolation qualifier)
)

# This is good, please implement it:
T = [298.15, 1873.15, 1900.0, 3000.0]
Y = [417.0e9, 417.0e9, 100.0e9, 100.0e9]
data = list(map(list, zip(T, Y)))
variables = ["Temperature"]
molten.SetYoungsModulusTabular(data, variables, interpolation="")
```

- As it was the case for Sections, the "SpecializedSolvers.cs" file will also grow quickly. Split it into multiple files for each specialized solver under a subdirectory "Solvers" before the next step, which is described in the following snippet:

```python
# Transform this
molten = sif.AddMaterial()
molten.Name = "Molten"

# Into this, and ensure names are unique and mandatory:
molten = sif.AddMaterial("Molten")
```

- A minimal syntax highlighter will be available for VSCode for helping manually editing files. It will distinguish between section headers, section entries labels, and section entries values, which may be string, bool, keyword, or numerical types.
