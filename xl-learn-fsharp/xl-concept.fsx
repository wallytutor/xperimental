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

module PhDThesis =
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
            let numPoints     = 100
            let domainDepth   = 0.002
            let z, dz = UserInit.linearSpace domainDepth numPoints

            { cellCenters       = z
              spacing           = dz
              timePoints        = [| 0.0; 7200.0|]
              timeSteps         = [| 7200.0 |]
              ycField           = Array.init numPoints (fun _ -> 0.0023)
              ynField           = Array.init numPoints (fun _ -> 0.0000)
              ycInf             = 0.0
              ynInf             = 0.0
              hcInf             = 1.0e-05
              hnInf             = 1.0e-05
              temperature       = 1173.15
              relaxation        = 0.75
              maxNonlinIter     = 20
              relativeTolerance = 1.0e-06
              absoluteTolerance = 1.0e-15 }

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

    let stepCarburizing () =
        let t, dt = UserInit.timeSpace (2.0 * 3600.0) 1.0e-00
        { UserInit.create () with
            timePoints = t
            timeSteps = dt
            ycInf     = 0.011
            ynInf     = 0.000 }

    let stepDiffusion (manager: SlyckeManager) =
        let t, dt = UserInit.timeSpace (1.0 * 3600.0) 1.0e-00
        let ycField, ynField = manager.getReinitialization ()

        { UserInit.create () with
            timePoints = t
            timeSteps  = dt
            ycField    = ycField
            ynField    = ynField
            ycInf      = 0.0
            ynInf      = 0.0
            hcInf      = 0.0e-05
            hnInf      = 0.0e-05 }

    let stepNitriding (manager: SlyckeManager) =
        let t, dt = UserInit.timeSpace (3.0 * 3600.0) 1.0e-00
        let ycField, ynField = manager.getReinitialization ()

        { UserInit.create () with
            timePoints = t
            timeSteps  = dt
            ycField    = ycField
            ynField    = ynField
            ycInf      = 0.0
            ynInf      = 0.005
            hcInf      = 0.0e-05
            hnInf      = 1.0e-05 }

    let dumpResults (mngr: SlyckeManager, dumpName: string) =
        let z = mngr.cellCenters
        let xcFinal = mngr.carbonField.Concentration
        let xnFinal = mngr.nitrogenField.Concentration

        let conv xc xn = mngr.model.moleToMassFraction [| xc; xn |]
        let yFinal = Array.map2 conv xcFinal xnFinal

        let outputDir = Path.Combine(__SOURCE_DIRECTORY__, "sandbox")
        Directory.CreateDirectory outputDir |> ignore
        let dumpPath = Path.Combine(outputDir, dumpName)

        let dumpLine i =
            let zi = 1000.0 * z.[i]
            let yci = 100.0 * yFinal.[i].[0]
            let yni = 100.0 * yFinal.[i].[1]
            $"{zi:E17} {yci:E17} {yni:E17}"

        let finalStateLines = Array.init z.Length dumpLine
        File.WriteAllLines(dumpPath, finalStateLines)
        dumpPath.Replace("\\", "/")

// [<EntryPoint>]
// let Main (args: string[]) : int =
let main () =
    let manager1 = SlyckeManager.runSimulation (PhDThesis.stepCarburizing ())
    let manager2 = SlyckeManager.runSimulation (PhDThesis.stepDiffusion manager1)
    let manager3 = SlyckeManager.runSimulation (PhDThesis.stepNitriding manager2)

    let gnuplotPath1 = PhDThesis.dumpResults (manager1, "carburizing.dat")
    let gnuplotPath2 = PhDThesis.dumpResults (manager2, "diffusion.dat")
    let gnuplotPath3 = PhDThesis.dumpResults (manager3, "nitriding.dat")

    let result =
        Gnuplot.GnuplotInteractive ()
        |>> "set title 'Final Composition Profiles'"
        |>> "set xlabel 'Depth (mm)'"
        |>> "set ylabel 'Composition (%wt)'"
        |>> "set linestyle 1 dt 3 lw 1 lc '#000000'"
        |>> "set linestyle 2 dt 2 lw 1 lc '#000000'"
        |>> "set linestyle 3 dt 1 lw 1 lc '#000000'"
        |>> "set linestyle 4 dt 1 lw 1 lc '#FF0000'"
        |>> "set grid"
        |>> "set key right top"
        |>> $"plot \\"
        |>> $"'{gnuplotPath1}' using 1:2 with lines linestyle 1 title 'C (carburizing)',\\"
        |>> $"'{gnuplotPath2}' using 1:2 with lines linestyle 2 title 'C (homogenizing)',\\"
        |>> $"'{gnuplotPath3}' using 1:2 with lines linestyle 3 title 'C (nitriding)',\\"
        |>> $"''               using 1:3 with lines linestyle 4 title 'N (nitriding)'"
        |> ignore

    0

main ()