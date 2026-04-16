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
size_d = d / 4
size_g = g / 4
size_m = 0.001
#endregion: parameters

#region: options
monitor = get_monitors()[0]

options = {
    "General.GraphicsWidth": monitor.width,
    "General.GraphicsHeight": monitor.height,
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
    set_transfinite(model, [11, 29], size_g) # B

    # Width of the gap/molten zone:
    set_transfinite(model, [27, 28, 30], 2*size_d) # B

    # Sides of molten zone:
    set_progression(model, 10, size_d, size_g) # B
    set_progression(model, 37, size_d, size_g) # B

    # Make surfaces transfinite and recombine:
    model.set_transfinite_surface(base_gap)
    model.set_transfinite_surface(base_molten)
    model.set_recombine(2, base_gap)
    model.set_recombine(2, base_molten)

    # Generate base meshes for the zones:
    model.generate_mesh(dim=2)
    model.synchronize()

    # Extrude the zones to create the 3D geometry:
    h_layers = D1 + D2
    n_layers = int(h_layers / (2*size_d))
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

    idx = 1
    body = body_mould[1]
    size = size_m
    model._mesh.field.add("Constant", idx)
    model._mesh.field.setNumbers(idx, "VolumesList", [body])
    model._mesh.field.setNumber(idx, "VIn", size)

    field_min = 2
    model._mesh.field.add("Min", field_min)
    model._mesh.field.setNumbers(field_min, "FieldsList", [idx])
    model._mesh.field.setAsBackgroundMesh(field_min)
    model.synchronize()

    model.generate_mesh(dim=3)




    # # Copy and enforce shared nodes between the zones:
    # copy_molten = model._occ.copy([(2, base_molten)])[0][1]
    # copy_gap    = model._occ.copy([(2, base_gap)])[0][1]
    # model.fragment([(2, copy_molten)], [(2, copy_gap)])

    # # # Extrude the zones to create the 3D geometry:
    # # h_layers = D1 + D2
    # # tags_molten = model.extrude([(2, copy_molten)], 0, 0, h_layers)
    # # tags_gap    = model.extrude([(2, copy_gap)],    0, 0, h_layers)
    # # body_molten = tags_molten[1]
    # # body_gap    = tags_gap[1]
    # # model.synchronize()

    # # # Enforce shared nodes between the zones:
    # # (body_molten, body_gap), _   = model.fragment([body_molten], [body_gap])
    # # (body_gap,    body_mould), _ = model.fragment([body_gap],    [body_mould])
    # # (body_molten, body_mould), _ = model.fragment([body_molten], [body_mould])
    # # model.synchronize()

    # # # Sides of gap zone:
    # # set_transfinite(model, [61, 56], size_g) # B
    # # set_transfinite(model, [62, 57], size_g) # T

    # # # Width of the gap/molten zone:
    # # set_transfinite(model, [54, 59, 65], 4*size_g)
    # # set_transfinite(model, [49, 60, 66], 4*size_g)

    # # # Sides of molten zone:
    # # set_progression(model, 72, size_d, size_g) # B
    # # set_progression(model, 73, size_d, size_g) # T
    # # set_progression(model, 68, size_d, size_g) # B
    # # set_progression(model, 69, size_d, size_g) # T

    # # # Get new indices for the bases of the zones:
    # # base_molten = 46
    # # base_gap    = 40

    # # # Base of the gap:
    # # model.set_transfinite_surface(base_gap)
    # # model.set_recombine(2, base_gap)

    # # # Base of the molten zone:
    # # model.set_transfinite_surface(base_molten)
    # # model.set_recombine(2, base_molten)

    # # model.generate_mesh(dim=2)
    # # model.synchronize()

    # # h_layers = D1 + D2
    # # n_layers = int(h_layers / size_d)
    # # opts = dict(numElements=[n_layers], recombine=True)
    # # surf = [(2, base_molten), (2, base_gap)]
    # # model.extrude(surf, 0, 0, h_layers, **opts)
    # # model.synchronize()
    # # model.generate_mesh(dim=3)
