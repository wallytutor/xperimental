# Automatic differentiation in F#

## Level 1

Progressive complexity, starting with forward mode based on dual numbers.
This does not scale well for functions with many inputs, but is a good
starting point for understanding the concepts. Using `type Dual with` allors for organizing the building blocks as logically related extensions to the `Dual` type.

## Level 2

Implements an algebraic type `Expr` for constructing the graph of operations, for which overloads are provided so common operations work associated to the type. For providing a simpler user interface, module `Sym` is introduced.

## Level 3

This level actually is a consolidation of the previous exercises. Here we use SRTP (Statically Resolved Type Parameters) to implement a more general and efficient version of the forward mode, which can be used for both `Dual` and `Expr` types. This allows us to have a single implementation of the gradient function that works for both representations.
