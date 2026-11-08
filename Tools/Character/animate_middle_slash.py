"""Author and export the MiddleSlash rig with in-place clips for the prototype."""
import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

parser = argparse.ArgumentParser()
parser.add_argument("--project", type=Path, required=True)
project = parser.parse_args(sys.argv[sys.argv.index("--") + 1:]).project
assets = project / "Assets" / "Prototype" / "Character"
(assets / "Resources").mkdir(parents=True, exist_ok=True)
rig = bpy.data.objects["Armature"]
scene = bpy.context.scene
scene.render.fps = 60
source_walk = bpy.data.actions["Walk"]
rig.animation_data.action = source_walk
meshes = [obj for obj in rig.children if obj.type == "MESH" and
          any(mod.type == "ARMATURE" and mod.object == rig for mod in obj.modifiers)]

# Copy the source cycle before adding the weapon markers and authored poses.
walk_frames = []
for frame in range(1, 71):
    scene.frame_set(frame)
    walk_frames.append({bone.name: bone.matrix_basis.copy() for bone in rig.pose.bones})

rig.animation_data.action = None
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
edit = rig.data.edit_bones
socket = edit.new("SwordSocket")
socket.head = (0, 0, .4)
socket.tail = (0, .15, .4)
socket.parent = edit["Bone.004"]
for name, head in [("SwordTip", (0, .15, .4)), ("SwordUp", (0, 0, .5))]:
    marker = edit.new(name)
    marker.head = head
    marker.tail = Vector(head) + Vector((0, .05, 0))
    marker.parent = socket
bpy.ops.object.mode_set(mode="OBJECT")
bones = rig.pose.bones
for bone in bones:
    bone.rotation_mode = "QUATERNION"

def place(name, position):
    bone = bones[name]
    offset = Vector(position) - bone.bone.head_local
    bone.location = bone.bone.matrix_local.to_quaternion().inverted() @ offset

def torso(pitch, yaw, height):
    pelvis = bones["Bone"]
    rest = pelvis.bone.matrix_local.to_quaternion()
    rotation = Quaternion((0, 0, 1), math.radians(yaw)) @ Quaternion((1, 0, 0), math.radians(pitch))
    pelvis.rotation_quaternion = rest.inverted() @ rotation @ rest
    pelvis.location = rest.inverted() @ Vector((0, 0, height))

def weapon(grip, direction, roll=0, blade_normal=None):
    direction = Vector(direction).normalized()
    if blade_normal is None:
        orientation = direction.to_track_quat("Y", "Z") @ Quaternion((0, 1, 0), math.radians(roll))
        weapon_frame = orientation.to_matrix().to_4x4()
    else:
        width = direction.cross(Vector(blade_normal)).normalized()
        normal = width.cross(direction)
        weapon_frame = Matrix((width, direction, normal)).transposed().to_4x4()
    weapon_frame.translation = Vector(grip)
    bones["SwordSocket"].matrix = weapon_frame

def back_weapon(follow_torso=False):
    grip = Vector((.19, -.24, .26))
    direction = Vector((-.48, -.33, .81))
    if follow_torso:
        pelvis = bones["Bone"]
        offset = pelvis.matrix @ pelvis.bone.matrix_local.inverted()
        grip = offset @ Vector((.19, -.10, .25)) + Vector((0, -.20, 0))
    weapon(grip, direction, roll=85)

def combat_hands(grip, direction, blade_normal=None):
    direction = Vector(direction).normalized()
    place("handik.r", grip)
    place("handik.l", Vector(grip) - direction * .075)
    bpy.context.view_layer.update()
    weapon(bones["hand.r"].head, direction, blade_normal=blade_normal)

def pose(pitch, yaw, height, grip, direction, left_y=-.055, right_y=.04, left_lift=0, right_lift=0, blade_normal=None):
    return (pitch, yaw, height, grip, direction, left_y, right_y, left_lift, right_lift, blade_normal)

# Keep the wide face normal to the vertical cut plane so the edge leads.
cut_plane_normal = (1, 0, 0)
diagonal_cut_normal = (1, .28, -.48)

