# -*- coding: utf-8 -*-

@(run_on_import := lambda f: f())
def load_project(
        name: str = "xl_database",
        mode: str = "Debug",
        dotnet: str = "net10.0"
    ) -> None:
    """ Helper for getting project paths from notebook. """
    from pathlib import Path
    here = Path(__file__).resolve().parent

    runtime_config = here / "runtime.json"
    assembly_path  = here.parent / f"{name}/bin/{mode}/{dotnet}"

    import sys
    sys.path.insert(0, str(assembly_path))

    import pythonnet
    pythonnet.load("coreclr", runtime_config=str(runtime_config))

    # XXX: this must be imported after loading the assembly!
    import clr
    clr.AddReference(name)


def import_from(module_name, name):
    from importlib import import_module
    module = import_module(module_name)
    return getattr(module, name)


# XXX LiteDB must come after loading the assembly!
XlDatabase     = import_from("xl_database", "XlDatabase")
Equipment      = import_from("xl_database", "Equipment")
AnalysisType   = import_from("xl_database", "AnalysisType")
AnalysisResult = import_from("xl_database", "AnalysisResult")
BsonExpression = import_from("LiteDB",      "BsonExpression")


def expression(text: str, **kwargs) -> object:
    """Create a BsonExpression from a string. """
    return BsonExpression.Create(text, **kwargs)
