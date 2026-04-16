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
    "General.GraphicsWidth": monitor.width,
    "General.GraphicsHeight": monitor.height,
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


def model_mould(model):
    # Create baseline rectangle for lower body:
    o = (0.0, -H, 0.0)
    r = model.add_rectangle(*o, L, 2*H)
    tags_base = model.extrude([(2, r)], 0, 0, D0)
    body_base = tags_base[1]

    # Create the main body closing section:
    o = (w, -H, D0)
    r = model.add_rectangle(*o, L-w, 2*H)
    tags_close = model.extrude([(2, r)], 0, 0, D1+D2)
    body_close = tags_close[1]

    # Create the main body cross section:
    o = (0.0, -H+d, D0)
    r = model.add_rectangle(*o, w, 2*(H-d))
    tags_cross = model.extrude([(2, r)], 0, 0, D1+D2)
    body_cross = tags_cross[1]

    # Create rectangle for the top inertial zone:
    o2 = (L, -H, D0+D1)
    r2 = model.add_rectangle(*o2, L_inertial, 2*H)
    tags_inertial = model.extrude([(2, r2)], 0.0, 0, D2)
    body_inertial = tags_inertial[1]

    # Fuse mould parts:
    others = [body_close, body_cross, body_inertial]
    outputs = model.fuse([body_base], others)
    return outputs[0][0]


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
    body_mould = model_mould(model)
    model.synchronize()

    #region: meshing 1
    # The *actual* material inside the gap:
    o = (0.0, H - d + g, D0)
    melt = model.add_rectangle(*o, w, d - g)

    # Cut the quenched zone out of the overall domain.
    # objs, tool = [(2, base_melt)],[(2, melt)]
    objs, tool = [body_mould],[(2, melt)]
    tags, _ = model.fragment(objs, tool, removeTool=1)
    model.synchronize()

    # TODO try to get from `tags` instead of hardcoding
    # Get tags for the bases of the zones:
    base_molten = 15
    base_gap    = 22

    # Sides of gap zone:
    set_transfinite(model, [11, 29], size_g)

    # Width of the gap/molten zone:
    # set_transfinite(model, [27, 28, 30], size_d)
    width_char = 3 * size_g
    set_progression(model, 28, size_d, width_char)
    set_progression(model, 27, width_char, size_d)
    set_progression(model, 30, width_char, size_d)

    # Sides of molten zone:
    set_progression(model, 10, size_d, size_g)
    set_progression(model, 37, size_d, size_g)

    # Make surfaces transfinite and recombine:
    model.set_transfinite_surface(base_gap)
    model.set_transfinite_surface(base_molten)
    model.set_recombine(2, base_gap)
    model.set_recombine(2, base_molten)

    # Generate base meshes for the zones:
    model.generate_mesh(dim=2)
    model.synchronize()
    #endregion: meshing 1

    #region: meshing 2
    # Extrude the zones to create the 3D geometry:
    h_layers = D1 + D2
    n_layers = int(h_layers / width_char)
    opts = dict(numElements=[n_layers], recombine=True)
    tags_molten = model.extrude([(2, base_molten)], 0, 0, h_layers, **opts)
    tags_gap    = model.extrude([(2, base_gap)],    0, 0, h_layers, **opts)
    body_molten = tags_molten[1]
    body_gap    = tags_gap[1]
    model.synchronize()

    # Enforce shared nodes between the zones:
    (body_molten, body_gap), _   = model.fragment([body_molten], [body_gap])
    (body_gap,    body_mould), _ = model.fragment([body_gap],    [body_mould])
    (body_molten, body_mould), _ = model.fragment([body_molten], [body_mould])
    model.synchronize()

    field_cons = 1
    body = body_mould[1]
    size = size_m
    model._mesh.field.add("Constant", field_cons)
    model._mesh.field.setNumbers(field_cons, "VolumesList", [body])
    model._mesh.field.setNumber(field_cons, "VIn", size)

    field_dist = 2
    model._mesh.field.add("Distance", field_dist)
    model._mesh.field.setNumbers(field_dist, "SurfacesList", [35, 36, 40])
    model._mesh.field.setNumber(field_dist, "Sampling", 1000)

    field_thre = 3
    model._mesh.field.add("Threshold", field_thre)
    model._mesh.field.setNumber(field_thre, "InField", field_dist)
    model._mesh.field.setNumber(field_thre, "DistMin",  2*size_g)
    model._mesh.field.setNumber(field_thre, "DistMax", 2*width_char)
    model._mesh.field.setNumber(field_thre, "SizeMin", width_char)
    model._mesh.field.setNumber(field_thre, "SizeMax", size_m)
    # model._mesh.field.setNumber(field_thre, "StopAtDistMax", 1)

    field_min = 4
    field_list = [field_cons, field_thre]
    model._mesh.field.add("Min", field_min)
    model._mesh.field.setNumbers(field_min, "FieldsList", field_list)
    model._mesh.field.setAsBackgroundMesh(field_min)
    model.synchronize()
    #endregion: meshing 2

    #region: tagging
    # Identify surfaces for boundary conditions:
    bc_sx_gap    = [37]
    bc_sx_molten = [41]
    bc_sx_mould  = [44]
    bc_sy_molten = [39]
    bc_sy_mould  = [48]
    bc_ex_gap    = [38]
    bc_ex_molten = [42]
    bc_ex_mould  = [43, 45, 46, 47, 49, 50, 51, 52, 53]

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
        {"tags": [body_mould[1]],  "name": "mould",  "tag_id": 3},
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