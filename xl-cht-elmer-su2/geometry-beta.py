# -*- coding: utf-8 -*-
import numpy as np
import majordome_simulation.meshing as ms
from majordome.simulation import GmshOCCModel
from majordome.simulation import GeometricProgression
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
    "Mesh.CharacteristicLengthMin": 0,
    "Mesh.CharacteristicLengthMax": 1e22,
    # "Mesh.ColorCarousel": 2,
    # "Mesh.SurfaceFaces": True,
    # "Mesh.MeshSizeMax": 0.02,
    "Mesh.SaveAll": False,
    "Mesh.SaveGroupsOfNodes": True,
    "Mesh.Algorithm": 5,
    "Mesh.ElementOrder": 1,
}

yaml = YAML()
data = yaml.load(open("dimensioning.yaml"))

m_h = data["m_h"]
D_h = data["D_h"]

center_to_wall = (1 + m_h / 2) * D_h
bnd_thickness = 0.003

R_out = D_h / 2
R_bnd = R_out - 0.003

with GmshOCCModel(render=True, **options) as model:
    hole = ms.CircularCrossSection(
        model              = model,
        radius             = R_out,
        boundary_thickness = bnd_thickness,
        cell_size_boundary = 0.0001,
        cell_size_center   = 0.00050,
        num_points_angular = 10,
    )
    # pts = ms.points_on_circle(R_bnd, num_points=8)

    ring = hole._create_ring_boundary()
    hole._create_polygonal_core(ring.lines_in, ring.points_in)

    # model.hexagon_xy(center_to_wall, rotation=30)
    model.generate_mesh(dim=2)
    # model.synchronize()
