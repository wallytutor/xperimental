module Diffusion.Core.MixtureProperties

open ElementData

let private validateComposition (name: string) (comp: float array) (mass: float array) =
    if Array.length comp <> Array.length mass then
        invalidArg name $"Length of {name} must match number of valid elements."

let private meanMolarMassFromMass (w: float array) (y: float array) : float =
    1.0 / Array.sum (Array.map2 (fun yk wk -> yk / wk) y w)

let private meanMolarMassFromMole (w: float array) (x: float array)  : float =
    Array.sum (Array.map2 (fun xk wk -> xk * wk) x w)

let private makeMeanMolarMassFromMass (w: float array) =
    fun (y: float array) ->
        validateComposition "y" y w
        meanMolarMassFromMass w y

let private makeMeanMolarMassFromMole (w: float array) =
    fun (x: float array) ->
        validateComposition "x" x w
        meanMolarMassFromMole w x

let makeMassFractionToMoleFractionConverter (elements: string list) =
    let w = getMolarMassArray elements
    let meanMolarMass = makeMeanMolarMassFromMass w

    fun (y: float array) ->
        validateComposition "y" y w
        let m = meanMolarMass y
        Array.map2 (fun yk wk -> m * yk / wk) y w

let makeMoleFractionToMassFractionConverter (elements: string list) =
    let w = getMolarMassArray elements
    let meanMolarMass = makeMeanMolarMassFromMole w

    fun (x: float array) ->
        validateComposition "x" x w
        let m = meanMolarMass x
        Array.map2 (fun xk wk -> xk * wk / m) x w

let makeInterstitialMolarFractionToConcentrationConverter
    (matrixMeanMolarMass: float) (matrixFreeDensity: float) =
    let M = matrixMeanMolarMass
    let rho = matrixFreeDensity

    fun (x: float array) ->
        let matrixMoleFraction = 1.0 - Array.sum x
        let matrixConcentration = rho / (matrixMoleFraction * M)
        x |> Array.map (fun xi -> xi * matrixConcentration)

let makeConcentrationToInterstitialMolarFractionConverter
    (matrixMeanMolarMass: float) (matrixFreeDensity: float) =
    let M = matrixMeanMolarMass
    let rho = matrixFreeDensity

    fun (c: float array) ->
        let matrixConcentration = rho / M
        let totalConcentration = Array.sum c + matrixConcentration
        c |> Array.map (fun ci -> ci / totalConcentration)
