"""Rig and export the floating Enemy Task mesh and its combat poses."""
import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

parser = argparse.ArgumentParser()
parser.add_argument("--project", type=Path, required=True)
project = parser.parse_args(sys.argv[sys.argv.index("--") + 1:]).project
assets = project / "Assets" / "Prototype" / "EnemyTask"
mesh = bpy.data.objects["output_unwrapped"]
mesh.name = "EnemyTask Mesh"
scene = bpy.context.scene
scene.render.fps = 60

armature = bpy.data.armatures.new("EnemyTask Skeleton")
rig = bpy.data.objects.new("EnemyTask Rig", armature)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")

def bone(name, head, tail, parent=None):
    item = armature.edit_bones.new(name)
    item.head, item.tail = head, tail
    item.parent = parent
    return item

root = bone("Root", (0, 0, -.92), (0, 0, -.64))
body = bone("Body", (0, 0, -.42), (0, 0, .58), root)
for side, sign in (("L", -1), ("R", 1)):
    elbow = (sign * .61, -.09, -.64)
    arm = bone(f"Arm.{side}", (sign * .37, -.06, -.40), elbow, body)
    bone(f"Fist.{side}", elbow, (sign * .73, -.14, -.84), body)

bpy.ops.object.mode_set(mode="OBJECT")
mesh.parent = rig
skin = mesh.modifiers.new("EnemyTask Skin", "ARMATURE")
skin.object = rig
groups = {name: mesh.vertex_groups.new(name=name) for name in ("Body", "Arm.L", "Arm.R", "Fist.L", "Fist.R")}

def smooth(low, high, value):
    t = max(0, min(1, (value - low) / (high - low)))
    return t * t * (3 - 2 * t)

for vertex in mesh.data.vertices:
    x, y, z = vertex.co
    side = "L" if x < 0 else "R"
    lateral = abs(x)
    limb = smooth(.33, .58, lateral) * smooth(.29, .58, -z)
    hand = max(smooth(.54, .72, lateral), smooth(.57, .81, -z))
    weights = {"Body": 1 - limb, f"Arm.{side}": limb * (1 - hand), f"Fist.{side}": limb * hand}
    for name, weight in weights.items():
        if weight > 0:
            groups[name].add([vertex.index], weight, "REPLACE")

rig.animation_data_create()
for item in rig.pose.bones:
    item.rotation_mode = "QUATERNION"

def aim(item, direction):
    rest = item.bone.matrix_local.to_quaternion()
    forward = rest @ Vector((0, 1, 0))
    turn = forward.rotation_difference(Vector(direction).normalized())
    item.rotation_quaternion = rest.inverted() @ turn @ rest

def apply(height, pitch, left, right, left_scale, right_scale, left_length=1, right_length=1):
    for item in rig.pose.bones:
        item.matrix_basis = Matrix.Identity(4)
    body_pose = rig.pose.bones["Body"]
    body_pose.location = body_pose.bone.matrix_local.to_quaternion().inverted() @ Vector((0, 0, height))
    aim(body_pose, (0, math.sin(math.radians(pitch)), math.cos(math.radians(pitch))))
    for side, direction, size, length in (("L", left, left_scale, left_length), ("R", right, right_scale, right_length)):
        arm = rig.pose.bones[f"Arm.{side}"]
        aim(arm, direction)
        arm.scale.y = length

        # Keep the claw on the stretched arm tip without inheriting arm shear in FBX.
        fist = rig.pose.bones[f"Fist.{side}"]
        arm_rest = arm.bone.matrix_local.to_quaternion()
        fist_rest = fist.bone.matrix_local.to_quaternion()
        turn = arm_rest @ arm.rotation_quaternion @ arm_rest.inverted()
        wrist = arm.bone.head_local + Vector(direction).normalized() * arm.bone.length * length
        fist.location = fist_rest.inverted() @ (wrist - fist.bone.head_local)
        fist.rotation_quaternion = fist_rest.inverted() @ turn @ fist_rest
        fist.scale = (size,) * 3

rest = (0, 0, (-.67,-.08,-.73), (.67,-.08,-.73), 1, 1)
ready = (.055, -8, (-.52,-.75,.40), (.52,-.75,.40), 2.15, 2.15)
exhausted = (-.15, -20, (-.54,-.81,-.18), (.54,-.81,-.18), 1.16, 1.16, 1.34, 1.34)
defeated = (-.32, -54, (-.95,-.15,-.22), (.95,-.15,-.22), .8, .8)

