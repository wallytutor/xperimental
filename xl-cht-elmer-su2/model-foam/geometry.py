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
    "Mesh.MshFileVersion": 2.2
}
#endregion: config

#region: parameters
yaml = YAML()
data = yaml.load(open("../dimensioning.yaml"))

m_h = data["m_h"]
D_h = data["D_h"]
h_h = 1.0

center_to_wall = (1 + m_h) * D_h / 2

num_splits         = 6
num_points_angular = 10
rotation           = 30
n_layers           = int(h_h / (0.1 * D_h))

R_out_fluid = D_h / 2
R_out_solid = 2 * center_to_wall / np.sqrt(3)

fluid_bl_tot = 0.003
fluid_bl_ext = 0.0001
fluid_bl_int = 0.0005

solid_bl_tot = R_out_solid - R_out_fluid
solid_bl_ext = 0.0050
solid_bl_int = 0.0001
#endregion: parameters

with GmshOCCModel(render=True, **options) as model:
    #region: fluid hole
    fluid_hole = CircularCrossSection(
        model              = model,
        radius             = R_out_fluid,
        boundary_thickness = fluid_bl_tot,
        cell_size_external = fluid_bl_ext,
        cell_size_internal = fluid_bl_int,
        num_points_angular = num_points_angular,
        num_splits         = num_splits,
        core_unstructured  = True,
        core_polygonal     = False,
        radius_fraction    = 0.5,
        rotation           = rotation
    )

    hole = fluid_hole.ring
    model.synchronize()
    #endregion: fluid hole

    #region: hexagon-alt
    funcs = RingBuilder.get_progression_callbacks(
        model, num_points_angular, solid_bl_tot, solid_bl_ext, solid_bl_int)

    hexagon = RingBuilder(
        model              = model,
        splits             = num_splits,
        radius_out         = R_out_solid,
        points_in          = fluid_hole.ring.points_out,
        lines_in           = fluid_hole.ring.lines_out,
        callback_lines     = funcs[0],
        callback_surfaces  = funcs[1],
        rotation           = rotation
    )
    model.synchronize()
    #endregion: hexagon-alt

    #region: create volume
    base  = [(2, t) for t in hole.surface_tags]
    base += [(2, t) for t in hexagon.surface_tags]

    opts = dict(numElements=[n_layers], recombine=True)
    new_tags = model.extrude(base, 0, 0, h_h, **opts)
    model.synchronize()
    #endregion: create volume

    #region: physical groups
    extruded = ms.get_extrusion_tags(new_tags, 2)
    extruded_super, extruded_ndim = extruded

    fluid    = extruded_super[:7]
    solid    = extruded_super[7:]
    outlet   = extruded_ndim[:7]
    top      = extruded_ndim[7:]
    inlet    = hole.surface_tags
    bottom   = hexagon.surface_tags
    symmetry = [41, 44, 47, 50, 53, 55]
    # coupled  = [17, 21, 25, 29, 33, 36]

    model.add_physical_groups(
        surfaces=[
            {"tags": inlet,    "name": "fluidInlet",     "tag_id": 10},
            {"tags": outlet,   "name": "fluidOutlet",    "tag_id": 20},
            {"tags": bottom,   "name": "solidBottom",    "tag_id": 30},
            {"tags": top,      "name": "solidTop",       "tag_id": 40},
            {"tags": symmetry, "name": "solidSymmetry",  "tag_id": 50},
        ],
        volumes=[
            {"tags": fluid, "name": "fluid", "tag_id": 100},
            {"tags": solid, "name": "solid", "tag_id": 200},
        ]
    )
    model.synchronize()
    #endregion: physical groups

    model.generate_mesh(dim=2)
    model.generate_mesh(dim=3)
    model.synchronize()
    model.dump(f"mesh.msh")
