# -*- coding: utf-8 -*-
import numpy as np
import majordome_simulation.meshing as ms
from majordome_simulation.meshing import GmshOCCModel
from majordome_simulation.meshing import GeometricProgression
from majordome_simulation.meshing import RingBuilder
from majordome_simulation.meshing import CircularCrossSection
from ruamel.yaml import YAML
from screeninfo import get_monitors

monitor = get_monitors()[0]

options = {
    "General.GraphicsWidth": int(monitor.width*0.8),
    "General.GraphicsHeight": int(monitor.height*0.8),
    "General.Verbosity": 5,
    "Geometry.Points": True,
    "Geometry.Lines": True,
    "Geometry.Surfaces": True,
    "Mesh.CharacteristicLengthMin": 0.00,
    "Mesh.CharacteristicLengthMax": 0.01,
    "Mesh.ColorCarousel": 2,
    "Mesh.SurfaceFaces": False,
    "Mesh.SaveAll": False,
    "Mesh.SaveGroupsOfNodes": True,
    "Mesh.Algorithm": 5,
    "Mesh.ElementOrder": 1,
}

yaml = YAML()
data = yaml.load(open("dimensioning.yaml"))

m_h = data["m_h"]
D_h = data["D_h"]

center_to_wall = (1 + m_h) * D_h / 2

num_splits         = 6
num_points_angular = 10
rotation           = 30

fluid_bl = 0.005
solid_bl = 0.003

with GmshOCCModel(render=True, **options) as model:
    #region: fluid hole
    R_out = D_h / 2

    fluid_hole = ms.CircularCrossSection(
        model              = model,
        radius             = R_out,
        boundary_thickness = fluid_bl,
        cell_size_external = 0.0001,
        cell_size_internal = 0.0010,
        num_points_angular = num_points_angular,
        num_splits         = num_splits,
        core_polygonal     = True,
        radius_fraction    = 0.5,
        rotation           = rotation
    )
    #endregion: fluid hole

    #region: solid ring
    R_out = R_out + solid_bl
    bl = solid_bl

    funcs = RingBuilder.get_progression_callbacks(
        model, num_points_angular, bl, 0.001, 0.0001
    )

    solid_ring = RingBuilder(
        model              = model,
        splits             = num_splits,
        radius_out         = R_out + solid_bl,
        points_in          = fluid_hole.ring.points_out,
        lines_in           = fluid_hole.ring.lines_out,
        linker_out         = fluid_hole._add_arc,
        callback_lines     = funcs[0],
        callback_surfaces  = funcs[1],
        rotation           = rotation
    )
    #endregion: solid ring

    #region: hexagon
    R_out = 2 * center_to_wall / np.sqrt(3)
    bl = R_out - solid_bl - D_h / 2

    arc_len = 2 * np.pi * R_out / num_splits
    arc_len = arc_len / num_points_angular

    funcs = RingBuilder.get_progression_callbacks(
        model, num_points_angular, bl, arc_len, arc_len
    )

    solid_ring = RingBuilder(
        model              = model,
        splits             = num_splits,
        radius_out         = R_out,
        points_in          = solid_ring.points_out,
        lines_in           = solid_ring.lines_out,
        callback_lines     = funcs[0],
        callback_surfaces  = funcs[1],
        rotation           = rotation
    )
    # model.hexagon_xy(center_to_wall, rotation=30)
    #endregion: hexagon

    model.synchronize()
    model.generate_mesh(dim=2)
