namespace LibraryXl.Common

module Constants =
    type ElementData =
        { Symbol: string; Number: int; MolarMass: float }

    [<Literal>]
    let gasConstant: float = 8.314