def make_clip(name, keys):
    rig.animation_data.action = None
    clip = bpy.data.actions.new(name)
    rig.animation_data.action = clip
    clip.use_fake_user = True
    for frame, pose in keys:
        apply(*pose)
        for item in rig.pose.bones:
            for prop in ("location", "rotation_quaternion", "scale"):
                item.keyframe_insert(prop, frame=frame, group=item.name)

make_clip("TaskIdle", [(1,rest), (40,(.07,4,*rest[2:])), (79,rest)])
make_clip("TaskJump", [
    (1, rest),
    (9, (-.16, 18, (-.84,.27,-.47), (.84,.27,-.47), 1, 1)),
    (15, (.035, -14, (-.71,-.55,.40), (.71,-.55,.40), 1, 1)),
    (22, (.03, -10, (-.83,-.40,.26), (.83,-.40,.26), 1, 1)),
    (33, (-.12, 16, (-.87,-.20,-.45), (.87,-.20,-.45), 1, 1)),
    (41, rest),
])
make_clip("TaskWindup", [(1,rest), (13,(-.045,7,(-.85,.18,-.4),(.85,.18,-.4),1.24,1.24)), (28,ready)])
for side in ("Left","Right"):
    if side == "Left":
        load = (.035,8,(-.74,.18,.55),ready[3],2.35,2.15)
        hit = (.025,-16,(-.10,-1,.12),ready[3],3.25,2.15,8.0,1)
    else:
        load = (.035,8,ready[2],(.74,.18,.55),2.15,2.35)
        hit = (.025,-16,ready[2],(.10,-1,.12),2.15,3.25,1,8.0)
    make_clip("TaskPunch" + side, [(1,ready), (8,load), (17,hit), (21,hit), (35,ready)])

make_clip("TaskExhausted", [(1,ready), (18,exhausted), (30,exhausted)])
make_clip("TaskStagger", [(1,exhausted), (8,(-.13,26,(-.86,-.30,.39),(.86,-.30,.39),1.05,1.05)),
                          (23,exhausted), (30,exhausted)])
make_clip("TaskRecover", [(1,exhausted), (16,(-.05,10,(-.80,-.38,-.40),(.80,-.38,-.40),1.02,1.02)), (36,rest)])
make_clip("TaskDefeated", [(1,exhausted), (22,defeated), (30,defeated)])
# Fit the collision boxes to the deformed claws at the impact key.
hit_markers = {}
for side, clip in (("L", "TaskPunchLeft"), ("R", "TaskPunchRight")):
    rig.animation_data.action = bpy.data.actions[clip]
    scene.frame_set(17)
    deformed = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    fist_name = f"Fist.{side}"
    group_index = groups[fist_name].index
    vertices = [deformed.data.vertices[vertex.index].co for vertex in mesh.data.vertices
                if any(group.group == group_index and group.weight >= .5 for group in vertex.groups)]
    low = Vector(min(v[i] for v in vertices) for i in range(3))
    high = Vector(max(v[i] for v in vertices) for i in range(3))
    center = (low + high) * .5
    half = (high - low) * .5
    rest_from_impact = armature.bones[fist_name].matrix_local @ rig.pose.bones[fist_name].matrix.inverted()
    hit_markers[f"HitCenter.{side}"] = (fist_name, rest_from_impact @ center)
    for index, axis in enumerate("XYZ"):
        offset = center.copy()
        offset[index] += half[index]
        hit_markers[f"Hit{axis}.{side}"] = (fist_name, rest_from_impact @ offset)

bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="EDIT")
for name, (parent, position) in hit_markers.items():
    marker_bone = bone(name, position, position + Vector((0, 0, .06)), armature.edit_bones[parent])
    marker_bone.use_deform = False
bpy.ops.object.mode_set(mode="OBJECT")

rig.animation_data.action = bpy.data.actions["TaskIdle"]
scene.frame_start, scene.frame_end = 1, 79
scene.frame_set(1)

bpy.data.images["texture_0"].save(filepath=str(assets / "EnemyTask_Albedo.png"))
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(project / "ArtSource" / "EnemyTask_Rigged.blend"))
bpy.ops.object.select_all(action="DESELECT")
mesh.select_set(True)
rig.select_set(True)
bpy.ops.export_scene.fbx(
    filepath=str(assets / "EnemyTask.fbx"), use_selection=True, object_types={"ARMATURE","MESH"},
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0,
    axis_forward="-Z", axis_up="Y",
)
print("Exported Enemy Task", len(armature.bones), "bones and", len(bpy.data.actions), "clips")

