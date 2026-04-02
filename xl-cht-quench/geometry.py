# -*- coding: utf-8 -*-
from majordome.simulation import GmshOCCModel
from majordome.simulation import GeometricProgression

#region: parameters
# Width of the domain
L = 0.075

# Height of the domain
H = 0.010

# Width of the molten zone
w = 0.065

# Thickness of the quenched zone
d = 0.0020

# Gap between the quenched zone and the mould
g = 0.0002

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

    #region: meshing
    # Identify lines for boundary conditions:
    bc_i_0      = [6, 7]
    bc_i_1      = [14, 15]
    bc_l_molten = [11]
    bc_l_gap    = [21]
    bc_l_mould  = [27]
    bc_t_molten = [12]
    bc_t_gap    = [22]
    bc_t_mould  = [29]
    bc_e_mould  = [24, 25, 26, 28]

    set_transfinite(model, bc_i_0, size_g)
    set_transfinite(model, bc_i_1, size_g)
    set_transfinite(model, bc_l_gap, size_g)
    set_transfinite(model, bc_t_gap, size_g)
    set_progression(model, bc_l_molten[0], size_d, size_g)
    set_progression(model, bc_t_molten[0], size_g, size_d)
    set_progression(model, bc_l_mould[0], size_g, size_m)
    set_progression(model, bc_t_mould[0], size_m, size_g)
    set_transfinite(model, bc_e_mould, size_m)
    model.synchronize()

    bounds = [
        {"tags": bc_i_0,      "name": "i_molten_gap", "tag_id": 1},
        {"tags": bc_i_1,      "name": "i_gap_mould",  "tag_id": 2},
        {"tags": bc_l_molten, "name": "l_molten",     "tag_id": 3},
        {"tags": bc_l_gap,    "name": "l_gap",        "tag_id": 4},
        {"tags": bc_l_mould,  "name": "l_mould",      "tag_id": 5},
        {"tags": bc_t_molten, "name": "t_molten",     "tag_id": 6},
        {"tags": bc_t_gap,    "name": "t_gap",        "tag_id": 7},
        {"tags": bc_t_mould,  "name": "t_mould",      "tag_id": 8},
        {"tags": bc_e_mould,  "name": "e_mould",      "tag_id": 9},
    ]

    zones = [
        {"tags": [tag_molten[1]], "name": "molten", "tag_id": 1},
        {"tags": [tag_gap[1]],    "name": "gap",    "tag_id": 2},
        {"tags": [tag_mould[1]],  "name": "mould",  "tag_id": 3},
    ]

    for entry in bounds:
        model.add_physical_curve(**entry)

    for entry in zones:
        model.add_physical_surface(**entry)

    model.synchronize()
    #endregion: meshing

    model.generate_mesh(dim=2)
    model.dump(f"geometry.msh")
