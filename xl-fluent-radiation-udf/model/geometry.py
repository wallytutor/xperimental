# -*- coding: utf-8 -*-
import gmsh


def create_rectangle(model, Lx, Ly):
    model.occ.addRectangle(0, 0, 0, Lx, Ly)
    model.occ.synchronize()


def mesh_structuring(model, nx, ny, coef):
    # Linear spacing along x-axis:
    model.mesh.setTransfiniteCurve(1, nx)
    model.mesh.setTransfiniteCurve(3, nx)

    # Boundary layer resolution along y-axis:
    model.mesh.setTransfiniteCurve(2,  ny, meshType="Bump", coef=coef)
    model.mesh.setTransfiniteCurve(-4, ny, meshType="Bump", coef=coef)

    # Transfinite meshing/rectangles of the surface:
    model.mesh.setTransfiniteSurface(1)
    model.mesh.setRecombine(2, 1)

    model.mesh.generate(2)
    model.occ.synchronize()


def physical_groups(model):
    model.addPhysicalGroup(1, [4], 1, "inlet")
    model.addPhysicalGroup(1, [2], 2, "outlet")
    model.addPhysicalGroup(1, [1], 3, "wall-lower")
    model.addPhysicalGroup(1, [3], 4, "wall-upper")
    model.addPhysicalGroup(2, [1], 5, "fluid")
    model.occ.synchronize()


def main():
    gmsh.initialize()
    gmsh.model.add("rect")

    Lx = 1.0
    Ly = 0.1

    nx = 100
    ny = 21
    coef = 0.1

    create_rectangle(gmsh.model, Lx, Ly)
    mesh_structuring(gmsh.model, nx, ny, coef)
    physical_groups(gmsh.model)

    gmsh.option.setNumber("Mesh.SaveAll", 1)
    gmsh.option.setNumber("Mesh.SaveGroupsOfNodes", 1)
    gmsh.option.setNumber("Mesh.SaveGroupsOfElements", 1)
    gmsh.option.setNumber("Mesh.SaveElementTagType", 2)
    gmsh.option.setNumber("Mesh.Format", 1)

    gmsh.write("geometry.cgns")
    gmsh.fltk.run()
    gmsh.finalize()


if __name__ == "__main__":
    main()
