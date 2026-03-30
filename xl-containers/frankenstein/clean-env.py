# -*- coding: utf-8 -*-

PS1 = "OpenFOAM12> "


class EnvFileParser:
    """ Parser an environmental variables file with processing. """
    def __init__(self, file_path):
        self.file_path = file_path

    def _process_args(self, name, args):
        """ Custom argument processing. """
        match name:
            # TODO add handler for LD_LIBRARY_PATH
            # smear it over multiple lines maybe...
            case "PS1":
                return PS1
            case _:
                return "".join(args).strip("\n")

    def _process_envs(self, name, args):
        """ Compose `name=value` for a given pair. """
        args = self._process_args(name, args)
        return f"{name}='{args}'"

    def _process_line(self, line):
        """ Process a single line of data. """
        if line.startswith("#")              \
           or line.startswith("APPTAINER")   \
           or line.startswith("SINGULARITY") \
           or "=" not in line:
            return

        try:
            values = line.split("=")
            return self._process_envs(values[0], values[1:])
        except Exception as err:
            print(f"While parsing {line}: {err}")
            return

    def __iter__(self):
        """ Open the file for reading. """
        self.file = open(self.file_path, 'r')
        return self

    def __next__(self):
        """ Iterate over file lines. """
        if not (line := self.file.readline()):
            self.file.close()
            raise StopIteration

        return self._process_line(line)


if __name__ == "__main__":
    env_file = "openfoam12-rockylinux9.env"
    parser = EnvFileParser(env_file)

    lines = [line for line in parser if line]

    with open(env_file, "w") as fp:
        fp.write("\n".join(sorted(lines)))
