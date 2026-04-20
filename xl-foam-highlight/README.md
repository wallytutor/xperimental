# OpenFOAM Dictionary Highlight

Minimal syntax highlighter for OpenFOAM dictionary files in VS Code.

## Supported files

The extension auto-detects these filenames with no manual file association:

- `controlDict`
- `decomposeParDict`
- `fvSolution`

## Highlighting rules

- C-style comments:
  - `// line comments`
  - `/* block comments */`
- Delimiters:
  - braces `{}`
  - brackets `[]`
  - parentheses `()`
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
