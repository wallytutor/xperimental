module Diffusion.Core.FvmDomain1D

open Diffusion.Numerics

type ImmersedNodeDomain1D =
    { CellSizes: float array
      Spacing: float array
      Interior: float array
      WestBoundary: float
      EastBoundary: float }

    member self.ToArray () : float array =
        let nInterior = self.Interior.Length
        let nTotal = nInterior + 2
        let arr = Array.zeroCreate nTotal

        arr.[0]          <- self.WestBoundary
        arr.[nTotal - 1] <- self.EastBoundary

        for i in 1 .. nInterior do
            arr.[i] <- self.Interior.[i - 1]

        arr

    member self.PrintInfo () : string =
        let nInterior = self.Interior.Length
        let nTotal    = nInterior + 2

        let fmtInterior (i) =
            let zc = self.Interior.[i]
            let dz = self.CellSizes.[i]
            let z0 = zc - dz / 2.0
            let z1 = zc + dz / 2.0
            $"Cell {i:D4} at {zc:E6}, Size = {dz:E6}, Range = [{z0:E6}; {z1:E6}]"

        let interiorLines =
          [ for i in 0 .. nInterior - 1 -> fmtInterior i ]

        let coordinates = self.ToArray()
        let gridSpacingLines =
          [ for i in 1 .. nTotal - 1 ->
                let delta = self.Spacing.[i-1]
                let z0 = coordinates.[i - 1]
                let z1 = coordinates.[i]
                $"Spacing {i-1:D4} is {delta:E6}, Range = [{z0:E6}; {z1:E6}]" ]

        let lines = [
            $"Total points ....: {nTotal}"
            $"West boundary ...: {self.WestBoundary:E6}"
            $"East boundary ...: {self.EastBoundary:E6}"
            "\n"
            yield! interiorLines
            "\n"
            yield! gridSpacingLines ]

        String.concat "\n" lines

    static member private CreateFromCoordinates
      (zf: float array) : ImmersedNodeDomain1D =
        let dz = zf |> Array.pairwise |> Array.map (fun (zi, zj) -> zj - zi)
        let zc = zf |> Array.pairwise |> Array.map (fun (zi, zj) -> (zi + zj) / 2.0)
        let dc = zc |> Array.pairwise |> Array.map (fun (zi, zj) -> zj - zi)

        let halfWest = zc.[0] - zf.[0]
        let halfEast = zf.[zf.Length - 1] - zc.[zc.Length - 1]

        { CellSizes = dz
          Spacing = [| yield halfWest; yield! dc; yield halfEast |]
          Interior = Array.copy zc
          WestBoundary = zf.[0]
          EastBoundary = zf.[zf.Length - 1] }
    static member CreateFromLinearSpace
        (depth: float) (n: int) (shift: float option) : ImmersedNodeDomain1D =
        let shift = defaultArg shift 0.0
        let zf = linearSpace shift (shift + depth) (n + 1)
        ImmersedNodeDomain1D.CreateFromCoordinates zf

    static member CreateFromGeometricSpace
      (depth: float) (n: int) (d0: float) (d1: float)
      (shift: float option) : ImmersedNodeDomain1D =
        let shift = defaultArg shift 0.0
        let zf = geometricSpace shift (shift + depth) (n + 1) d0 d1
        ImmersedNodeDomain1D.CreateFromCoordinates zf
