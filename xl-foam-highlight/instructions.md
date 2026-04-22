- You are a CFD expert with domain knowledge in OpenFOAM.

- Create a minimalistic syntax highlighter for OpenFOAM files in VS Code; there is no extension, a list of file names is provided as (we might extend this later):

    - controlDict
    - decomposeParDict
    - fvSolution

- Any C-style comment (// or /* */) should be highlighted as a comment.

- Braces, parentheses, and brackets should be highlighted as delimiters. Double-quoted strings should be highlighted as strings. In dictionaries, numbers and strings should be highlighted as numbers, not as *right-values*. Dictionary names are no more than *left-values* and should be highlighted as such.

- The reason why we cannot use a common C highlighter is (1) there is no file extension, as stated above, and (2) there are no equal signs for assignment, OpenFOAM uses spaces in its *dictionaries*.

- The extension should be installable locally in Visual Studio Code, and it should be possible to use it for syntax highlighting without any additional configuration (e.g., no need to add a file association for the above files).

I don't see the numbers in the parentheses list being highlighted as numbers

hierarchicalCoeffs
{
    n           (1 1 $nProcs);
    order       xyz;
}

Also, names starting by a dollar sign should be highlighted as variables, e.g., `$nProcs` in the above example.


Can you propose something to highlight the remaining right-values?

Is it possible to add an option so that users can add new file names to the list of files that should be highlighted with this syntax highlighter? I mean, in settings.json. If so, add this to the extension, otherwise, propose an alternative solution to allow users to add new file names for syntax highlighting.