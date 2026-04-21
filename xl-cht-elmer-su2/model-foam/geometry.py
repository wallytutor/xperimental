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

#region: refinement controls
# Number of points along the angular direction of the fluid hole and hexagon:
num_points_angular = 6

# Fluid boundary layer thickness and cell sizes:
fluid_bl_tot       = 0.003
fluid_bl_ext       = 0.0001
fluid_bl_int       = 0.0006

# Solid boundary layer cell sizes:
solid_bl_ext       = 0.0050
solid_bl_int       = 0.0001

# Relative layer thickness (w.r.t D_h) for the extrusion direction:
rel_layer          = 0.15
#endregion: refinement controls

#region: parameters
yaml = YAML()
data = yaml.load(open("../dimensioning.yaml"))

m_h = data["m_h"]
D_h = data["D_h"]
h_t = data["h_t"]

# Six-fold symmetry of the system:
num_splits = 6

# Rotate so that face of the hexagon is aligned with the x-axis:
rotation   = 30

# Number of layers in the extrusion direction:
n_layers   = int(h_t / (rel_layer * D_h))

# Apothem of the hexagon, i.e. distance from center to face:
center_to_wall = (1 + m_h) * D_h / 2

# Outer radius of the hexagon and fluid hole:
R_out_solid = 2 * center_to_wall / np.sqrt(3)
R_out_fluid = D_h / 2
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
        core_unstructured  = False,
        core_polygonal     = True,
        radius_fraction    = 0.5,
        rotation           = rotation
    )

    hole = fluid_hole.ring
    core = fluid_hole.core
    model.synchronize()
    #endregion: fluid hole

    #region: hexagon
    solid_bl_tot = R_out_solid - R_out_fluid

    callback_lines, callback_surfaces = RingBuilder.get_progression_callbacks(
        model, num_points_angular, solid_bl_tot, solid_bl_ext, solid_bl_int)

    hexa = RingBuilder(
        model              = model,
        splits             = num_splits,
        radius_out         = R_out_solid,
        points_in          = fluid_hole.ring.points_out,
        lines_in           = fluid_hole.ring.lines_out,
        callback_lines     = callback_lines,
        callback_surfaces  = callback_surfaces,
        rotation           = rotation
    )
    model.synchronize()
    #endregion: hexagon

    #region: create volume
    # TODO add API to access this:
    p_orig = fluid_hole._p_origin

    p0 = core.points_in[-1]
    p1 = core.points_in[0]

    l0 = core.lines_in[-1]
    l1 = model.add_line(p1, p_orig)
    l2 = model.add_line(p_orig, p0)

    loop = model.add_curve_loop([l0, l1, l2])
    surf = model.add_plane_surface([loop])
    model.synchronize()

    base = [(2, surf),
            (2, core.surface_tags[-1]),
            (2, hole.surface_tags[-1]),
            (2, hexa.surface_tags[-1])]

    removes  = core.surface_tags[:-1]
    removes += hole.surface_tags[:-1]
    removes += hexa.surface_tags[:-1]
    model.remove([(2, item) for item in removes])
    model.synchronize()

    model.set_transfinite_curve(l1, 5)
    model.set_transfinite_curve(l2, 5)

    opts = dict(numElements=[n_layers], recombine=True)
    new_tags = model.extrude(base, 0, 0, h_t, **opts)
    model.synchronize()
    #endregion: create volume

    #region: physical groups
    extruded = ms.get_extrusion_tags(new_tags, 2)
    extruded_super, extruded_ndim = extruded

    fluid      = extruded_super[:3]
    outlet     = extruded_ndim[:3]
    solid      = [extruded_super[3]]
    top        = [extruded_ndim[3]]
    inlet      = [s[1] for s in base[:3]]
    bottom     = [base[3][1]]
    symmetry   = [34]
    fluidLeft  = [21, 25, 29]
    fluidRight = [22, 24, 28]
    solidLeft  = [33]
    solidRight = [32]

    model.add_physical_groups(
        surfaces=[
            {"tags": inlet,      "name": "fluidInlet",    "tag_id": 10},
            {"tags": outlet,     "name": "fluidOutlet",   "tag_id": 20},
            {"tags": bottom,     "name": "solidBottom",   "tag_id": 30},
            {"tags": top,        "name": "solidTop",      "tag_id": 40},
            {"tags": symmetry,   "name": "solidSymmetry", "tag_id": 50},
            {"tags": fluidLeft,  "name": "fluidLeft",     "tag_id": 60},
            {"tags": fluidRight, "name": "fluidRight",    "tag_id": 61},
            {"tags": solidLeft,  "name": "solidLeft",     "tag_id": 70},
            {"tags": solidRight, "name": "solidRight",    "tag_id": 71},
        ],
        volumes=[
            {"tags": fluid, "name": "fluid", "tag_id": 100},
            {"tags": solid, "name": "solid", "tag_id": 200},
        ]
    )

    model.synchronize()
    #endregion: physical groups

    #region: mesh
    model.generate_mesh(dim=2)
    model.generate_mesh(dim=3)
    model.synchronize()
    model.dump("mesh.msh")
    #endregion: mesh
