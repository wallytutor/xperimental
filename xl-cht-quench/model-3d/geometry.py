# -*- coding: utf-8 -*-
from majordome.simulation import GmshOCCModel
from majordome.simulation import GeometricProgression

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
size_d = d / 3
size_g = g / 3
size_m = H / 4
#endregion: parameters

#region: options
options = {
    "General.GraphicsWidth": 1920,
    "General.GraphicsHeight": 1080,
    # "Mesh.CharacteristicLengthMin": size_g,
    # "Mesh.CharacteristicLengthMax": 2.5,
    "Mesh.SurfaceFaces": True,
    "Mesh.MeshSizeMax": 2.5,
    "Mesh.SaveAll": False,
    "Mesh.SaveGroupsOfNodes": True,
    "Mesh.Algorithm": 5,
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
    model.set_transfinite_curve(tag, nn, "Progression", q)


with GmshOCCModel(name="domain", render=True, **options) as model:
    #region: geometry
    # The overall domain rectangle:
    o1 = (0.0, 0.0, 0.0)
    r1 = model.add_rectangle(*o1, L, H)

    # The quenched zone rectangle:
    o2 = (0.0, H - d + 0, 0.0)
    r2 = model.add_rectangle(*o2, w - 0, d - 0)

    # The *actual* material inside the gap:
    o3 = (0.0, H - d + g, 0.0)
    r3 = model.add_rectangle(*o3, w - g, d - g)

    # Split the quenched zone into the actual material and the gap:
    objs, tool = [(2, r2)], [(2, r3)]
    tags_op1, _ = model.fragment(objs, tool)
    model.synchronize()

    # Cut the quenched zone out of the overall domain.
    # Remove the duplicate molten material zone after the cut.
    objs, tool = [(2, r1)], [tags_op1[0]]
    tags_op2, _ = model.fragment(objs, tool)
    model.remove([tags_op2[-1]])
    model.synchronize()

    # Enforce shared nodes between the zones:
    tags_fr1, _ = model.fragment([tags_op1[1]], [tags_op2[1]])
    tags_fr2, _ = model.fragment([tags_op2[0]], [tags_fr1[1]])
    model.synchronize()

    # Retrieve the tags of the different zones:
    tag_molten = tags_fr1[0]
    tag_gap    = tags_fr2[1]
    tag_mould  = tags_fr2[0]

    # Create the other half of the mould by symmetry:
    model.transform_symmetrize([tag_mould], axis="y", copy=True)
    tags_mould = model.fuse([tag_mould], [(2, 1+tag_mould[1])])[0][0]
    model.fragment([tag_gap], [tag_molten])
    model.synchronize()
    #endregion: geometry

    #region: geometry extension
    # Create new baseline rectangle for lower body:
    on1 = (0.0, -H, 0.0)
    rn1 = model.add_rectangle(*on1, L, 2*H)
    tags_base = model.extrude([(2, rn1)], 0, 0, D0)
    body_base = tags_base[1]

    # Move the geometry by D0 in the z direction:
    model.translate([tag_molten, tag_gap, tag_mould], 0, 0, D0)
    tags_molten = model.extrude([tag_molten], 0, 0, D1+D2)
    tags_gap    = model.extrude([tag_gap],    0, 0, D1+D2)
    tags_mould  = model.extrude([tag_mould],  0, 0, D1+D2)
    body_molten = tags_molten[1]
    body_gap    = tags_gap[1]
    body_mould  = tags_mould[1]

    # Create rectangle for the top inertial zone:
    on2 = (L, -H, D0+D1)
    rn2 = model.add_rectangle(*on2, L_inertial, 2*H)
    tags_inertial = model.extrude([(2, rn2)], 0.0, 0, D2)
    body_inertial = tags_inertial[1]

    # # Fuse mould parts:
    outputs = model.fuse([body_mould], [body_inertial, body_base])
    body_mould = outputs[0][0]

    model.synchronize()
    #endregion: geometry extension

    #region: repair geometry
    model.fragment([body_molten], [body_gap])
    model.fragment([body_gap], [body_mould])
    model.fragment([body_molten], [body_mould])
    model.synchronize()
    #endregion: repair geometry

    #region: meshing
    # # Identify surfaces for boundary conditions:
    bc_in_0      = [40, 41]
    bc_in_1      = [46, 47, 48]
    bc_in_2      = [52]

    bc_sx_molten = [50]
    bc_sx_gap    = [44]
    bc_sx_mould  = [54]

    bc_sy_molten = [51]
    bc_sy_gap    = [45]
    bc_sy_mould  = [59]

    bc_ex_molten = [53]
    bc_ex_gap    = [49]
    bc_ex_mould  = [55, 56, 57, 58, 60, 61, 62, 63, 64]

    constant_sizes = {
        body_molten[1]: size_d,
        body_gap[1]:    size_g,
        body_mould[1]:  size_m,
    }
    fields_list = []

    for idx, (body, size) in enumerate(constant_sizes.items(), start=1):
         model._mesh.field.add("Constant", idx)
         model._mesh.field.setNumbers(idx, "VolumesList", [body])
         model._mesh.field.setNumber(idx, "VIn", size)
         fields_list.append(idx)

    field_min = 1 + len(constant_sizes)
    model._mesh.field.add("Min", field_min)
    model._mesh.field.setNumbers(field_min, "FieldsList", fields_list)
    model._mesh.field.setAsBackgroundMesh(field_min)

    # model._mesh.field.add("Distance", field_gap)
    # model._mesh.field.setNumbers(field_gap, "SurfacesList", bc_in_0 + bc_in_1)
    # model._mesh.field.setNumber(field_gap, "Sampling", 200)

    # model._mesh.field.add("Threshold", field_thresh)
    # model._mesh.field.setNumber(field_thresh, "InField", field_gap)
    # model._mesh.field.setNumber(field_thresh, "DistMin", 2*size_g)
    # model._mesh.field.setNumber(field_thresh, "DistMax", 4*size_g)
    # model._mesh.field.setNumber(field_thresh, "SizeMin", size_g)
    # model._mesh.field.setNumber(field_thresh, "SizeMax", size_m)
    # model._mesh.field.setNumber(field_thresh, "StopAtDistMax", 1)
    model.synchronize()
    #endregion: meshing

    #region: tagging
    bounds = [
        {"tags": bc_in_0,      "name": "in_molten_gap",   "tag_id":  1},
        {"tags": bc_in_1,      "name": "in_gap_mould",    "tag_id":  2},
        {"tags": bc_in_2,      "name": "in_molten_mould", "tag_id":  3},
        {"tags": bc_sx_molten, "name": "sx_molten",       "tag_id":  4},
        {"tags": bc_sx_gap,    "name": "sx_gap",          "tag_id":  5},
        {"tags": bc_sx_mould,  "name": "sx_mould",        "tag_id":  6},
        {"tags": bc_sy_molten, "name": "sy_molten",       "tag_id":  7},
        {"tags": bc_sy_gap,    "name": "sy_gap",          "tag_id":  8},
        {"tags": bc_sy_mould,  "name": "sy_mould",        "tag_id":  9},
        {"tags": bc_ex_molten, "name": "ex_molten",       "tag_id": 10},
        {"tags": bc_ex_gap,    "name": "ex_gap",          "tag_id": 11},
        {"tags": bc_ex_mould,  "name": "ex_mould",        "tag_id": 12},
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

    model.generate_mesh(dim=3)
    model.dump(f"geometry.msh")
