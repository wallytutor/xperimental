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
size_d = d / 5
size_g = g / 5
size_m = 0.001
#endregion: parameters

#region: options
options = {
    "General.GraphicsWidth": 1920,
    "General.GraphicsHeight": 1080,
    "Mesh.SurfaceFaces": True,
    "Mesh.MeshSizeMax": 1.5,
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
    #region: geometry 1
    # The overall domain rectangle:
    o1 = (0.0, 0.0, D0)
    r1 = model.add_rectangle(*o1, L, H)

    # The *actual* material inside the gap:
    o2 = (0.0, H - d + g, D0)
    r2 = model.add_rectangle(*o2, w, d - g)

    # The gap zone rectangle:
    o3 = (0.0, H - d, D0)
    r3 = model.add_rectangle(*o3, w, g)

    # Cut the quenched zone out of the overall domain.
    objs, tool = [(2, r1)], [(2, r2), (2, r3)]
    tags, _ = model.fragment(objs, tool, removeObject=1)
    model.synchronize()

    # Create the other half of the mould by symmetry:
    (tag_mould, tag_gap, tag_molten) = tags
    model.transform_symmetrize([tag_mould], axis="y", copy=True)
    tags_mould = model.fuse([tag_mould], [(2, 1+tag_mould[1])])[0][0]
    model.synchronize()

    # Enforce shared nodes between the zones:
    (tag_molten, tag_gap), _   = model.fragment([tag_molten], [tag_gap])
    (tag_gap, tag_mould), _    = model.fragment([tag_gap],    [tag_mould])
    (tag_molten, tag_mould), _ = model.fragment([tag_molten], [tag_mould])
    model.synchronize()
    #endregion: geometry 1

    #region: transfinite surface
    set_transfinite(model, [3, 8], size_g)
    set_transfinite(model, [2, 9, 11], 4*size_g)
    model.set_transfinite_surface(tag_gap[1])
    model.set_recombine(2, tag_gap[1])

    set_progression(model,  4, size_g, size_d)
    set_progression(model, 10, size_d, size_g)
    model.set_transfinite_surface(tag_molten[1])
    model.set_recombine(2, tag_molten[1])

    set_progression(model, 12, size_g, size_m)
    set_progression(model, 17, size_m, size_d)
    # model.generate_mesh(dim=2) # DEBUG
    #endregion: transfinite surface

    #region: geometry 2
    # Create new baseline rectangle for lower body:
    on1 = (0.0, -H, 0.0)
    rn1 = model.add_rectangle(*on1, L, 2*H)
    tags_base = model.extrude([(2, rn1)], 0, 0, D0)
    body_base = tags_base[1]

    # Move the geometry by D0 in the z direction:
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

    # Fuse mould parts:
    outputs = model.fuse([body_mould], [body_inertial, body_base])
    body_mould = outputs[0][0]
    model.synchronize()

    # Enforce shared nodes between the zones:
    model.fragment([body_molten], [body_gap])
    model.fragment([body_gap], [body_mould])
    model.fragment([body_molten], [body_mould])
    model.synchronize()
    #endregion: geometry 2

    #region: base tags
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
    #endregion: base tags

    #region: meshing anisotropy
    # model.generate_mesh(dim=2)
    # n_layers = int((D1+D2)/size_d)

    # model.extrude(
    #     [(2, 44), (2, 49)],
    #     0, 0, D1+D2,
    #     numElements=[n_layers],
    #     recombine=True
    # )

    # model.synchronize()
    #endregion: meshing anisotropy

    #region: meshing 1
    # constant_sizes = {
    #     body_molten[1]: size_d,
    #     body_gap[1]:    size_g,
    #     body_mould[1]:  size_m,
    # }
    # fields_list = []

    # for idx, (body, size) in enumerate(constant_sizes.items(), start=1):
    #      model._mesh.field.add("Constant", idx)
    #      model._mesh.field.setNumbers(idx, "VolumesList", [body])
    #      model._mesh.field.setNumber(idx, "VIn", size)
    #      fields_list.append(idx)

    # field_min = 1 + len(constant_sizes)
    # model._mesh.field.add("Min", field_min)
    # model._mesh.field.setNumbers(field_min, "FieldsList", fields_list)
    # model._mesh.field.setAsBackgroundMesh(field_min)
    # model.synchronize()
    #endregion: meshing 1

    #region: meshing 2
    # model._mesh.field.add("Distance", 1)
    # model._mesh.field.setNumbers(1, "SurfacesList", bc_in_0 + bc_in_1)
    # model._mesh.field.setNumber(1, "Sampling", 1000)

    # model._mesh.field.add("Threshold", 2)
    # model._mesh.field.setNumber(2, "InField", 1)
    # model._mesh.field.setNumber(2, "DistMin", 1*size_g)
    # model._mesh.field.setNumber(2, "DistMax", 2*size_g)
    # model._mesh.field.setNumber(2, "SizeMin", size_g)
    # model._mesh.field.setNumber(2, "SizeMax", size_m)
    # model._mesh.field.setNumber(2, "StopAtDistMax", 1)

    # model._mesh.field.add("Min", 3)
    # model._mesh.field.setNumbers(3, "FieldsList", [2])
    # model._mesh.field.setAsBackgroundMesh(3)
    # model.synchronize()
    #endregion: meshing 2

    #region: tagging
    # bounds = [
    #     {"tags": bc_in_0,      "name": "in_molten_gap",   "tag_id":  1},
    #     {"tags": bc_in_1,      "name": "in_gap_mould",    "tag_id":  2},
    #     {"tags": bc_in_2,      "name": "in_molten_mould", "tag_id":  3},
    #     {"tags": bc_sx_molten, "name": "sx_molten",       "tag_id":  4},
    #     {"tags": bc_sx_gap,    "name": "sx_gap",          "tag_id":  5},
    #     {"tags": bc_sx_mould,  "name": "sx_mould",        "tag_id":  6},
    #     {"tags": bc_sy_molten, "name": "sy_molten",       "tag_id":  7},
    #     {"tags": bc_sy_gap,    "name": "sy_gap",          "tag_id":  8},
    #     {"tags": bc_sy_mould,  "name": "sy_mould",        "tag_id":  9},
    #     {"tags": bc_ex_molten, "name": "ex_molten",       "tag_id": 10},
    #     {"tags": bc_ex_gap,    "name": "ex_gap",          "tag_id": 11},
    #     {"tags": bc_ex_mould,  "name": "ex_mould",        "tag_id": 12},
    # ]

    # zones = [
    #     {"tags": [body_molten[1]], "name": "molten", "tag_id": 1},
    #     {"tags": [body_gap[1]],    "name": "gap",    "tag_id": 2},
    #     {"tags": [body_mould[1]],  "name": "mould",  "tag_id": 3},
    # ]

    # for entry in bounds:
    #     model.add_physical_surface(**entry)

    # for entry in zones:
    #     model.add_physical_volume(**entry)

    # model.synchronize()
    #endregion: tagging


    # model.generate_mesh(dim=3)
    # model.dump(f"geometry.msh")