drag_direction = (.48, -.86, -.37)
guard = pose(5, -15, -.016, (.24, -.13, .425), drag_direction)
lower_to_guard = pose(9, -7, -.026, (.30, .045, .44), (.96, .16, -.16))
reach = pose(-8, -19, -.008, (.31, -.15, .51), (.62, -.53, .58))
raised = pose(-28, -9, -.018, (.31, -.105, .54), (.53, -.65, .57), blade_normal=diagonal_cut_normal)
cut = pose(-8, 16, -.035, (-.055, .185, .395), (-.55, .75, -.30), -.074, .1, blade_normal=diagonal_cut_normal)
sweep_load = pose(-18, 30, -.035, (-.12, .06, .39), (-.88, -.45, -.10), -.095, .07)
sweep_start = pose(-10, 12, -.04, (-.10, .11, .40), (-.93, .31, -.10), -.08, .075)
sweep_middle = pose(12, -5, -.042, (.065, .147, .447), (.1, .98, .16), -.035, .065, .026)
sweep_end = pose(-8, -26, -.021, (.31, .035, .53), (.84, .2, .49), .061, -.024)
sweep_follow_through = pose(-11, -19, -.026, (.33, .005, .54), (.88, -.18, .43), .07, -.05)
heavy_ready = pose(-31, -12, -.034, (.30, -.115, .54), (.48, -.70, .53), .08, -.071, blade_normal=cut_plane_normal)
heavy_cut = pose(-34, 14, -.052, (.08, .18, .385), (.02, .94, -.29), -.09, .113, blade_normal=cut_plane_normal)
heavy_straighten = pose(0, 14, -.027, heavy_cut[3], heavy_cut[4], -.09, .113, blade_normal=cut_plane_normal)
heavy_lift = pose(1, 2, -.022, (.24, .19, .43), (.76, .64, -.12), -.06, .09, blade_normal=cut_plane_normal)
defense = pose(0, 0, -.013, None, None, -.031, -.031)

def reset_bones():
    for bone in bones:
        bone.matrix_basis = Matrix.Identity(4)

def apply_pose(values):
    pitch, yaw, height, grip, direction, left_y, right_y, left_lift, right_lift, blade_normal = values
    torso(pitch, yaw, height)
    # The head IK must travel with the lean or it pulls the chest upright.
    forward_lean = min(1, max(0, (-pitch - 8) / 26))
    for name in ("HeadIk", "head_target"):
        rest = bones[name].bone.head_local
        place(name, rest + Vector((0, .16, -.075)) * forward_lean)
    place("heelik.l", (-.065, left_y, .031 + left_lift))
    place("heelik.r", (.065, right_y, .031 + right_lift))
    bpy.context.view_layer.update()
    if grip is None:
        back_weapon()
    else:
        combat_hands(grip, direction, blade_normal)

previous_quaternions = {}

def action(name):
    previous_quaternions.clear()
    rig.animation_data.action = None
    reset_bones()
    clip = bpy.data.actions.new(name)
    rig.animation_data.action = clip
    clip.use_fake_user = True
    return clip

def key(frame):
    for bone in bones:
        rotation = bone.rotation_quaternion.copy()
        previous = previous_quaternions.get(bone.name)
        # Equivalent quaternion signs must stay consistent for component interpolation.
        if previous is not None and rotation.dot(previous) < 0:
            rotation.negate()
            bone.rotation_quaternion = rotation
        previous_quaternions[bone.name] = rotation

        bone.keyframe_insert("location", frame=frame, group=bone.name)
        bone.keyframe_insert("rotation_quaternion", frame=frame, group=bone.name)
        bone.keyframe_insert("scale", frame=frame, group=bone.name)

def add_poses(name, points):
    action(name)
    for frame, values in points:
        reset_bones()
        apply_pose(values)
        key(frame)

def locomotion(name, armed):
    action(name)
    for index, transforms in enumerate(walk_frames):
        for bone_name, matrix in transforms.items():
            bones[bone_name].matrix_basis = matrix
        if armed:
            for foot in ("heelik.l", "heelik.r"):
                bones[foot].location *= .64
            cycle = math.tau * index / (len(walk_frames) - 1)
            torso(6, -13 + 4 * math.sin(cycle), -.022 + .006 * math.cos(2 * cycle))
            combat_hands((.24, -.14 + .014 * math.sin(cycle), .43),
                         (.48 + .035 * math.sin(cycle - .75), -.86, -.37))
        else:
            bpy.context.view_layer.update()
            back_weapon()
        key(index + 1)

