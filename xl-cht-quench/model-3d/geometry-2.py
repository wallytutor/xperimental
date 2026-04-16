# -*- coding: utf-8 -*-
from majordome.simulation import GmshOCCModel
from majordome.simulation import GeometricProgression
from screeninfo import get_monitors

#region: parameters
# Depth of extrusion
D0 = 0.010
D1 = 0.120
D2 = 0.030

# Width of the domain
L = 0.075

# Width of the inertial zone
L_inertial = 0.050

# Height of the domain
H = 0.010

# Width of the molten zone
w = 0.065

# Thickness of the quenched zone
d = 0.0020

# Gap between the quenched zone and the mould
g = 0.27e-03

# Characteristic size for the mesh elements:
size_d = d / 5
size_g = g / 5
size_m = 0.001
#endregion: parameters

#region: options
monitor = get_monitors()[0]

options = {
    "General.GraphicsWidth": int(monitor.width*0.8),
    "General.GraphicsHeight": int(monitor.height*0.8),
    "Mesh.ColorCarousel": 2,
    "Mesh.SurfaceFaces": True,
    "Mesh.MeshSizeMax": 2*size_m,
    "Mesh.SaveAll": False,
    "Mesh.SaveGroupsOfNodes": True,
    "Mesh.Algorithm": 6,
    "Mesh.ElementOrder": 2,
    "Geometry.Points": False,
    "Geometry.Lines": True,
    "Geometry.Surfaces": True,
}
#endregion: options

def set_transfinite(model, tagset, charlen):
    for tag in tagset:
        nn = int(1 + model.get_length(tag) / charlen)
        model.set_transfinite_curve(tag, nn)


def set_progression(model, tag, d0, d1):
    dl = model.get_length(tag)
    nn, q = GeometricProgression.fit(dl, d0, d1)
    print(f"Progression for tag {tag}: n={nn}, q={q}")
    model.set_transfinite_curve(tag, nn, "Progression", q)


