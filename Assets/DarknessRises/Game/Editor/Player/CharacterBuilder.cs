using DarknessRises.Game.Player;
using DarknessRises.Game.Weapons;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace DarknessRises.Game.Editor.Player
{
    /// <summary>
    /// Builds the character's model and animation: the humanoid model from Human Basic Motions, its Animator
    /// controller (a directional walk with death on the base layer, and aim, shoot and throw on an upper-body layer),
    /// the grips its hands hold the Weapon by, and the view that drives it. The controller is regenerated from the
    /// <see cref="CharacterAnimationSet"/> on every rebuild; empty required slots get the project's default clips.
    /// </summary>
    /// <remarks>
    /// Keep the throw and death timings in the Player definition in step with the clips.
    /// </remarks>
    public static class CharacterBuilder
    {
        private const string AnimationFolder = "Assets/DarknessRises/Animation/Character";
        private const string GeneratedClipsFolder = AnimationFolder + "/Generated";
        private const string AnimationSetPath = AnimationFolder + "/CharacterAnimationSet.asset";
        private const string ControllerPath = AnimationFolder + "/Character.controller";
        private const string UpperBodyMaskPath = AnimationFolder + "/CharacterUpperBody.mask";
        private const string DeathClipPath = GeneratedClipsFolder + "/Death.anim";

        // Imported from the Asset Store (approved sources): Human Basic Motions FREE and Crafting Mecanim Animation Pack FREE.
        private const string HumanAnimations = "Assets/Kevin Iglesias/Human Animations";
        private const string ModelPath = HumanAnimations + "/Models/HumanM_Model.fbx";
        private const string IdlePath = HumanAnimations + "/Animations/Male/Idles/HumanM@Idle01.fbx";
        private const string WalkFolder = HumanAnimations + "/Animations/Male/Movement/Walk";
        private const string ThrowPath = "Assets/ExplosiveLLC/Crafting Mecanim Animation Pack FREE/Animations/Crafter@Carry-Handoff.FBX";

        private const string ModelName = "Model";
        private const string RecoilName = "Recoil";
        private const string PlaceholderPistolName = "Placeholder Pistol";

        // Animator parameters, matching CharacterAnimationView.
        private const string MoveForward = "MoveForward";
        private const string MoveRight = "MoveRight";
        private const string Shoot = "Shoot";
        private const string Throw = "Throw";
        private const string Dead = "Dead";
        private const int UpperBodyLayer = 1;

        // Two-handed grip on the Weapon, in the weapon mount's space (metres): the right hand holds it,
        // the left cups it from below and to the side.
        private static readonly Vector3 RightGrip = new(0.02f, -0.05f, 0.3f);
        private static readonly Vector3 LeftGrip = new(-0.05f, -0.08f, 0.32f);
        private static readonly Vector3 LeftGripTurn = new(0f, 25f, 0f);
        private static readonly Vector3 RightElbowHint = new(0.6f, -1f, 0f);
        private static readonly Vector3 LeftElbowHint = new(-0.6f, -1f, 0f);

        // How the upper body's twist is shared along the spine.
        private const float SpineShare = 0.3f;
        private const float ChestShare = 0.3f;
        private const float UpperChestShare = 0.4f;

        /// <summary>Adds the humanoid model under <paramref name="character"/>, and a capsule collider for its body.</summary>
        public static void BuildModel(Transform character, Material material)
        {
            const float bodyHeight = 2f;
            const float bodyRadius = 0.4f;
            var body = character.gameObject.AddComponent<CapsuleCollider>();
            body.height = bodyHeight;
            body.radius = bodyRadius;
            body.center = Vector3.up * (bodyHeight / 2f);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath)
                ?? throw new System.InvalidOperationException($"Import Human Basic Motions FREE first: {ModelPath} is missing.");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, character);
            model.name = ModelName;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            // The pack's demo material is Built-in only; paint the model with the project's URP character material.
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// Builds the Animator controller from the animation set and adds the view that drives it, turning the
        /// upper body toward <paramref name="primaryWeapon"/>'s aim and putting the hands on its grips.
        /// </summary>
        public static CharacterAnimationView Animate(Transform character, PrimaryWeaponView primaryWeapon)
        {
            EnsureFolder(GeneratedClipsFolder);

            var model = character.Find(ModelName)
                ?? throw new System.InvalidOperationException($"{character.name} has no {ModelName} to animate.");
            var animator = model.GetComponent<Animator>();

            var set = LoadOrCreateAnimationSet(animator);
            var controller = BuildController(set, BuildUpperBodyMask());

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            var mount = (Transform)new SerializedObject(primaryWeapon).FindProperty("mount").objectReferenceValue;
            var recoil = BuildGrips(mount, out var rightGrip, out var leftGrip);

            var view = character.gameObject.AddComponent<CharacterAnimationView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("animator").objectReferenceValue = animator;
            serialized.FindProperty("model").objectReferenceValue = model;
            serialized.FindProperty("aim").objectReferenceValue = mount;
            serialized.FindProperty("upperBodyLayer").intValue = UpperBodyLayer;
            serialized.FindProperty("recoil").objectReferenceValue = recoil;
            SetTwistBones(serialized.FindProperty("twistBones"), animator);
            SetArm(serialized.FindProperty("rightArm"), animator, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm,
                HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal, rightGrip, RightElbowHint);
            SetArm(serialized.FindProperty("leftArm"), animator, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm,
                HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal, leftGrip, LeftElbowHint);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // The Weapon's model moves onto a recoil pivot with the grips, so a shot kicks the gun and the hands back together.
        // The muzzle stays where it is: Shots always leave from it.
        private static Transform BuildGrips(Transform mount, out Transform rightGrip, out Transform leftGrip)
        {
            var recoil = new GameObject(RecoilName).transform;
            recoil.SetParent(mount, false);

            var pistol = mount.Find(PlaceholderPistolName);
            if (pistol != null)
            {
                pistol.SetParent(recoil, false);
            }

            rightGrip = new GameObject("Right Grip").transform;
            rightGrip.SetParent(recoil, false);
            rightGrip.localPosition = RightGrip;

            leftGrip = new GameObject("Left Grip").transform;
            leftGrip.SetParent(recoil, false);
            leftGrip.localPosition = LeftGrip;
            leftGrip.localEulerAngles = LeftGripTurn;
            return recoil;
        }

        private static void SetTwistBones(SerializedProperty list, Animator animator)
        {
            var upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
            var bones = upperChest != null
                ? new[] { (HumanBodyBones.Spine, SpineShare), (HumanBodyBones.Chest, ChestShare), (HumanBodyBones.UpperChest, UpperChestShare) }
                : new[] { (HumanBodyBones.Spine, SpineShare), (HumanBodyBones.Chest, ChestShare + UpperChestShare) };

            list.arraySize = bones.Length;
            for (var i = 0; i < bones.Length; i++)
            {
                var element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("bone").objectReferenceValue = animator.GetBoneTransform(bones[i].Item1);
                element.FindPropertyRelative("share").floatValue = bones[i].Item2;
            }
        }

        private static void SetArm(
            SerializedProperty arm,
            Animator animator,
            HumanBodyBones upper,
            HumanBodyBones lower,
            HumanBodyBones hand,
            HumanBodyBones fingers,
            Transform grip,
            Vector3 elbowHint)
        {
            arm.FindPropertyRelative("upperArm").objectReferenceValue = animator.GetBoneTransform(upper);
            arm.FindPropertyRelative("forearm").objectReferenceValue = animator.GetBoneTransform(lower);
            arm.FindPropertyRelative("hand").objectReferenceValue = animator.GetBoneTransform(hand);
            arm.FindPropertyRelative("fingers").objectReferenceValue = animator.GetBoneTransform(fingers);
            arm.FindPropertyRelative("grip").objectReferenceValue = grip;
            arm.FindPropertyRelative("elbowHint").vector3Value = elbowHint;
        }

        private static CharacterAnimationSet LoadOrCreateAnimationSet(Animator animator)
        {
            var set = AssetDatabase.LoadAssetAtPath<CharacterAnimationSet>(AnimationSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CharacterAnimationSet>();
                AssetDatabase.CreateAsset(set, AnimationSetPath);
            }

            var serialized = new SerializedObject(set);
            FillSlot(serialized, "idle", () => LoadClip(IdlePath));
            FillSlot(serialized, "walkForward", () => LoadWalk("Forward"));
            FillSlot(serialized, "walkBackward", () => LoadWalk("Backward"));
            FillSlot(serialized, "walkLeft", () => LoadWalk("Left"));
            FillSlot(serialized, "walkRight", () => LoadWalk("Right"));
            FillSlot(serialized, "walkForwardLeft", () => LoadWalk("ForwardLeft"));
            FillSlot(serialized, "walkForwardRight", () => LoadWalk("ForwardRight"));
            FillSlot(serialized, "walkBackwardLeft", () => LoadWalk("BackwardLeft"));
            FillSlot(serialized, "walkBackwardRight", () => LoadWalk("BackwardRight"));
            FillSlot(serialized, "throwGrenade", () => LoadClip(ThrowPath));
            FillSlot(serialized, "death", () => LoadOrCreateDeath(serialized.FindProperty("idle").objectReferenceValue as AnimationClip));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        private static void FillSlot(SerializedObject set, string slot, System.Func<AnimationClip> defaultClip)
        {
            var property = set.FindProperty(slot);
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = defaultClip();
            }
        }

        private static AnimationClip LoadWalk(string direction)
        {
            return LoadClip($"{WalkFolder}/HumanM@Walk01_{direction}.fbx");
        }

        private static AnimationClip LoadClip(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            throw new System.InvalidOperationException($"No animation clip in {path}. Import the character's Asset Store packs first.");
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
            var diagonal = Mathf.Sqrt(0.5f);
            AddWalk(walk, set.Idle, Vector2.zero);
            AddWalk(walk, set.WalkForward, Vector2.up);
            AddWalk(walk, set.WalkBackward, Vector2.down);
            AddWalk(walk, set.WalkLeft, Vector2.left);
            AddWalk(walk, set.WalkRight, Vector2.right);
            AddWalk(walk, set.WalkForwardLeft, new Vector2(-diagonal, diagonal));
            AddWalk(walk, set.WalkForwardRight, new Vector2(diagonal, diagonal));
            AddWalk(walk, set.WalkBackwardLeft, new Vector2(-diagonal, -diagonal));
            AddWalk(walk, set.WalkBackwardRight, new Vector2(diagonal, -diagonal));
            machine.defaultState = locomotion;

            var death = machine.AddState("Death");
            death.motion = set.Death;
            var toDeath = machine.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, Dead);
            toDeath.canTransitionToSelf = false;
            toDeath.hasExitTime = false;
            toDeath.duration = 0.1f;
        }

        private static void AddWalk(BlendTree walk, AnimationClip clip, Vector2 direction)
        {
            if (clip != null)
            {
                walk.AddChild(clip, direction);
            }
        }

        // An empty Aim or Shoot slot leaves an empty state: the layer then adds nothing over the walk,
        // and the view's arm IK holds the Weapon and its recoil kicks it.
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
            AddTriggered(aim, throwGrenade, Throw, 0.1f);
            AddTriggered(shoot, throwGrenade, Throw, 0.1f);
            AddBackWhenDone(shoot, aim, 0.05f, set.Shoot != null);
            AddBackWhenDone(throwGrenade, aim, 0.15f, true);
        }

        private static void AddTriggered(AnimatorState from, AnimatorState to, string trigger, float duration)
        {
            var transition = from.AddTransition(to);
            transition.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            transition.hasExitTime = false;
            transition.duration = duration;
        }

        // An empty state has no length to wait out, so it goes straight back.
        private static void AddBackWhenDone(AnimatorState from, AnimatorState to, float duration, bool hasClip)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = hasClip ? 1f - duration : 0f;
            transition.duration = duration;
        }

        // Aim, shoot and throw play on the spine, arms and head; the legs and root stay with the walk.
        private static AvatarMask BuildUpperBodyMask()
        {
            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
            if (mask == null)
            {
                mask = new AvatarMask();
                AssetDatabase.CreateAsset(mask, UpperBodyMaskPath);
            }

            mask.transformCount = 0;
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

        /// <summary>
        /// Neither approved pack has a death, so this generates one from the idle's first pose: the knees buckle,
        /// then the body falls onto its back and lies still. Replace it in the animation set when a real clip exists.
        /// </summary>
        private static AnimationClip LoadOrCreateDeath(AnimationClip idle)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(DeathClipPath);
            if (existing != null)
            {
                return existing;
            }

            const float buckleSeconds = 0.3f;
            const float landSeconds = 0.85f;
            const float endSeconds = 1.2f;
            const float buckledBodyHeight = 0.7f;
            const float lyingBodyHeight = 0.14f;
            const float buckledKnees = 0.1f;
            const float lyingKnees = 0.8f;
            const float lyingArmsDownUp = 0.1f;
            const float fallBackDegrees = -84f;
            const float buckleBackDegrees = -12f;

            var clip = new AnimationClip { name = "Death" };
            var times = new[] { 0f, buckleSeconds, landSeconds, endSeconds };
            var curves = new System.Collections.Generic.Dictionary<string, float>();
            foreach (var binding in AnimationUtility.GetCurveBindings(idle))
            {
                curves[binding.propertyName] = AnimationUtility.GetEditorCurve(idle, binding).Evaluate(0f);
            }

            var standingRoot = new Quaternion(curves["RootQ.x"], curves["RootQ.y"], curves["RootQ.z"], curves["RootQ.w"]);
            var buckledRoot = Quaternion.Euler(buckleBackDegrees, 0f, 0f) * standingRoot;
            var lyingRoot = Quaternion.Euler(fallBackDegrees, 0f, 0f) * standingRoot;
            var height = curves["RootT.y"];

            foreach (var pair in curves)
            {
                var standing = pair.Value;
                var values = pair.Key switch
                {
                    "RootT.y" => new[] { height, buckledBodyHeight, lyingBodyHeight, lyingBodyHeight },
                    "RootQ.x" => new[] { standing, buckledRoot.x, lyingRoot.x, lyingRoot.x },
                    "RootQ.y" => new[] { standing, buckledRoot.y, lyingRoot.y, lyingRoot.y },
                    "RootQ.z" => new[] { standing, buckledRoot.z, lyingRoot.z, lyingRoot.z },
                    "RootQ.w" => new[] { standing, buckledRoot.w, lyingRoot.w, lyingRoot.w },
                    "Left Lower Leg Stretch" or "Right Lower Leg Stretch" => new[] { standing, buckledKnees, lyingKnees, lyingKnees },
                    "Left Arm Down-Up" or "Right Arm Down-Up" => new[] { standing, standing, lyingArmsDownUp, lyingArmsDownUp },
                    _ => new[] { standing, standing, standing, standing },
                };

                var keys = new Keyframe[times.Length];
                for (var i = 0; i < times.Length; i++)
                {
                    keys[i] = new Keyframe(times[i], values[i]);
                }

                var curve = new AnimationCurve(keys);
                for (var i = 0; i < keys.Length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                }

                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), pair.Key), curve);
            }

            AssetDatabase.CreateAsset(clip, DeathClipPath);
            return clip;
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
