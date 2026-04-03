# Sample 1


Generate the equivalent of `model.sif` programmatically using Xl.Elmer.Sif C# library via pythonnet.

Notes on equivalence:

- Parameters are sorted alphabetically within each section (Elmer ignores order).

- Multi-word Elmer keywords are stored as PascalCase (e.g. HeatConductivity).

- Elmer's SIF parser is case-insensitive and normalises spaces, so HeatConductivity and Heat Conductivity are treated identically.

- Array size hints (n) in the original are cosmetic and are omitted.

- MATC script variables and inline $(...) references are reproduced verbatim.

- Comment lines are not generated.

```python
import sys
import os
from pathlib import Path

import clr

# # Add the compiled library to the sys.path so CLR can find it
# dll_dir = Path(__file__).parent.parent.parent / "Xl.Elmer.Sif" / "bin" / "Debug" / "net9.0"
# if not dll_dir.exists():
#     raise FileNotFoundError(
#         f"Library not found at {dll_dir}\n"
#         f"Please build the project: dotnet build Xl.Elmer.Sif"
#     )
```

```python
__file__
```

```python

```
