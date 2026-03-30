# -*- coding: utf-8 -*-
""" Provides a module to copy files from the Ansys installation directory. """
from itertools import chain
from pathlib import Path
import json
import shutil
import sys


def create_lists(fluent_version: Path) -> tuple[list[str], list[str]]:
    """ Creates the lists of files to copy and remove. """
    with open(fluent_version/ "copy_docker_files.json") as fp:
        data = json.load(fp)

    to_cp = [vk for v in data["copy"].values() for vk in v]
    to_rm = [vk for v in data["remove"].values() for vk in v]

    return to_cp, to_rm


def only_copy_new(copy):
    """ Ensures that the file is copied only once. """
    def wrapper(source: Path, destination: Path):
        if not destination.exists():
            copy(source, destination)

    return wrapper


@only_copy_new
def copy_file(source: Path, destination: Path) -> None:
    """ Copies the file to the destination. """
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src=source, dst=destination)


@only_copy_new
def copy_folder(source: Path, destination: Path) -> None:
    """ Copies the folder to the destination. """
    destination.mkdir(parents=True, exist_ok=True)
    shutil.copytree(src=source, dst=destination, dirs_exist_ok=True)


def manage_copy(src: Path, dst: Path, file: str) -> None:
    """ Copies the file or folder to the destination. """
    source = src / file
    destination = dst / file

    if source.is_file():
        copy_file(source, destination)
    elif source.is_dir():
        copy_folder(source, destination)


def manage_remove(dst: Path, file: str) -> None:
    """ Removes the file or folder if it exists. """
    destination = dst / file

    if destination.is_file():
        destination.unlink()

    elif destination.is_dir():
        shutil.rmtree(destination)


def copy_files(src: str) -> None:
    """ Copy files from the Ansys installation directory.

    Parameters
    ----------
    src: str
        Path of ``ansys_inc`` folder in the Ansys installation directory.
    """
    fluent_version = Path(__file__).resolve().parent
    cp_list, rm_list = create_lists(fluent_version)

    src = Path(src).resolve()
    dst = (Path(fluent_version) / "ansys_inc").resolve()

    if not src.is_dir():
        raise FileNotFoundError(f"Ansys directory does not exist at {src}")
    
    if not dst.is_dir():
        dst.mkdir(parents=True, exist_ok=True)

    for file in cp_list:
        manage_copy(src, dst, file)

    for file in rm_list:
        manage_remove(dst, file)


if __name__ == "__main__":
    copy_files(sys.argv[1])
