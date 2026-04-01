# Conjugate heat transfer

## Preparing the environment

- Starting with `uv` (recommended):

```bash
uv venv .venv
.venv\Scripts\activate
uv pip install -r requirements.txt
```

- Starting with plain `python` (slower):

```bash
python -m venv .venv
.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
```

---

## Running SU2 model

- Generate the geometry and mesh:

```bash
python geometry-build.py
```

- Run the SU2 CFD solver:

```bash
.\manage.ps1 -Run -NumberProc 20

# Equivalent to:
# cd model-su2/
# mpiexec.exe -n 20 SU2_CFD.exe master.cfg
```

- Clean up the generated files:

```bash
.\manage.ps1 -Clean
```

## Running Elmer model

*Upcoming...*
