# -*- coding: utf-8 -*-
import numpy as np
import majordome_simulation.meshing as ms
from majordome_simulation.meshing import GmshOCCModel
from majordome_simulation.meshing import RingBuilder
from majordome_simulation.meshing import CircularCrossSection
from ruamel.yaml import YAML
from screeninfo import get_monitors

#region: config
monitor = get_monitors()[0]

options = {
    "General.GraphicsWidth": int(monitor.width*0.8),
    "General.GraphicsHeight": int(monitor.height*0.8),
    "General.Verbosity": 5,
    "Geometry.Points": True,
    "Geometry.Lines": True,
    "Geometry.Surfaces": True,
    "Mesh.CharacteristicLengthMin": 1.0e-05,
    "Mesh.CharacteristicLengthMax": 1.0e-02,
    "Mesh.ColorCarousel": 2,
    "Mesh.SurfaceFaces": True,
    "Mesh.SaveAll": False,
    "Mesh.SaveGroupsOfNodes": True,
    "Mesh.Algorithm": 6,
    "Mesh.ElementOrder": 1,
}
#endregion: config

#region: parameters
yaml = YAML()
data = yaml.load(open("dimensioning.yaml"))

m_h = data["m_h"]
D_h = data["D_h"]

h_h = 1.0

center_to_wall = (1 + m_h) * D_h / 2

num_splits         = 6
num_points_angular = 10
rotation           = 30
n_layers           = int(h_h / (0.1 * D_h))

fluid_bl = 0.003
solid_bl = 0.002
#endregion: parameters

#region: functions
def make_base(model):
    #region: fluid hole
    R_out = D_h / 2

    fluid_hole = CircularCrossSection(
        model              = model,
        radius             = R_out,
        boundary_thickness = fluid_bl,
        cell_size_external = 0.0001,
        cell_size_internal = 0.0005,
        num_points_angular = num_points_angular,
        num_splits         = num_splits,
        core_unstructured  = True,
        core_polygonal     = False,
        radius_fraction    = 0.5,
        rotation           = rotation
    )
    #endregion: fluid hole

    #region: hexagon
    # R_out = R_out + solid_bl
    # bl = solid_bl

    # funcs = RingBuilder.get_progression_callbacks(
    #     model, num_points_angular, bl, 0.001, 0.00005
    # )

    # solid_ring = RingBuilder(
    #     model              = model,
    #     splits             = num_splits,
    #     radius_out         = R_out + solid_bl,
    #     points_in          = fluid_hole.ring.points_out,
    #     lines_in           = fluid_hole.ring.lines_out,
    #     linker_out         = fluid_hole._add_arc,
    #     callback_lines     = funcs[0],
    #     callback_surfaces  = funcs[1],
    #     rotation           = rotation
    # )

    # R_out = 2 * center_to_wall / np.sqrt(3)
    # bl = R_out - solid_bl - D_h / 2

    # arc_len = 2 * np.pi * R_out / num_splits
    # arc_len = arc_len / num_points_angular

    # funcs = RingBuilder.get_progression_callbacks(
    #     model, num_points_angular, bl, arc_len, arc_len
    # )

    # hexagon = RingBuilder(
    #     model              = model,
    #     splits             = num_splits,
    #     radius_out         = R_out,
    #     points_in          = solid_ring.points_out,
    #     lines_in           = solid_ring.lines_out,
    #     callback_lines     = funcs[0],
    #     callback_surfaces  = funcs[1],
    #     rotation           = rotation
    # )
    #endregion: hexagon

    #region: hexagon-alt
    R_out = 2 * center_to_wall / np.sqrt(3)
    bl = R_out - D_h / 2

    funcs = RingBuilder.get_progression_callbacks(
        model, num_points_angular, bl, 0.005, 0.0001
    )

    hexagon = RingBuilder(
        model              = model,
        splits             = num_splits,
        radius_out         = R_out + solid_bl,
        points_in          = fluid_hole.ring.points_out,
        lines_in           = fluid_hole.ring.lines_out,
        callback_lines     = funcs[0],
        callback_surfaces  = funcs[1],
        rotation           = rotation
    )
    #endregion: hexagon-alt

    model.synchronize()
    return fluid_hole.ring, hexagon


