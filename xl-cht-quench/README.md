# Conjugate Heat Transfer Quench Simulation

- [Elmer Docs](https://www.nic.funet.fi/pub/sci/physics/elmer/doc/)

## Running the Simulation

```powershell
# Generate the mesh (change NumDimensions to "2d" or "3d")
.\manage.ps1 -NumDimensions "2d" -NumProc 20 -RebuildMesh

# Run the simulation
.\manage.ps1 -NumDimensions "2d" -NumProc 20 -Simulate

# Force reinitialization of the simulation (relaxation step)
.\manage.ps1 -NumDimensions "2d" -NumProc 20 -Reinitialize
```
