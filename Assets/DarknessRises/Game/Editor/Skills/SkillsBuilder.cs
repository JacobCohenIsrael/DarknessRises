using DarknessRises.Game.Audio;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Editor.Hud;
using DarknessRises.Game.Editor.Weapons;
using DarknessRises.Game.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarknessRises.Game.Editor.Skills
{
    /// <summary>
    /// Builds the Skills: the Flare, Adrenaline and Lure definitions, their input actions (Q, E and F, and the Gesture
    /// device's buttons on touch), the Flare light, the Adrenaline heartbeat and the Skill runner on the character, the
    /// Lure in the Room, and their buttons on the HUD. Existing definitions are kept.
    /// </summary>
    public static class SkillsBuilder
    {
        private const string SkillsConfigurationFolder = "Assets/DarknessRises/Configuration/Skills";
        private const string FlareDefinitionPath = SkillsConfigurationFolder + "/Flare.asset";
        private const string AdrenalineDefinitionPath = SkillsConfigurationFolder + "/Adrenaline.asset";
        private const string LureDefinitionPath = SkillsConfigurationFolder + "/Lure.asset";

        // Slow-mo's definition before it became Adrenaline (#117). Moved, not recreated, so its GUID and tuning survive.
        private const string FormerSlowMoDefinitionPath = SkillsConfigurationFolder + "/SlowMo.asset";

        // The Lure's rattle: an approved Free Pack placeholder, looped where the Lure lies.
        private const string RattleClipPath = "Assets/Free Pack/Walking in ChainMail - Loop.wav";

        private const string GameplayMap = "Gameplay";
        private const string FlareAction = "Flare";
        private const string AdrenalineAction = "Adrenaline";
        private const string FormerSlowMoAction = "SlowMo";
        private const string LureAction = "Lure";
        private const string FlareBinding = "<Keyboard>/q";
        private const string AdrenalineBinding = "<Keyboard>/e";
        private const string LureBinding = "<Keyboard>/f";
        private const string AdrenalineGestureBinding = "<GestureDevice>/adrenaline";
        private const string FormerSlowMoGestureBinding = "<GestureDevice>/slowMo";
        private const string LureGestureBinding = "<GestureDevice>/lure";
        private const string KeyboardMouseGroup = "Keyboard&Mouse";
        private const string TouchGroup = "Touch";

        // The noisemaker: a small dull can, seen only where something else lights it.
        private static readonly Vector3 LureBodySize = new(0.22f, 0.16f, 0.22f);
        private static readonly Color LureBodyColor = new(0.42f, 0.4f, 0.36f);

        public static FlareDefinition LoadOrCreateFlare()
        {
            var flare = LoadOrCreate<FlareDefinition>(FlareDefinitionPath);
            DrawIconIfEmpty(flare, HudIcons.Flare);
            return flare;
        }

        public static AdrenalineDefinition LoadOrCreateAdrenaline()
        {
            EnsureFolder(SkillsConfigurationFolder);
            if (AssetDatabase.LoadMainAssetAtPath(FormerSlowMoDefinitionPath) != null && AssetDatabase.LoadMainAssetAtPath(AdrenalineDefinitionPath) == null)
            {
                var error = AssetDatabase.MoveAsset(FormerSlowMoDefinitionPath, AdrenalineDefinitionPath);
                if (!string.IsNullOrEmpty(error))
                {
                    throw new System.InvalidOperationException($"Couldn't move {FormerSlowMoDefinitionPath}: {error}");
                }
            }

            // A heart: the character's pulse races.
            var adrenaline = LoadOrCreate<AdrenalineDefinition>(AdrenalineDefinitionPath);
            DrawIconIfEmpty(adrenaline, HudIcons.Heart);
            var serialized = new SerializedObject(adrenaline);
            var name = serialized.FindProperty("button.displayName");
            if (name.stringValue == "SLOW-MO")
            {
                name.stringValue = "ADRENALINE";
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return adrenaline;
        }

        public static LureDefinition LoadOrCreateLure()
        {
            var lure = LoadOrCreate<LureDefinition>(LureDefinitionPath);
            DrawIconIfEmpty(lure, HudIcons.Lure);
            var serialized = new SerializedObject(lure);
            var rattle = serialized.FindProperty("rattleClip");
            AddressableContent.ReferenceIfEmpty(rattle, () => GameplaySoundImport.Load(RattleClipPath), AddressableContent.SharedGroup);
            GameplaySoundImport.ApplyToReferenced(rattle);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return lure;
        }

        // A Skill's button icon is drawn by the HUD builder, unless another was chosen in the Inspector.
        private static void DrawIconIfEmpty(ScriptableObject definition, System.Func<Sprite> draw)
        {
            var serialized = new SerializedObject(definition);
            var icon = serialized.FindProperty("button.icon");
            if (icon.objectReferenceValue == null)
            {
                icon.objectReferenceValue = draw();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Gives the character its Skills (read from <paramref name="controls"/>, Flare light and Adrenaline heartbeat
        /// included), puts the Lure in the scene and adds the row of Skill buttons to the HUD canvas, then hands them to
        /// the composition root.
        /// </summary>
        public static void Build(GameRoot gameRoot, Transform character, PlayerControls controls, Canvas hud)
        {
            EnsureSkillActions();

            var serializedControls = new SerializedObject(controls);
            serializedControls.FindProperty("flare").objectReferenceValue = LoadActionReference($"{GameplayMap}/{FlareAction}");
            serializedControls.FindProperty("adrenaline").objectReferenceValue = LoadActionReference($"{GameplayMap}/{AdrenalineAction}");
            serializedControls.FindProperty("lure").objectReferenceValue = LoadActionReference($"{GameplayMap}/{LureAction}");
            serializedControls.ApplyModifiedPropertiesWithoutUndo();

            var flareObject = new GameObject("Flare", typeof(Light), typeof(FlareView));
            flareObject.transform.SetParent(character, false);
            var flareLight = flareObject.GetComponent<Light>();
            flareLight.type = LightType.Point;
            flareLight.shadows = LightShadows.None;
            flareLight.enabled = false;
            var flareView = flareObject.GetComponent<FlareView>();
            SetReference(flareView, "flareLight", flareLight);

            var heartbeat = new GameObject("Adrenaline Heartbeat", typeof(AudioSource)).GetComponent<AudioSource>();
            heartbeat.transform.SetParent(character, false);
            heartbeat.playOnAwake = false;
            heartbeat.loop = true;
            heartbeat.spatialBlend = 0f;
            AudioRouting.Route(heartbeat, AudioCategory.Sound);

            var lure = BuildLure(character, controls);

            var skills = character.gameObject.AddComponent<CharacterSkillsView>();
            SetReference(skills, "controls", controls);
            SetReference(skills, "flare", flareView);
            SetReference(skills, "adrenalineHeartbeat", heartbeat);
            SetReference(skills, "lure", lure);

            var skillBar = SkillHudBuilder.BuildSkillBar(hud, controls);

            var serializedRoot = new SerializedObject(gameRoot);
            serializedRoot.FindProperty("characterSkills").objectReferenceValue = skills;
            serializedRoot.FindProperty("skillBar").objectReferenceValue = skillBar;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        // The Lure lives in the Room, not on the character: hidden until thrown, with its rattle on its body.
        private static LureView BuildLure(Transform character, PlayerControls controls)
        {
            var root = new GameObject("Lure");
            var view = root.AddComponent<LureView>();

            // The body's origin is on the floor, where it lands; the can stands on it.
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            var can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            can.name = "Can";
            can.transform.SetParent(body.transform, false);
            can.transform.localPosition = Vector3.up * LureBodySize.y;
            can.transform.localScale = LureBodySize;
            Object.DestroyImmediate(can.GetComponent<Collider>());
            var renderer = can.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = TestRoomBuilder.LoadOrCreateMaterial("Lure", LureBodyColor);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var rattle = new GameObject("Rattle", typeof(AudioSource)).GetComponent<AudioSource>();
            rattle.transform.SetParent(body.transform, false);
            rattle.playOnAwake = false;
            rattle.loop = true;
            rattle.spatialBlend = 1f;
            AudioRouting.Route(rattle, AudioCategory.Sound);
            body.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("controls").objectReferenceValue = controls;
            serialized.FindProperty("thrower").objectReferenceValue = character;
            serialized.FindProperty("body").objectReferenceValue = body.transform;
            serialized.FindProperty("rattle").objectReferenceValue = rattle;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>
        /// Adds Gameplay/Flare (Q), Gameplay/Adrenaline (E) and Gameplay/Lure (F, and the Gesture device's lure button on
        /// touch) to the game's input actions if they're missing. Slow-mo's action and Gesture binding are renamed to
        /// Adrenaline's, keeping the action's id.
        /// </summary>
        private static void EnsureSkillActions()
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            var adrenaline = actions.FindAction($"{GameplayMap}/{AdrenalineAction}");
            if (actions.FindAction($"{GameplayMap}/{FlareAction}") != null
                && adrenaline != null
                && HasBinding(adrenaline, AdrenalineGestureBinding)
                && actions.FindAction($"{GameplayMap}/{LureAction}") != null)
            {
                return;
            }

            var edited = InputActionAsset.FromJson(actions.ToJson());
            var gameplay = edited.FindActionMap(GameplayMap, throwIfNotFound: true);
            if (gameplay.FindAction(FlareAction) == null)
            {
                gameplay.AddAction(FlareAction, InputActionType.Button, FlareBinding, groups: KeyboardMouseGroup);
            }

            gameplay.FindAction(FormerSlowMoAction)?.Rename(AdrenalineAction);
            var editedAdrenaline = gameplay.FindAction(AdrenalineAction)
                ?? gameplay.AddAction(AdrenalineAction, InputActionType.Button, AdrenalineBinding, groups: KeyboardMouseGroup);
            for (var i = 0; i < editedAdrenaline.bindings.Count; i++)
            {
                if (editedAdrenaline.bindings[i].path == FormerSlowMoGestureBinding)
                {
                    editedAdrenaline.ChangeBinding(i).WithPath(AdrenalineGestureBinding);
                }
            }

            if (!HasBinding(editedAdrenaline, AdrenalineGestureBinding))
            {
                editedAdrenaline.AddBinding(AdrenalineGestureBinding, groups: TouchGroup);
            }

            if (gameplay.FindAction(LureAction) == null)
            {
                var lure = gameplay.AddAction(LureAction, InputActionType.Button, LureBinding, groups: KeyboardMouseGroup);
                lure.AddBinding(LureGestureBinding, groups: TouchGroup);
            }

            System.IO.File.WriteAllText(controlsPath, edited.ToJson());
            Object.DestroyImmediate(edited);
            AssetDatabase.ImportAsset(controlsPath);
        }

        private static bool HasBinding(InputAction action, string path)
        {
            for (var i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].path == path)
                {
                    return true;
                }
            }

            return false;
        }

        private static InputActionReference LoadActionReference(string actionPath)
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            var action = actions.FindAction(actionPath, throwIfNotFound: true);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(controlsPath))
            {
                if (asset is InputActionReference reference && reference.action != null && reference.action.id == action.id)
                {
                    return reference;
                }
            }

            throw new System.InvalidOperationException($"No action reference for {actionPath} in {controlsPath}.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            EnsureFolder(SkillsConfigurationFolder);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            // Re-save so fields added since the asset was created are written with their defaults.
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void SetReference(Object owner, string propertyName, Object value)
        {
            var serialized = new SerializedObject(owner);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
