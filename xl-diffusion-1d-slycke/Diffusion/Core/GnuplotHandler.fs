module Diffusion.Core.GnuplotHandler

open System
open System.Diagnostics

let private executableName =
    match OperatingSystem.getOsPlatform () with
    | OperatingSystem.Windows -> [ "gnuplot.exe" ]
    | OperatingSystem.Linux   -> [ "gnuplot" ]
    | OperatingSystem.OSX     -> [ "gnuplot" ]
    | OperatingSystem.Unknown -> failwith "not supported OS"

let private tryStartGnuplot (fileName: string) =
    try
        let psi = new ProcessStartInfo (
            FileName               = fileName,
            Arguments              = "-persist",
            UseShellExecute        = false,
            CreateNoWindow         = (OperatingSystem.getOsPlatform () <> OperatingSystem.Windows),
            RedirectStandardInput  = true,
            RedirectStandardError  = true
        )

        match Process.Start psi with
        | null ->
            let msg = $"Failed to start gnuplot ({fileName}): Process.Start returned null"
            eprintfn $"{msg}"
            Trace.TraceWarning msg
            None
        | proc ->
            // Forward runtime gnuplot diagnostics to stderr/trace so failures are visible.
            proc.ErrorDataReceived.Add (fun args ->
                if not (String.IsNullOrWhiteSpace args.Data) then
                    let msg = $"gnuplot stderr: {args.Data}"
                    eprintfn $"{msg}"
                    Trace.TraceWarning msg)
            proc.BeginErrorReadLine ()
            Some proc
    with
    | ex ->
        let msg = $"Exception while starting gnuplot ({fileName}): {ex.Message}"
        eprintfn $"{msg}"
        Trace.TraceWarning msg
        None

let private processLine (arr: float[]) (i: int) =
    if i < arr.Length then $"{arr[i]}" else ""

// TODO this is just a sketch, work in a grammar for all commands.
type LineStyle =
    { Color: string
      Width: float
      PointType: int
      DashType: int }

    member self.ToString (id: int) : string =
        let tokens = [
            "set"; "linestyle"; id.ToString();
            "dt"; self.DashType.ToString();
            "lw"; self.Width.ToString();
            "pt"; self.PointType.ToString();
            "lc"; $"'{self.Color}'" ]
        String.concat " " tokens

type GnuplotInteractive() =
    let handle = executableName |> Seq.tryPick tryStartGnuplot

    member _.write (command: string) =
        match handle with
        | Some proc when proc.HasExited ->
            let msg = $"Gnuplot process has exited with code {proc.ExitCode}" +
                      $"Command attempted: {command}"

            eprintfn $"{msg}"
            Trace.TraceWarning msg
        | Some proc ->
            proc.StandardInput.WriteLine command
            proc.StandardInput.Flush ()
        | None -> ()

    static member (|>>) (gp: GnuplotInteractive, str: string) =
        if String.IsNullOrWhiteSpace str then
            gp
        else
            gp.write str
            gp

    static member (|>>) (gp: GnuplotInteractive, f: GnuplotInteractive -> GnuplotInteractive) =
        f gp

let pngCairoOutput (saveAs: string) (options: string option) (gp: GnuplotInteractive) =
    let optionsStr = defaultArg options "font 'Sans,10' size 400,600"

    gp.write $"\nset term pngcairo enhanced color {optionsStr}"
    gp.write $"\nset output '{saveAs}.png'"
    gp

let dataBlock (tag: string) (arrays: seq<float[]>) (gp: GnuplotInteractive) =
    let length = arrays |> Seq.map Array.length |> Seq.max

    gp.write $"\n${tag} << EOD"

    for i in 0 .. length - 1 do
        let line =
            arrays
            |> Seq.map (fun arr -> processLine arr i)
            |> String.concat "  "

        gp.write line

    gp.write "EOD\n"
    gp
