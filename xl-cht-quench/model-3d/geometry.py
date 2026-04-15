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
size_d = d / 4
size_g = g / 3
size_m = 0.001
#endregion: parameters

#region: options
options = {
    "Mesh.CharacteristicLengthMin": size_g,
    "Mesh.CharacteristicLengthMax": 1.0,
    "Mesh.MeshSizeMax": 1.2,
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
    model.synchronize()
    #endregion: repair geometry

    #region: meshing
    # Identify surfaces for boundary conditions:
    bc_in_0      = [40, 41]
    bc_in_1      = [46, 47, 48]
    bc_in_2      = [42]
    bc_sx_molten = [38]
    bc_sx_gap    = [44]
    bc_sx_mould  = [50]
    bc_sy_molten = [39]
    bc_sy_gap    = [45]
    bc_sy_mould  = [56]
    bc_ex_molten = [43]
    bc_ex_gap    = [49]
    bc_ex_mould  = [51, 52, 54, 55, 57, 58, 59, 60, 61]

    # set_transfinite(model, bc_i_0, size_g)
    # set_transfinite(model, bc_i_1, size_g)
    # set_transfinite(model, bc_l_gap, size_g)
    # set_transfinite(model, bc_t_gap, size_g)
    # set_progression(model, bc_l_molten[0], size_d, size_g)
    # set_progression(model, bc_t_molten[0], size_g, size_d)
    # set_progression(model, bc_l_mould[0], size_g, size_m)
    # set_progression(model, bc_t_mould[0], size_m, size_g)
    # set_transfinite(model, bc_e_mould, size_m)
    model.synchronize()

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
    #endregion: meshing

    # model.generate_mesh(dim=2)
    # model.dump(f"geometry.msh")
