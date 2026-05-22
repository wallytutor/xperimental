# -*- coding: utf-8 -*-

import sys
import subprocess

from pathlib import Path
from setuptools import setup
from setuptools.command.build_py import build_py
from setuptools.command.editable_wheel import editable_wheel


def compile_c_library():
    """ Runs your exact native GCC compilation command. """
    print(" === Compiling C Library with GCC via Subprocess ===")

    ext = ".dll" if sys.platform.startswith("win") else ".so"
    here = Path(__file__).parent.resolve()
    output = here / f"src/wsgglib/wsgglib{ext}"

    result = subprocess.run([
        "gcc",
        "-shared",
        "-o",
        str(output),
        "-fPIC",
        "-O2",
        "-Wall",
        "-Wextra",
        "-ansi",
        "-pedantic",
        "src/wsgglib.c"
    ], capture_output=True, text=True)

    if result.returncode != 0:
        print("GCC Compilation Failed!", file=sys.stderr)
        print(result.stderr, file=sys.stderr)
        sys.exit(1)

    print(f"Successfully compiled binary to: {output}")


class CustomBuildPy(build_py):
    def run(self):
        compile_c_library()
        super().run()


class CustomEditableWheel(editable_wheel):
    def run(self):
        compile_c_library()
        super().run()


setup(
    cmdclass={
        "build_py": CustomBuildPy,
        "editable_wheel": CustomEditableWheel,
    },
    package_data={"wsgglib": ["*.dll", "*.so", "*.dylib"]},
)