def make_this(model, this, other):
    # Remove other bodies:
    model.remove([(2, t) for t in other.surface_tags])

    # Create base surface
    base = [(2, t) for t in this.surface_tags]

    model.generate_mesh(dim=2)
    opts = dict(numElements=[n_layers], recombine=True)
    new_tags = model.extrude(base, 0, 0, h_h, **opts)
    model.synchronize()

    model.generate_mesh(dim=3)
    model.synchronize()
    return new_tags


def make_fluid(model):
    hole, hexagon = make_base(model)
    new_tags = make_this(model, hole, hexagon)

    extruded = ms.get_extrusion_tags(new_tags, 2)
    extruded_super, extruded_ndim = extruded

    fluid  = extruded_super
    outlet = extruded_ndim
    around = [11, 15, 19, 23, 27, 30]
    inlet  = hole.surface_tags

    model.add_physical_groups(
        surfaces=[
            {"tags": inlet,  "name": "fluid_inlet",     "tag_id": 1},
            {"tags": outlet, "name": "fluid_outlet",    "tag_id": 2},
            {"tags": around, "name": "cht_fluid_solid", "tag_id": 3},
        ],
        volumes=[
            {"tags": fluid, "name": "fluid", "tag_id": 1},
        ]
    )

    model.synchronize()


def make_solid(model):
    hole, hexagon = make_base(model)
    new_tags = make_this(model, hexagon, hole)

    extruded = ms.get_extrusion_tags(new_tags, 2)
    extruded_super, extruded_ndim = extruded

    solid    = extruded_super
    bottom   = hexagon.surface_tags
    top      = extruded_ndim
    around   = [15, 19, 23, 27, 31, 35]
    symmetry = [17, 21, 25, 29, 33, 36]

    model.add_physical_groups(
        surfaces=[
            {"tags": bottom,   "name": "solid_bottom",    "tag_id": 4},
            {"tags": top,      "name": "solid_top",       "tag_id": 5},
            {"tags": symmetry, "name": "solid_symmetry",  "tag_id": 6},
            {"tags": around,   "name": "cht_solid_fluid", "tag_id": 7},
        ],
        volumes=[
            {"tags": solid, "name": "solid", "tag_id": 2},
        ]
    )


def make_full(model):
    hole, hexagon = make_base(model)

    # Create base surface
    base  = [(2, t) for t in hole.surface_tags]
    base += [(2, t) for t in hexagon.surface_tags]

    model.generate_mesh(dim=2)
    opts = dict(numElements=[n_layers], recombine=True)
    new_tags = model.extrude(base, 0, 0, h_h, **opts)
    model.synchronize()

    model.generate_mesh(dim=3)
    model.synchronize()

    extruded = ms.get_extrusion_tags(new_tags, 2)
    extruded_super, extruded_ndim = extruded

    fluid    = extruded_super[:7]
    solid    = extruded_super[7:]
    outlet   = extruded_ndim[:7]
    top      = extruded_ndim[7:]
    inlet    = hole.surface_tags
    bottom   = hexagon.surface_tags
    symmetry = [41, 44, 47, 50, 53, 55]

    model.add_physical_groups(
        surfaces=[
            {"tags": inlet,    "name": "fluid_inlet",     "tag_id": 1},
            {"tags": outlet,   "name": "fluid_outlet",    "tag_id": 2},
            {"tags": bottom,   "name": "solid_bottom",    "tag_id": 3},
            {"tags": top,      "name": "solid_top",       "tag_id": 4},
            {"tags": symmetry, "name": "solid_symmetry",  "tag_id": 5},
        ],
        volumes=[
            {"tags": fluid, "name": "fluid", "tag_id": 1},
            {"tags": solid, "name": "solid", "tag_id": 2},
        ]
    )
    model.synchronize()
#endregion: functions

#region: su2
# with GmshOCCModel(render=True, **options) as model:
#     make_fluid(model)
#     model.dump(f"model-su2/mesh_fluid.su2")

# with GmshOCCModel(render=True, **options) as model:
#     make_solid(model)
#     model.dump(f"model-su2/mesh_solid.su2")
#endregion: su2

#region: elmer
elmer = {**options}
elmer["Mesh.ElementOrder"] = 2

with GmshOCCModel(render=True, **elmer) as model:
    make_full(model)
    model.dump(f"model-elmer/mesh.msh")
#endregion: elmer
