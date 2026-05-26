module Diffusion.Core.OperatingSystem

open System.Runtime.InteropServices

let platform  = RuntimeInformation.IsOSPlatform
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
