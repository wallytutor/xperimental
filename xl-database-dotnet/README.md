# xl-database-dotnet

A minimal .NET project to illustrate how to create a .NET assembly that can be used from Python with Python.NET. This is intended as a demo for the use of .NET assemblies in the context of research projects requiring a simple local database.

Target Windows 11 running from PowerShell.

References:

- [Python.NET documentation](https://pythonnet.github.io/)
- [LiteDB documentation](https://www.litedb.org/docs/)
- [LiteDB Studio](https://github.com/litedb-org/LiteDB.Studio)
- [OpenAPI](https://aka.ms/aspnet/openapi)
- [Ollama](https://github.com/ollama/ollama)

## Creating a similar project

- Project created with the .NET CLI with the following steps:

```bash
# Create a new solution in the current directory:
dotnet new sln -n xl-database-fsharp -o .

# Create a shared library:
dotnet new classlib -o 'xl_database'
dotnet sln add 'xl_database/xl_database.csproj'

# Add the database dependency to the project:
dotnet add package LiteDB --project 'xl_database/xl_database.csproj'
# Manually add the <CopyLocalLockFileAssemblies> property to the above
# project file to ensure that the LiteDB assembly is copied to the
# output directory and you can use it from Python. See file for details.

# Create a web API project to test the assembly:
dotnet new webapi -n 'xl_webapi' --no-https
dotnet add 'xl_webapi/xl_webapi.csproj' `
    reference 'xl_database/xl_database.csproj'
dotnet sln add 'xl_webapi/xl_webapi.csproj'

# Restore dependencies and build:
dotnet restore
dotnet build
```

## Basic usage from Python

- Prepare the interaction through Python with the following steps:

```bash
# Create a virtual environment:
python -m venv scratch/.venv

# Activate the virtual environment:
scratch\.venv\Scripts\Activate.ps1

# Update pip:
python -m pip install --upgrade pip

# Install the Python.NET package:
python -m pip install 'pythonnet>=3.0.0'

# Install the IPython kernel package to enable notebook support:
python -m pip install 'ipykernel'
```

Now you can use the .NET assembly from Python. See the [scratch/connect.qmd](scratch/connect.qmd) file for examples of how to do this.

## Running the web API

It is recommended to use the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension for Visual Studio Code to interact with the web API. You can find example requests in the [xl_webapi/requests.http](xl_webapi/requests.http) file.

1. Start the web API with the following command:

```bash
dotnet run --project xl_webapi
```

2. Open `xl_webapi.http` and click Send Request above any block.


3. Alternatively, open `http://localhost:5031/openapi/v1.json` in your browser to view the OpenAPI specification. You can use this specification to generate client code in various languages such as with PowerShell as illustrated below.

```bash
# Create equipment
Invoke-RestMethod http://localhost:5031/equipment/ -Method Post `
    -ContentType "application/json" `
    -Body '{"name":"SEM","model":"Model XYZ","manufacturer":"SEM Corp","serialNumber":"12345"}'

# List equipment
Invoke-RestMethod http://localhost:5031/equipment/
```

## Development notes

- Objects to be serialized with LiteDB must have a public properties with getters (and optionally setters). Notice that `readonly` properties are not supported.

- To enable type-safe equality checks, we can use a generic interface `IDocument<TSelf>` that inherits from `IDocument` and defines a method `IsSameAs(TSelf item)`. This allows us to implement the method in each document class with the correct type, avoiding the need for type checks and casts. See the [Documents.cs](xl_database/Documents.cs) file for details.
