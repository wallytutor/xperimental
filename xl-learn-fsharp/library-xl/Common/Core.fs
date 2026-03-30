namespace LibraryXl.Common
open System.Runtime.InteropServices

module OperatingSystem =
    let platform = RuntimeInformation.IsOSPlatform
    let isWindows = platform OSPlatform.Windows
    let isLinux   = platform OSPlatform.Linux
    let isOSX     = platform OSPlatform.OSX

    type OsPlatform =
        | Windows
        | Linux
        | OSX
        | Unknown

    let getOsPlatform () =
        if isWindows then
            Windows
        elif isLinux then
            Linux
        elif isOSX then
            OSX
        else
            Unknown

module Constants =
    type ElementData =
        { Symbol: string; Number: int; MolarMass: float }

    [<Literal>]
    let gasConstant: float = 8.314