with GmshOCCModel(name="domain", render=True, **options) as model:
    #region: geometry base
    orig_base  = (0.0, -H, 0.0)
    orig_melt  = (0.0, H - d + g, D0)
    orig_gap   = (0.0, H - d, D0)
    orig_mould = (0.0, -H + d, D0)
    orig_side  = (w, -H, D0)

    id_melt  = model.add_rectangle(*orig_melt, w, d - g)
    id_gap   = model.add_rectangle(*orig_gap, w, g)
    id_mould = model.add_rectangle(*orig_mould, w, 2*(H-d))
    id_side  = model.add_rectangle(*orig_side, L-w, 2*H)

    r = model.add_rectangle(*orig_base, L, 2*H)
    tags_base = model.extrude([(2, r)], 0, 0, D0)
    model.synchronize()

    id_melt   = (2, id_melt)
    id_gap    = (2, id_gap)
    id_mould  = (2, id_mould)
    id_side   = (2, id_side)
    body_base = tags_base[1]

    tmp, _   = model.fragment([body_base], [id_melt],  removeTool=1)
    tmp, _   = model.fragment([body_base], [id_gap],   removeTool=1)
    tmp, _   = model.fragment([body_base], [id_mould], removeTool=1)
    tmp, _   = model.fragment([body_base], [id_side],  removeTool=1)
    model.synchronize()
    #endregion: geometry base

    #region: meshing 1
    # TODO try to get from `tmp` instead of hardcoding
    # Get tags for the bases of the zones:
    base_molten = 13
    base_gap    = 10
    base_mould  = 11
    base_side   = 4

    # Sides of gap zone:
    set_transfinite(model, [15, 19], size_g)

    # Width of the gap/molten zone:
    width_char = 3 * size_g
    set_progression(model, 22, size_d, width_char) # mould-env
    set_progression(model, 20, size_d, width_char) # mould-gap
    set_progression(model, 18, size_d, width_char) # gap-melt
    set_progression(model, 12, width_char, size_d) # melt-sym

    # Sides of molten zone:
    set_progression(model, 14, size_d, size_g) # left
    set_progression(model, 24, size_g, size_d) # right

    # Sides of mould zone:
    set_progression(model, 16, width_char, size_m) # left
    set_progression(model, 21, size_m, width_char) # right

    # # Make surfaces transfinite and recombine:
    model.set_transfinite_surface(base_gap)
    model.set_transfinite_surface(base_molten)
    model.set_transfinite_surface(base_mould)
    model.set_recombine(2, base_gap)
    model.set_recombine(2, base_molten)
    model.set_recombine(2, base_mould)

    # Generate base meshes for the zones:
    model.generate_mesh(dim=2)
    model.synchronize()
    #endregion: meshing 1

    #region: meshing 2
    # Extrude the zones to create the 3D geometry:
    h_layers = D1 + D2
    n_layers = int(h_layers / (2 * width_char))
    opts = dict(numElements=[n_layers], recombine=True)

    tags_molten = model.extrude([(2, base_molten)], 0, 0, h_layers, **opts)
    tags_gap    = model.extrude([(2, base_gap)],    0, 0, h_layers, **opts)
    tags_mould  = model.extrude([(2, base_mould)],  0, 0, h_layers, **opts)

    body_molten = tags_molten[1]
    body_gap    = tags_gap[1]
    body_mould  = tags_mould[1]
    model.synchronize()

    # Create rectangle for the top inertial zone:
    tags_side   = model.extrude([(2, base_side)],   0, 0, h_layers)
    body_side   = tags_side[1]
    model.synchronize()

    orig_inertial = (L, -H, D0 + D1)
    id_inertial   = model.add_rectangle(*orig_inertial, L_inertial, 2*H)
    tags_inertial = model.extrude([(2, id_inertial)], 0.0, 0, D2)
    body_inertial = tags_inertial[1]
    model.synchronize()

    model.fragment([body_side],  [body_molten])
    model.fragment([body_side],  [body_gap])
    model.fragment([body_side],  [body_mould])
    model.fragment([body_side],  [body_base])
    model.fragment([body_side],  [body_inertial])

    model.fragment([body_base],  [body_molten])
    model.fragment([body_base],  [body_gap])
    model.fragment([body_base],  [body_mould])
    model.synchronize()
    #endregion: meshing 2

    #region: meshing 3
    field_dist = 1
    model._mesh.field.add("Distance", field_dist)
    model._mesh.field.setNumbers(field_dist, "SurfacesList", [73, 80, 79])
    model._mesh.field.setNumber(field_dist, "Sampling", 100)

    field_thre = 2
    model._mesh.field.add("Threshold", field_thre)
    model._mesh.field.setNumber(field_thre, "InField", field_dist)
    model._mesh.field.setNumber(field_thre, "DistMin",  2*size_g)
    model._mesh.field.setNumber(field_thre, "DistMax", 2*width_char)
    model._mesh.field.setNumber(field_thre, "SizeMin", width_char)
    model._mesh.field.setNumber(field_thre, "SizeMax", size_m)
    # model._mesh.field.setNumber(field_thre, "StopAtDistMax", 1)

    field_min = 3
    field_list = [field_thre]
    model._mesh.field.add("Min", field_min)
    model._mesh.field.setNumbers(field_min, "FieldsList", field_list)
    model._mesh.field.setAsBackgroundMesh(field_min)
    model.synchronize()
    #endregion: meshing 3

    #region: tagging
    # Identify surfaces for boundary conditions:
    bc_sx_gap    = [65]
    bc_sx_molten = [51]
    bc_sx_mould  = [77, 8]

    bc_sy_molten = [52]
    bc_sy_mould  = [7, 81, 89]

    bc_ex_gap    = [66]
    bc_ex_molten = [54]
    bc_ex_mould  = [5, 6, 9, 12, 76,  78, 82, 84, 85, 86, 87, 88, 90,  91]

    mould = [body_mould[1], body_side[1], body_base[1], body_inertial[1]]

    bounds = [
        {"tags": bc_sx_gap,    "name": "sx_gap",          "tag_id":  1},
        {"tags": bc_sx_molten, "name": "sx_molten",       "tag_id":  2},
        {"tags": bc_sx_mould,  "name": "sx_mould",        "tag_id":  3},
        {"tags": bc_sy_molten, "name": "sy_molten",       "tag_id":  4},
        {"tags": bc_sy_mould,  "name": "sy_mould",        "tag_id":  5},
        {"tags": bc_ex_molten, "name": "ex_molten",       "tag_id":  6},
        {"tags": bc_ex_gap,    "name": "ex_gap",          "tag_id":  7},
        {"tags": bc_ex_mould,  "name": "ex_mould",        "tag_id":  8},
    ]

    zones = [
        {"tags": [body_molten[1]], "name": "molten", "tag_id": 1},
        {"tags": [body_gap[1]],    "name": "gap",    "tag_id": 2},
        {"tags": mould,            "name": "mould",  "tag_id": 3},
    ]

    for entry in bounds:
        model.add_physical_surface(**entry)

    for entry in zones:
        model.add_physical_volume(**entry)

    model.synchronize()
    #endregion: tagging

    model.generate_mesh(dim=2)
    model.generate_mesh(dim=3)
    model.dump(f"geometry.msh")