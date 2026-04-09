// #r "library-xl/bin/Debug/net10.0/library-xl.dll"
#load "library-xl/Common/Core.fs"
#load "library-xl/Common/Elements.fs"
#load "library-xl/Common/Mixtures.fs"
#load "library-xl/Common/Numerical.fs"
#load "library-xl/Common/Plotting.fs"
#load "library-xl/Common/Thermophysics.fs"
#load "library-xl/Slycke.fs"
open LibraryXl.Common
open LibraryXl.Slycke
open System.IO

open Slycke

type UserInit =
    { cellCenters: float array
      spacing: float array
      timePoints: float array
      timeSteps: float array
      ycField: float array
      ynField: float array
      ycInf: float
      ynInf: float
      hcInf: float
      hnInf: float
      temperature: float
      relaxation: float
      maxNonlinIter: int
      relativeTolerance: float
      absoluteTolerance: float }

    interface ISlyckeUserInit with
        member self.Relaxation = self.relaxation
        member self.MaxNonlinIter = self.maxNonlinIter
        member self.RelativeTolerance = self.relativeTolerance
        member self.AbsoluteTolerance = self.absoluteTolerance
        member self.CellCenters = self.cellCenters
        member self.Spacing = self.spacing
        member self.TimePoints = self.timePoints
        member self.TimeSteps = self.timeSteps
        member self.YcField = self.ycField
        member self.YnField = self.ynField
        member self.YInf (t: float) = [| self.ycInf; self.ynInf |]
        member self.HInf (t: float) = [| self.hcInf; self.hnInf |]
        member self.Temperature (t: float) = self.temperature

type UserInit with
    static member create () =
        // temperature : process temperature in K.
        // ycIni, ynIni : initial mass fractions of carbon and nitrogen.
        // ycInf, ynInf : mass fractions of carbon and nitrogen in the environment.
        // hcInf, hnInf : mass transfer coefficients for external BCs.
        // numPoints : number of spatial discretization points.
        // domainDepth : depth of the spatial domain in m.
        // timeStep : time step for time discretization in s.
        // timeEnd : end time for the simulation in s.
        // relaxation : relaxation factor for iterative solver (0 < relaxation <= 1).
        // maxNonlinIter : maximum number of nonlinear iterations per time step.
        // rtol, atol : relative and absolute tolerances of nonlinear solver.
        let numPoints     = 200
        let domainDepth   = 0.002
        let timeEnd       = 7200.0
        let timeStep      = 1.0
        let ycIni         = 0.0023
        let ynIni         = 0.0000

        let z, dz = UserInit.linearSpace domainDepth numPoints
        let t, dt = UserInit.timeSpace timeEnd timeStep

        { cellCenters       = z
          spacing           = dz
          timePoints        = t
          timeSteps         = dt
          ycField           = Array.init numPoints (fun _ -> ycIni)
          ynField           = Array.init numPoints (fun _ -> ynIni)
          ycInf             = 0.011
          ynInf             = 0.005
          hcInf             = 1.0e-05
          hnInf             = 1.0e-05
          temperature       = 1173.15
          relaxation        = 1.0
          maxNonlinIter     = 10
          relativeTolerance = 1.0e-05
          absoluteTolerance = 1.0e-09 }

    static member linearSpace (depth: float) (n: int) =
        let dz = depth / (float n)
        let z0 = dz / 2.0
        let z1 = depth - z0
        let z = Numerical.arangeInclusive z0 z1 dz

        let dz1 = depth - z[n - 1]
        let mid = Array.map2 (fun x1 x2 -> x2 - x1) z[.. z.Length - 2] z[1 ..]
        let dz = Array.append [| z0 |] (Array.append mid [| dz1 |])
        z, dz

    static member timeSpace (tend: float) (tstep: float) =
        let t = Numerical.arangeInclusive 0.0 tend tstep
        let dt = Array.map2 (fun ti tj -> tj - ti) t[.. t.Length - 2] t[1 ..]
        t, dt

let dumpResults (mngr: SlyckeManager, dumpPath: string) =
    let z = mngr.Setup.CellCenters
    let xcFinal = mngr.CarbonField.Concentration
    let xnFinal = mngr.NitrogenField.Concentration

    let conv xc xn = mngr.Model.moleToMassFraction [| xc; xn |]
    let yFinal = Array.map2 conv xcFinal xnFinal
    let numPoints = z.Length

    let dumpLine i =
        let zi = z.[i]
        let yci = yFinal.[i].[0]
        let yni = yFinal.[i].[1]
        $"{zi:E17} {yci:E17} {yni:E17}"

    let finalStateLines = Array.init numPoints dumpLine
    File.WriteAllLines(dumpPath, finalStateLines)

let init = UserInit.create ()
let manager = SlyckeManager.runSimulation init

let outputDir = Path.Combine(__SOURCE_DIRECTORY__, "sandbox")
Directory.CreateDirectory outputDir |> ignore

let finalStatePath = Path.Combine(outputDir, "solution.dat")
let gnuplotPath = finalStatePath.Replace("\\", "/")

dumpResults (manager, finalStatePath)

Gnuplot.GnuplotInteractive ()
|>> "set title 'Final mass-fraction state'"
|>> "set xlabel 'Depth (m)'"
|>> "set ylabel 'Mass fraction (-)'"
|>> "set grid"
|>> "set key left top"
|>> $"plot '{gnuplotPath}' \\"
|>> "using 1:2 with lines lw 2 title 'yC',\\"
|>> "'' using 1:3 with lines lw 2 title 'yN'"