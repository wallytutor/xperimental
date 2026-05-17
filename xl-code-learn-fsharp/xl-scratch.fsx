
let rec latex = function
    | Const c -> sprintf "%g" c
    | Var x -> x
    | Add(a,b) -> sprintf "%s + %s" (latex a) (latex b)
    | Sub(a,b) -> sprintf "%s - %s" (latex a) (latex b)
    | Mul(a,b) -> sprintf "%s \\cdot %s" (latex a) (latex b)
    | Div(a,b) -> sprintf "\\frac{%s}{%s}" (latex a) (latex b)
    | Sin a -> sprintf "\\sin(%s)" (latex a)
    | Cos a -> sprintf "\\cos(%s)" (latex a)
    | Exp a -> sprintf "e^{%s}" (latex a)
    | Log a -> sprintf "\\log(%s)" (latex a)
