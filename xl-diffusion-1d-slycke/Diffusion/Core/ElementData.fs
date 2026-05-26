module Diffusion.Core.ElementData

type private ElementSymbol = C | N | Si | Ca | V | Cr | Mn | Fe | Mo | W

type private ElementData =
    { Symbol: ElementSymbol; Number: int; Name: string; MolarMass: float }

let private tryParse = function
    | "C"  -> Some C
    | "N"  -> Some N
    | "Si" -> Some Si
    | "Ca" -> Some Ca
    | "V"  -> Some V
    | "Cr" -> Some Cr
    | "Mn" -> Some Mn
    | "Fe" -> Some Fe
    | "Mo" -> Some Mo
    | "W"  -> Some W
    | _ -> None

let private elementTable : Map<ElementSymbol, ElementData> =
    [ C,  { Symbol = C;  Number = 6;  Name = "carbon";     MolarMass = 12.011 }
      N,  { Symbol = N;  Number = 7;  Name = "nitrogen";   MolarMass = 14.007 }
      Si, { Symbol = Si; Number = 14; Name = "silicon";    MolarMass = 28.085 }
      Ca, { Symbol = Ca; Number = 20; Name = "calcium";    MolarMass = 40.078 }
      V,  { Symbol = V;  Number = 23; Name = "vanadium";   MolarMass = 50.9415 }
      Cr, { Symbol = Cr; Number = 24; Name = "chromium";   MolarMass = 51.9961 }
      Mn, { Symbol = Mn; Number = 25; Name = "manganese";  MolarMass = 54.938043 }
      Fe, { Symbol = Fe; Number = 26; Name = "iron";       MolarMass = 55.845 }
      Mo, { Symbol = Mo; Number = 42; Name = "molybdenum"; MolarMass = 95.95 }
      W,  { Symbol = W;  Number = 74; Name = "tungsten";   MolarMass = 183.84 } ]
    |> Map.ofList

let private tryGetByString (sym: string) =
    tryParse sym
    |> Option.bind (fun key -> Map.tryFind key elementTable)

let getMolarMassArray (elements: string list) =
    elements
    |> List.choose (fun sym -> tryGetByString sym)
    |> List.map (fun elemData -> elemData.MolarMass)
    |> List.toArray