def defensive_run():
    action("DefensiveRun")
    frame_count = 30
    stance_fraction = .46
    stride = .155
    for index in range(frame_count + 1):
        reset_bones()
        phase = (index % frame_count) / frame_count
        cycle = math.tau * phase
        torso(-52 + 2 * math.cos(2 * cycle), 5 * math.sin(cycle),
              -.032 + .015 * math.sin(2 * cycle))
        head_bob = .015 * math.sin(2 * cycle)
        place("HeadIk", (0, .185, .51 + head_bob))
        place("head_target", (0, .48, .54 + head_bob))

        for side, sign, offset in (("l", -1, 0), ("r", 1, .5)):
            foot_phase = (phase + offset) % 1
            if foot_phase < stance_fraction:
                travel = 1 - 2 * foot_phase / stance_fraction
                lift = 0
            else:
                swing = (foot_phase - stance_fraction) / (1 - stance_fraction)
                travel = -math.cos(math.pi * swing)
                lift = .13 * math.sin(math.pi * swing)
            place("heelik." + side, (sign * .073, -.015 + stride * travel, .031 + lift))
            place("handik." + side, (sign * (.225 + .009 * math.sin(cycle + offset * math.tau)),
                                      -.157 + .016 * math.cos(cycle + offset * math.tau), .431))

        bpy.context.view_layer.update()
        back_weapon(follow_torso=True)
        key(index + 1)

defensive_run()

locomotion("DefensiveWalk", False)
locomotion("OffensiveWalk", True)
add_poses("DefensiveIdle", [(1, defense), (43, pose(1.5, 0, -.009, None, None, -.031, -.031)), (85, defense)])
add_poses("OffensiveIdle", [(1, guard), (37, pose(3, -12, -.01, (.24, -.12, .432), drag_direction)), (73, guard)])
add_poses("DrawWindup", [(1, defense), (5, reach), (12, raised)])
add_poses("Sheathe", [(1, guard), (14, reach), (28, defense)])
add_poses("CutWindup", [(1, guard), (11, pose(-22, -23, -.023, (.31, -.145, .48), (.65, -.72, .24))), (22, raised)])
add_poses("CutStrike", [
    (1, raised),
    (3, pose(-23, -4, -.022, (.34, .01, .58), (.42, .21, .88), blade_normal=diagonal_cut_normal)),
    (5, pose(-17, 4, -.027, (.22, .15, .54), (.03, .98, .19), -.055, .08, blade_normal=diagonal_cut_normal)),
    (9, cut),
])
# Frame 7 of the 1–31 recovery clips is the shared combo branch pose.
add_poses("CutRecovery", [(1, cut), (4, pose(4, 30, -.035, (-.11, .16, .39), (-.92, .17, -.16), -.095, .07)), (7, sweep_load), (13, pose(1, 20, -.03, (-.10, .17, .39), (-.91, .22, -.12))), (20, lower_to_guard), (26, guard), (31, guard)])
add_poses("SweepWindup", [(1, sweep_load), (8, pose(-14, 19, -.041, (-.11, .08, .40), (-.99, -.07, -.12))), (15, sweep_start)])
add_poses("SweepStrike", [(1, sweep_start), (7, sweep_middle), (13, sweep_end)])
add_poses("SweepRecovery", [(1, sweep_end), (7, sweep_follow_through), (26, guard), (31, guard)])
add_poses("FinisherWindup", [(1, sweep_follow_through), (12, pose(-20, -19, -.027, (.33, -.055, .56), (.67, -.46, .57), blade_normal=cut_plane_normal)), (29, heavy_ready)])
add_poses("FinisherStrike", [
    (1, heavy_ready),
    (5, pose(-19, -5, -.032, (.35, -.02, .59), (.40, .13, .91), blade_normal=cut_plane_normal)),
    (8, pose(-23, 2, -.037, (.24, .18, .54), (.18, .93, .32), -.055, .08, 0, .03, blade_normal=cut_plane_normal)),
    (14, heavy_cut),
])
add_poses("FinisherRecovery", [(1, heavy_cut), (6, heavy_cut), (17, heavy_straighten), (23, heavy_lift), (29, lower_to_guard), (36, guard), (40, guard)])

def posed_floor():
    bpy.context.view_layer.update()
    graph = bpy.context.evaluated_depsgraph_get()
    samples = [mesh.evaluated_get(graph) for mesh in meshes]
    return min((sample.matrix_world @ vertex.co).z for sample in samples for vertex in sample.data.vertices)

