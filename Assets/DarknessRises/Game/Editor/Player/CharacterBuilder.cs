using DarknessRises.Game.Player;
using DarknessRises.Game.Weapons;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DarknessRises.Game.Editor.Player
{
    /// <summary>
    /// Builds the character's model and animation: a jointed placeholder model, its Animator controller
    /// (a directional walk with death on the base layer, and aim, shoot and throw on an upper-body layer),
    /// and the view that drives it. The controller is regenerated from the <see cref="CharacterAnimationSet"/>
    /// on every rebuild; empty slots in the set get generated placeholder clips that pose the placeholder model.
    /// </summary>
    /// <remarks>
    /// To drop in real clips, assign them in the set (and swap the model for one they fit), then rebuild the Test Room.
    /// Keep the throw and death timings in the Player definition in step with the clips.
    /// </remarks>
    public static class CharacterBuilder
    {
        private const string AnimationFolder = "Assets/DarknessRises/Animation/Character";
        private const string PlaceholderClipsFolder = AnimationFolder + "/Placeholder";
        private const string AnimationSetPath = AnimationFolder + "/CharacterAnimationSet.asset";
        private const string ControllerPath = AnimationFolder + "/Character.controller";
        private const string UpperBodyMaskPath = AnimationFolder + "/CharacterUpperBody.mask";
        private const string PlaceholderAvatarPath = PlaceholderClipsFolder + "/PlaceholderCharacter.asset";

        private const string ModelName = "Model";

        // Animator parameters, matching CharacterAnimationView.
        private const string MoveForward = "MoveForward";
        private const string MoveRight = "MoveRight";
        private const string Shoot = "Shoot";
        private const string Throw = "Throw";
        private const string Dead = "Dead";
        private const int UpperBodyLayer = 1;

        // Placeholder model paths, relative to the model root.
        private const string HipsPath = "Hips";
        private const string LeftLegPath = HipsPath + "/Left Leg";
        private const string RightLegPath = HipsPath + "/Right Leg";
        private const string UpperBodyPath = HipsPath + "/Upper Body";
        private const string GunArmPath = UpperBodyPath + "/Gun Arm";
        private const string ThrowArmPath = UpperBodyPath + "/Throw Arm";

        // Placeholder model layout, in metres.
        private const float HipsHeight = 0.9f;
        private const float HipsHalfWidth = 0.13f;
        private static readonly Vector3 LegSize = new(0.16f, HipsHeight, 0.18f);
        private static readonly Vector3 TorsoCentre = new(0f, 0.38f, 0f);
        private static readonly Vector3 TorsoScale = new(0.48f, 0.4f, 0.3f);
        private static readonly Vector3 HeadCentre = new(0f, 0.86f, 0f);
        private const float HeadSize = 0.3f;
        private static readonly Vector3 GunShoulder = new(0.25f, 0.55f, 0f);
        private static readonly Vector3 ThrowShoulder = new(-0.25f, 0.55f, 0f);
        private static readonly Vector3 ArmSize = new(0.11f, 0.5f, 0.11f);

        // Placeholder poses: the gun arm reaches from the shoulder to the placeholder Pistol in front of the chest.
        private static readonly Vector3 GunArmAimed = new(-70f, -32f, 0f);
        private static readonly Vector3 GunArmRecoil = new(-88f, -32f, 0f);

        // Placeholder clip timings, in seconds. The throw's release and length match the Player definition's defaults.
        private const float IdleSeconds = 2f;
        private const float StepSeconds = 0.4f;
        private const float ShootSeconds = 0.2f;
        private const float ThrowWindUpSeconds = 0.3f;
        private const float ThrowReleaseSeconds = 0.45f;
        private const float ThrowSeconds = 1f;
        private const float FallSeconds = 0.9f;
        private const float DeathSeconds = 1.2f;

        // The character's body for physics, the same size as the capsule it replaces.
        private const float BodyHeight = 2f;
        private const float BodyRadius = 0.4f;

        /// <summary>Adds the jointed placeholder model under <paramref name="character"/>, and a capsule collider for its body.</summary>
        public static void BuildPlaceholderModel(Transform character, Material material)
        {
            var body = character.gameObject.AddComponent<CapsuleCollider>();
            body.height = BodyHeight;
            body.radius = BodyRadius;
            body.center = Vector3.up * (BodyHeight / 2f);

            var model = new GameObject(ModelName, typeof(Animator)).transform;
            model.SetParent(character, false);

            var hips = AddJoint("Hips", model, Vector3.up * HipsHeight);

            var leftLeg = AddJoint("Left Leg", hips, Vector3.left * HipsHalfWidth);
            AddPart(PrimitiveType.Cube, "Left Leg Mesh", leftLeg, Vector3.down * (LegSize.y / 2f), LegSize, material);
            var rightLeg = AddJoint("Right Leg", hips, Vector3.right * HipsHalfWidth);
            AddPart(PrimitiveType.Cube, "Right Leg Mesh", rightLeg, Vector3.down * (LegSize.y / 2f), LegSize, material);

            var upperBody = AddJoint("Upper Body", hips, Vector3.zero);
            AddPart(PrimitiveType.Capsule, "Torso", upperBody, TorsoCentre, TorsoScale, material);
            AddPart(PrimitiveType.Sphere, "Head", upperBody, HeadCentre, Vector3.one * HeadSize, material);

            var gunArm = AddJoint("Gun Arm", upperBody, GunShoulder);
            AddPart(PrimitiveType.Cube, "Gun Arm Mesh", gunArm, Vector3.down * (ArmSize.y / 2f), ArmSize, material);
            var throwArm = AddJoint("Throw Arm", upperBody, ThrowShoulder);
            AddPart(PrimitiveType.Cube, "Throw Arm Mesh", throwArm, Vector3.down * (ArmSize.y / 2f), ArmSize, material);
        }

        /// <summary>
        /// Builds the Animator controller from the animation set and adds the view that drives it, turning the
        /// upper body toward <paramref name="primaryWeapon"/>'s aim.
        /// </summary>
        public static CharacterAnimationView Animate(Transform character, PrimaryWeaponView primaryWeapon)
        {
            EnsureFolder(PlaceholderClipsFolder);

            var model = character.Find(ModelName)
                ?? throw new System.InvalidOperationException($"{character.name} has no {ModelName} to animate.");

            var set = LoadOrCreateAnimationSet();
            var mask = BuildUpperBodyMask(model);
            var controller = BuildController(set, mask);

            var animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = LoadOrCreatePlaceholderAvatar(model.gameObject);
            animator.applyRootMotion = false;

            var aim = new SerializedObject(primaryWeapon).FindProperty("mount").objectReferenceValue;

            var view = character.gameObject.AddComponent<CharacterAnimationView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("model").objectReferenceValue = model;
            serialized.FindProperty("aim").objectReferenceValue = aim;
            serialized.FindProperty("upperBodyLayer").intValue = UpperBodyLayer;
            var twistBones = serialized.FindProperty("twistBones");
            twistBones.arraySize = 1;
            twistBones.GetArrayElementAtIndex(0).FindPropertyRelative("bone").objectReferenceValue = model.Find(UpperBodyPath);
            twistBones.GetArrayElementAtIndex(0).FindPropertyRelative("share").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static CharacterAnimationSet LoadOrCreateAnimationSet()
        {
            var set = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>(AnimationSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CharacterAnimationSet>();
                AssetDatabase.CreateAsset(set, AnimationSetPath);
            }

            var serialized = new SerializedObject(set);
            FillSlot(serialized, "idle", CreateIdle);
            FillSlot(serialized, "walkForward", () => CreateWalk("Walk Forward", Vector3.right, false));
            FillSlot(serialized, "walkBackward", () => CreateWalk("Walk Backward", Vector3.right, true));
            FillSlot(serialized, "walkLeft", () => CreateSideStep("Walk Left", -1f));
            FillSlot(serialized, "walkRight", () => CreateSideStep("Walk Right", 1f));
            FillSlot(serialized, "death", CreateDeath);
            FillSlot(serialized, "aim", CreateAim);
            FillSlot(serialized, "shoot", CreateShoot);
            FillSlot(serialized, "throwGrenade", CreateThrow);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        private static void FillSlot(SerializedObject set, string slot, System.Func<AnimationClip> createPlaceholder)
        {
            var property = set.FindProperty(slot);
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = createPlaceholder();
            }
        }

        private static AnimatorController BuildController(CharacterAnimationSet set, AvatarMask upperBodyMask)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            // Start over inside the same asset, so its GUID (and the scene's reference to it) stays put.
            for (var i = controller.layers.Length - 1; i >= 0; i--)
            {
                controller.RemoveLayer(i);
            }

            // Removing a layer leaves its blend trees behind in the asset.
            foreach (var leftover in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
            {
                if (leftover != null && leftover != controller)
                {
                    Object.DestroyImmediate(leftover, true);
                }
            }

            foreach (var parameter in controller.parameters)
            {
                controller.RemoveParameter(parameter);
            }

            controller.AddParameter(MoveForward, AnimatorControllerParameterType.Float);
            controller.AddParameter(MoveRight, AnimatorControllerParameterType.Float);
            controller.AddParameter(Shoot, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(Throw, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(Dead, AnimatorControllerParameterType.Bool);

            controller.AddLayer("Base");
            BuildBaseLayer(controller, set);

            controller.AddLayer("Upper Body");
            var layers = controller.layers;
            layers[UpperBodyLayer].avatarMask = upperBodyMask;
            layers[UpperBodyLayer].blendingMode = AnimatorLayerBlendingMode.Override;
            layers[UpperBodyLayer].defaultWeight = 1f;
            controller.layers = layers;
            BuildUpperBodyLayer(controller.layers[UpperBodyLayer].stateMachine, set);

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void BuildBaseLayer(AnimatorController controller, CharacterAnimationSet set)
        {
            var machine = controller.layers[0].stateMachine;

            var locomotion = controller.CreateBlendTreeInController("Walk", out var walk, 0);
            walk.blendType = BlendTreeType.FreeformDirectional2D;
            walk.blendParameter = MoveRight;
            walk.blendParameterY = MoveForward;
            walk.AddChild(set.Idle, Vector2.zero);
            walk.AddChild(set.WalkForward, Vector2.up);
            walk.AddChild(set.WalkBackward, Vector2.down);
            walk.AddChild(set.WalkLeft, Vector2.left);
            walk.AddChild(set.WalkRight, Vector2.right);
            machine.defaultState = locomotion;

            var death = machine.AddState("Death");
            death.motion = set.Death;
            var toDeath = machine.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, Dead);
            toDeath.canTransitionToSelf = false;
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
        }

        private static void BuildUpperBodyLayer(AnimatorStateMachine machine, CharacterAnimationSet set)
        {
            var aim = machine.AddState("Aim");
            aim.motion = set.Aim;
            machine.defaultState = aim;

            var shoot = machine.AddState("Shoot");
            shoot.motion = set.Shoot;

            var throwGrenade = machine.AddState("Throw");
            throwGrenade.motion = set.ThrowGrenade;

            // Every shot restarts the recoil, even mid-recoil.
            AddTriggered(aim, shoot, Shoot, 0.02f);
            AddTriggered(shoot, shoot, Shoot, 0f);
            AddTriggered(aim, throwGrenade, Throw, 0.05f);
            AddTriggered(shoot, throwGrenade, Throw, 0.05f);
            AddBackWhenDone(shoot, aim, 0.05f);
            AddBackWhenDone(throwGrenade, aim, 0.1f);
        }

        private static void AddTriggered(AnimatorState from, AnimatorState to, string trigger, float duration)
        {
            var transition = from.AddTransition(to);
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            transition.hasExitTime = false;
            transition.duration = duration;
        }

        private static void AddBackWhenDone(AnimatorState from, AnimatorState to, float duration)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = duration;
        }

        // Aim, shoot and throw play on the arms, head and torso; the legs, hips and the upper body's twist joint
        // stay with the walk. For a humanoid model the matching humanoid body parts are set too.
        private static AvatarMask BuildUpperBodyMask(Transform model)
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
            }

            // Paths are relative to the Animator's object, like the clips' curve paths.
            var bones = model.GetComponentsInChildren<Transform>(true);
            mask.transformCount = bones.Length - 1;
            for (var i = 1; i < bones.Length; i++)
            {
                var path = AnimationUtility.CalculateTransformPath(bones[i], model);
                mask.SetTransformPath(i - 1, path);
                mask.SetTransformActive(i - 1, path.StartsWith(UpperBodyPath + "/"));
            }

            for (var part = AvatarMaskBodyPart.Root; part < AvatarMaskBodyPart.LastBodyPart; part++)
            {
                var lowerBody = part is AvatarMaskBodyPart.Root
                    or AvatarMaskBodyPart.LeftLeg
                    or AvatarMaskBodyPart.RightLeg
                    or AvatarMaskBodyPart.LeftFootIK
                    or AvatarMaskBodyPart.RightFootIK;
                mask.SetHumanoidBodyPartActive(part, !lowerBody);
            }

            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static Avatar LoadOrCreatePlaceholderAvatar(GameObject model)
        {
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(PlaceholderAvatarPath);
            if (avatar != null)
            {
                return avatar;
            }

            avatar = AvatarBuilder.BuildGenericAvatar(model, string.Empty);
            avatar.name = "PlaceholderCharacter";
            AssetDatabase.CreateAsset(avatar, PlaceholderAvatarPath);
            return avatar;
        }

        private static AnimationClip CreateIdle()
        {
            var clip = new AnimationClip { name = "Idle" };
            SetEuler(clip, UpperBodyPath, new[] { 0f, IdleSeconds / 2f, IdleSeconds }, new[] { Vector3.zero, new Vector3(3f, 0f, 0f), Vector3.zero });
            SetPosition(clip, HipsPath, new[] { 0f, IdleSeconds }, new[] { Vector3.up * HipsHeight, Vector3.up * HipsHeight });
            return SavePlaceholder(clip, true);
        }

        // Legs swing about the hips' x axis; walking backwards plays the same swing in reverse.
        private static AnimationClip CreateWalk(string name, Vector3 axis, bool backwards)
        {
            const float swing = 25f;
            var direction = backwards ? -1f : 1f;
            var times = new[] { 0f, StepSeconds, StepSeconds * 2f };
            var clip = new AnimationClip { name = name };
            SetEuler(clip, LeftLegPath, times, new[] { axis * -swing * direction, axis * swing * direction, axis * -swing * direction });
            SetEuler(clip, RightLegPath, times, new[] { axis * swing * direction, axis * -swing * direction, axis * swing * direction });
            SetBob(clip);
            return SavePlaceholder(clip, true);
        }

        // Side-steps: the leading leg swings out about the z axis, then the other follows.
        private static AnimationClip CreateSideStep(string name, float side)
        {
            const float step = 20f;
            var times = new[] { 0f, StepSeconds, StepSeconds * 2f };
            var leading = side > 0f ? RightLegPath : LeftLegPath;
            var following = side > 0f ? LeftLegPath : RightLegPath;
            var clip = new AnimationClip { name = name };
            SetEuler(clip, leading, times, new[] { Vector3.zero, Vector3.forward * step * side, Vector3.zero });
            SetEuler(clip, following, times, new[] { Vector3.zero, Vector3.forward * step * side * 0.5f, Vector3.zero });
            SetBob(clip);
            return SavePlaceholder(clip, true);
        }

        private static void SetBob(AnimationClip clip)
        {
            const float bob = 0.03f;
            var low = Vector3.up * HipsHeight;
            var high = Vector3.up * (HipsHeight + bob);
            SetPosition(
                clip,
                HipsPath,
                new[] { 0f, StepSeconds / 2f, StepSeconds, StepSeconds * 1.5f, StepSeconds * 2f },
                new[] { low, high, low, high, low });
        }

        // Falls backwards, stiff as a plank, onto the floor.
        private static AnimationClip CreateDeath()
        {
            const float lyingHipsHeight = 0.2f;
            var clip = new AnimationClip { name = "Death" };
            var times = new[] { 0f, FallSeconds, DeathSeconds };
            SetEuler(clip, HipsPath, times, new[] { Vector3.zero, new Vector3(-85f, 0f, 0f), new Vector3(-85f, 0f, 0f) });
            SetPosition(clip, HipsPath, times, new[] { Vector3.up * HipsHeight, Vector3.up * lyingHipsHeight, Vector3.up * lyingHipsHeight });
            return SavePlaceholder(clip, false);
        }

        private static AnimationClip CreateAim()
        {
            var clip = new AnimationClip { name = "Aim" };
            var times = new[] { 0f, 1f };
            SetEuler(clip, GunArmPath, times, new[] { GunArmAimed, GunArmAimed });
            SetEuler(clip, ThrowArmPath, times, new[] { Vector3.zero, Vector3.zero });
            return SavePlaceholder(clip, true);
        }

        private static AnimationClip CreateShoot()
        {
            var clip = new AnimationClip { name = "Shoot" };
            SetEuler(clip, GunArmPath, new[] { 0f, ShootSeconds / 4f, ShootSeconds }, new[] { GunArmAimed, GunArmRecoil, GunArmAimed });
            SetEuler(clip, ThrowArmPath, new[] { 0f, ShootSeconds }, new[] { Vector3.zero, Vector3.zero });
            return SavePlaceholder(clip, false);
        }

        // Overarm: winds up behind the head, lets go reaching forward and up, then comes back down.
        private static AnimationClip CreateThrow()
        {
            var clip = new AnimationClip { name = "Throw" };
            SetEuler(
                clip,
                ThrowArmPath,
                new[] { 0f, ThrowWindUpSeconds, ThrowReleaseSeconds, ThrowSeconds },
                new[] { Vector3.zero, new Vector3(150f, 0f, 0f), new Vector3(250f, 0f, 0f), new Vector3(360f, 0f, 0f) });
            SetEuler(clip, GunArmPath, new[] { 0f, ThrowSeconds }, new[] { GunArmAimed, GunArmAimed });
            return SavePlaceholder(clip, false);
        }

        private static void SetEuler(AnimationClip clip, string path, float[] times, Vector3[] eulers)
        {
            for (var axis = 0; axis < 3; axis++)
            {
                var keys = new Keyframe[times.Length];
                for (var i = 0; i < times.Length; i++)
                {
                    keys[i] = new Keyframe(times[i], eulers[i][axis]);
                }

                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw." + "xyz"[axis]);
                AnimationUtility.SetEditorCurve(clip, binding, Smooth(keys));
            }
        }

        private static void SetPosition(AnimationClip clip, string path, float[] times, Vector3[] positions)
        {
            for (var axis = 0; axis < 3; axis++)
            {
                var keys = new Keyframe[times.Length];
                for (var i = 0; i < times.Length; i++)
                {
                    keys[i] = new Keyframe(times[i], positions[i][axis]);
                }

                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + "xyz"[axis]);
                AnimationUtility.SetEditorCurve(clip, binding, Smooth(keys));
            }
        }

        private static AnimationCurve Smooth(Keyframe[] keys)
        {
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }

            return curve;
        }

        private static AnimationClip SavePlaceholder(AnimationClip clip, bool loop)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            var path = $"{PlaceholderClipsFolder}/{clip.name}.anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                Object.DestroyImmediate(clip);
                return existing;
            }

            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static Transform AddJoint(string name, Transform parent, Vector3 localPosition)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = localPosition;
            return joint;
        }

        private static void AddPart(PrimitiveType shape, string name, Transform joint, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;

            // The character's capsule collider stands in for its body; the limbs don't collide.
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(joint, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
