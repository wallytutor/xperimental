# OpenFOAM Dictionary Highlight

Minimal syntax highlighter for OpenFOAM dictionary files in VS Code.

## Supported files

The extension auto-detects these filenames with no manual file association:

- `controlDict`
- `decomposeParDict`
- `fvSolution`

It also includes a broader built-in OpenFOAM filename list (for example `fvSchemes`, `boundary`, `U`, and `p_rgh`) for out-of-the-box highlighting.

## Add custom filenames in settings.json

You can add your own dictionary file names via extension settings:

- Setting key: `openfoamDictHighlight.additionalFilenames`
- Type: array of strings
- Match mode: exact file name match

Example:

```json
{
  "openfoamDictHighlight.additionalFilenames": [
    "mySolverDict",
    "regionProperties",
    "combustionProperties"
  ]
}
```

After changing this setting, the extension reapplies language detection to currently open files.

Alternative fallback (if you prefer native VS Code association behavior) is to use `files.associations`, for example:

```json
{
  "files.associations": {
    "**/mySolverDict": "openfoam-dict"
  }
}
```

## Highlighting rules

- C-style comments:
  - `// line comments`
  - `/* block comments */`
- Delimiters:
  - braces `{}`
  - brackets `[]`
  - parentheses `()`
- Double-quoted strings:
  - `"system"`
- Dictionary entries using OpenFOAM-style space-separated assignment and `;` terminators

## Install locally

### Option 1: Install unpacked extension folder

1. Open VS Code.
2. Open Extensions view.
3. Select `...` (More Actions) -> `Install from Location...`.
4. Pick this folder: `xl-foam-highlight`.

### Option 2: Package as VSIX and install

1. Install `vsce`:

   ```powershell
   npm install -g @vscode/vsce
   ```

2. Package this extension:

   ```powershell
   cd xl-foam-highlight
   vsce package
   ```

3. In VS Code, run `Extensions: Install from VSIX...` and select the generated `.vsix` file.
