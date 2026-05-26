# .NET Extras

## Design and Implementation Notes

- All files other than the main `Library.fs` use fully-qualified modules on top level instead of namespace declarations. This allows for avoiding one level of nesting and makes it easier to find the relevant code for each module.

## Requirements

- [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) or later
- (Optional) [Gnuplot](https://www.gnuplot.info/download.html) for plotting results
- (Optional) [Quarto](https://quarto.org/docs/get-started/) for building documentation
- (Optional) [uv](https://docs.astral.sh/uv/) for managing Python dependencies
- (Optional) [Python >3.12](https://www.python.org/downloads/) for building documentation and running samples


## Building and running the code

From the repository root, build the project using the .NET CLI:

```bash
dotnet build
```

In development mode you can run `dotnet restore` before building to ensure all dependencies are up to date and then build without restoring to save time:

```bash
dotnet build --no-restore
```

Tests can be run on a project basis using one of the following:

```bash
dotnet test src/Diffusion/Diffusion.Numerics.Tests.fsproj
dotnet test src/Diffusion/Diffusion.Core.Tests.fsproj
dotnet test src/Diffusion/Diffusion.Slycke.Tests.fsproj
```

Sample programs are located in the `samples/` directory. They are supplied as [Verso Notebooks](https://github.com/DataficationSDK/Verso) and can be run directly from VS Code with the [Verso extension](https://marketplace.visualstudio.com/items?itemName=Datafication.verso-notebook) installed. You can also run the samples from the command line using `dotnet fsi`. This allows you to interact with the code and modify it on the fly by copying snippets and executing them in the F# interactive session. Alternatively, you can export the samples as `.fsx` files and run them directly or with VS Code support.

## Building the documentation

The API documentation is generated using [fsdocs](https://fsprojects.github.io/FSharp.Formatting/) and is available in the `dotnet/` directory. Before building the API documentation, you need to install the `fsdocs-tool` global tool:

```bash
dotnet tool install -g fsdocs-tool
```

Then the documentation do be deployed in a server can be built using the following command:

```bash
fsdocs build --input docs/dotnet --output output
```

For navigating locally you need to use `watch`, as the generated documentation relies on client-side routing which doesn't work properly when opened directly from the file system:

```bash
fsdocs watch --input docs/dotnet
```