def sheathe_walk():
    source = bpy.data.actions["Sheathe"]
    rig.animation_data.action = source
    sample_count = 90
    start, end = source.frame_range
    poses = []
    for index in range(sample_count + 1):
        time = start + (end - start) * index / sample_count
        scene.frame_set(int(time), subframe=time % 1)
        poses.append({bone.name: bone.matrix_basis.copy() for bone in bones})

    action("SheatheWalk")
    for index, transforms in enumerate(poses):
        progress = index / sample_count
        for name, matrix in transforms.items():
            bones[name].matrix_basis = matrix
        bpy.context.view_layer.update()

        envelope = min(1, progress / .1, (1 - progress) / .1)
        for side, sign, offset in (("l", -1, 0), ("r", 1, .5)):
            name = "heelik." + side
            neutral = bones[name].head.copy()
            phase = (2 * progress + offset) % 1
            if phase < .6:
                travel, lift = 1 - 2 * phase / .6, 0
            else:
                swing = (phase - .6) / .4
                travel = -math.cos(math.pi * swing)
                lift = .06 * math.sin(math.pi * swing)
            foot = Vector((sign * .073, -.025 + .105 * travel, .031 + lift))
            place(name, neutral.lerp(foot, envelope))
        key(index + 1)

sheathe_walk()

def knockdown_clip(name, base_pose, keyframes):
    action(name)
    apply_pose(base_pose)
    floor = posed_floor()
    chest = bones["Bone.002"]
    socket = bones["SwordSocket"]
    stowed_socket = chest.matrix.inverted() @ socket.matrix if base_pose[3] is None else None
    for frame, tilt, recovery_pose in keyframes:
        reset_bones()
        apply_pose(recovery_pose)
        bpy.context.view_layer.update()
        if stowed_socket is not None:
            socket.matrix = chest.matrix @ stowed_socket
            bpy.context.view_layer.update()
        fall = Matrix.Rotation(math.radians(tilt), 4, "X")
        root_poses = [(bone, bone.matrix.copy()) for bone in bones if not bone.parent]
        for bone, matrix in root_poses:
            bone.matrix = fall @ matrix

        grounding = Matrix.Translation(Vector((0, 0, floor - posed_floor())))
        for bone, matrix in root_poses:
            bone.matrix = grounding @ fall @ matrix
        key(frame)

for name, base_pose in (("Defensive", defense), ("Offensive", guard)):
    crouch = pose(14, -7, -.06, base_pose[3], base_pose[4], -.085, .11, blade_normal=base_pose[-1])
    knockdown_clip(name + "Fall", base_pose,
                   [(1, 0, base_pose), (7, 25, crouch), (14, 66, crouch), (23, 88, base_pose)])
    knockdown_clip(name + "Rise", base_pose,
                   [(1, 88, base_pose), (25, 88, base_pose), (50, 53, crouch),
                    (71, 21, crouch), (91, 0, base_pose)])

# Keep the original Walk in the editable Blender file, but export the game clips.
source_walk.use_fake_user = True
rig.animation_data.action = bpy.data.actions["DefensiveIdle"]
scene.frame_set(1)
# Give Unity the actual sole height rather than the padded skin bounds.
ground_height = posed_floor()
bpy.context.view_layer.objects.active = rig
bpy.ops.object.mode_set(mode="EDIT")
ground = rig.data.edit_bones.new("GroundReference")
ground.head = rig.matrix_world.inverted() @ Vector((0, 0, ground_height))
ground.tail = ground.head + Vector((0, 0, .05))
ground.use_deform = False
bpy.ops.object.mode_set(mode="OBJECT")
print("GROUND_REFERENCE", round(ground_height, 6))
scene.frame_start = 1
scene.frame_end = 85
image = bpy.data.images["MidslashTexture"]
image.save(filepath=str(assets / "MiddleSlash_Albedo.png"))
for obj in scene.objects:
    if obj.type == 'MESH' and obj not in meshes:
        obj.hide_set(True)
        obj.hide_render = True
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(project / "ArtSource" / "MiddleSlash_Animated.blend"))
bpy.data.actions.remove(source_walk)
bpy.ops.object.select_all(action="DESELECT")
for obj in [rig, *meshes]:
    obj.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=str(assets / "Resources" / "MiddleSlash.fbx"),
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    add_leaf_bones=False,
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True,
    bake_anim_step=1.0,
    bake_anim_simplify_factor=0.0,
    axis_forward="-Z",
    axis_up="Y",
)
print("EXPORTED", len(meshes), "meshes", len(bpy.data.actions), "clips")


