namespace LibraryXl.Common

open System
open System.Diagnostics

module Gnuplot =
    let executableName =
        match OperatingSystem.getOsPlatform () with
        | OperatingSystem.Windows -> "gnuplot.exe"
        | OperatingSystem.Linux   -> "gnuplot"
        | OperatingSystem.OSX     -> "gnuplot"
        | OperatingSystem.Unknown -> failwith "not supported OS"

    type GnuplotInteractive() =
        let handle =
            try
                let psi = new ProcessStartInfo (
                    FileName              = executableName,
                    UseShellExecute       = false,
                    CreateNoWindow        = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                )
                Some (Process.Start psi)
            with
            | ex ->
                eprintfn $"Failed to start gnuplot ({executableName}): {ex.Message}"
                Trace.TraceWarning $"Failed to start gnuplot: {ex.Message}"
                None

        member self.write (command: string) =
            match handle with
            | Some proc -> proc.StandardInput.WriteLine command
            | None -> ()

        static member (|>>) (gp: GnuplotInteractive, str: string) =
            if String.IsNullOrWhiteSpace str then
                gp
            elif str.TrimStart().StartsWith "set term" then
                Trace.TraceWarning $"Terminal selection not allowed in interactive mode"
                gp
            else
                gp.write str
                gp