---
name: code-python
description: Provides directives for generating and modifying Python code.
---

# Coding in Python

## When to use this skill

- Use this when writing, modifying, and reviewing Python code.

- Do not use this skill for other programming languages.

## How to use this skill

**Important:** scripts outside packages are not meant to be typed and single line (summary) docstrings are expected, overriding what is said below.

### Code style

- Use PEP8 for style guidelines, unless the user explicitly requests otherwise or manually modifies the code for readability. Code line should be wrapped in an 80–character limit.

- There must be a blanck line before flow control statements like `if`, `match`, `for`, `while`, except when they are nested inside another flow control statement, as for example:

```python
for x in [1, 2, 3]:
    if x > 1:
        y = x + 1
```

- Indentation of function arguments and return values must be like this:

```python
def some_function(
        arg1: Any,
        arg2: Any
    ) -> Self:
    ...
```

### Code structure

- Classes use `__slots__` to reduce memory usage and make sure no attribute is missing or created on the fly.

- Type hints must be used and the code must include type annotations for function arguments and return values.

### Documentation

- Functions must be documented using docstrings. The summary line in the docstring should be on the same line as `"""` and there must be at one whitespace character between the `"""` and the summary text.

- Docstrings should be in Numpydoc format. This means that docstrings must include the following sections:
    - Summary (mandatory)
    - Extended summary (optional)
    - Parameters (mandatory for functions)
    - Returns (mandatory for functions)
    - Raises (optional)
    - See also (optional)
    - Notes (optional)

- Docstring lines must be wrapped in an 68–character limit.

- Class initialization `__init__` docstring must be included in the class docstring as usually done (not inside the function itself). The docstring of the methods must be in the methods themselves.
