// #define COMPILED

#if COMPILED
#r "../Diffusion/bin/Debug/net10.0/Diffusion.dll"
#else
#load "../Diffusion/Numerics/Autodiff.fs"
#load "../Diffusion/Numerics/TridiagonalSolver.fs"
#load "../Diffusion/Numerics/NumericalUtilities.fs"
#load "../Diffusion/Core/OperatingSystem.fs"
#load "../Diffusion/Core/GnuplotHandler.fs"
#load "../Diffusion/Core/ElementData.fs"
#load "../Diffusion/Core/MixtureProperties.fs"
#load "../Diffusion/Core/FunctionTypes.fs"
#load "../Diffusion/Core/FvmDomain1D.fs"
#load "../Diffusion/Core/DiffusionData1D.fs"
#load "../Diffusion/Core/Carbonitriding1D.fs"
#load "../Diffusion/Slycke/DiffusionPlainFeCN.fs"
#endif