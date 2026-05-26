module Diffusion.Core.FunctionTypes

type TimeValue<'T> =
    | Constant of 'T
    | Function of (float -> 'T)

    static member ToFunc (input: TimeValue<'T>) : (float -> 'T) =
        match input with
        | Constant value -> fun _ -> value
        | Function func -> func

type CellField =
    | Uniform of float
    | PerCell of float array

    static member expand (cellCount: int) = function
        | Uniform x -> Array.create cellCount x
        | PerCell xs when xs.Length = cellCount -> Array.copy xs
        | PerCell xs ->
            invalidArg "xs" $"Expected {cellCount} values, got {xs.Length}."