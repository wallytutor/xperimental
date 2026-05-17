# -*- coding: utf-8 -*-
from pathlib import Path
from ctypes import CDLL
from ctypes import c_int
# from ctypes import

the_dll = Path(__file__).resolve().parent / "clibrary.dll"

lib = CDLL(the_dll)

# XXX: useless actually
# lib.say_hello.argtypes = []
# lib.say_hello.restype = None

# XXX: mandatory
lib.add.argtypes = [c_int, c_int]
lib.add.restype = c_int

lib.say_hello()
print(f"Result: {lib.add(3, 5)}")



