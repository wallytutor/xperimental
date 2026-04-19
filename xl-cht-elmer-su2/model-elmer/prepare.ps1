ElmerGrid 14 2 mesh.msh -autoclean -merge 1.0e-05 -out 'elmer/'

ElmerGrid 2 2 'elmer/' -partdual -metiskway 4

mpiexec -n 4 ElmerSolver_mpi case.sif