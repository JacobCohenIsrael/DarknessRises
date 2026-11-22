using DarknessRises.Game.Controls;
using DarknessRises.Game.Editor.Hud;
using DarknessRises.Game.Editor.Weapons;
using DarknessRises.Game.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarknessRises.Game.Editor.Skills
{
    /// <summary>
    /// Builds the Skills: the Flare and Slow-mo definitions, their input actions (Q and E), the Flare light and
    /// Skill runner on the character, and their buttons on the HUD. Existing definitions are kept.
    /// </summary>
    public static class SkillsBuilder
    {
        private const string SkillsConfigurationFolder = "Assets/DarknessRises/Configuration/Skills";
        private const string FlareDefinitionPath = SkillsConfigurationFolder + "/Flare.asset";
        private const string SlowMoDefinitionPath = SkillsConfigurationFolder + "/SlowMo.asset";

        private const string GameplayMap = "Gameplay";
        private const string FlareAction = "Flare";
        private const string SlowMoAction = "SlowMo";
        private const string FlareBinding = "<Keyboard>/q";
        private const string SlowMoBinding = "<Keyboard>/e";
        private const string KeyboardMouseGroup = "Keyboard&Mouse";

        public static FlareDefinition LoadOrCreateFlare()
        {
            var flare = LoadOrCreate<FlareDefinition>(FlareDefinitionPath);
            DrawIconIfEmpty(flare, HudIcons.Flare);
            return flare;
        }

        public static SlowMoDefinition LoadOrCreateSlowMo()
        {
            // A heart: Slow-mo becomes Adrenaline.
            var slowMo = LoadOrCreate<SlowMoDefinition>(SlowMoDefinitionPath);
            DrawIconIfEmpty(slowMo, HudIcons.Heart);
            return slowMo;
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
        /// Gives the character its Skills (read from <paramref name="controls"/>, Flare light included) and adds the row
        /// of Skill buttons to the HUD canvas, then hands them to the composition root.
        /// </summary>
        public static void Build(GameRoot gameRoot, Transform character, PlayerControls controls, Canvas hud)
        {
            EnsureSkillActions();

            var serializedControls = new SerializedObject(controls);
            serializedControls.FindProperty("flare").objectReferenceValue = LoadActionReference($"{GameplayMap}/{FlareAction}");
            serializedControls.FindProperty("slowMo").objectReferenceValue = LoadActionReference($"{GameplayMap}/{SlowMoAction}");
            serializedControls.ApplyModifiedPropertiesWithoutUndo();

            var flareObject = new GameObject("Flare", typeof(Light), typeof(FlareView));
            flareObject.transform.SetParent(character, false);
            var flareLight = flareObject.GetComponent<Light>();
            flareLight.type = LightType.Point;
            flareLight.shadows = LightShadows.None;
            flareLight.enabled = false;
            var flareView = flareObject.GetComponent<FlareView>();
            SetReference(flareView, "flareLight", flareLight);

            var skills = character.gameObject.AddComponent<CharacterSkillsView>();
            SetReference(skills, "controls", controls);
            SetReference(skills, "flare", flareView);

            var skillBar = SkillHudBuilder.BuildSkillBar(hud, controls);

            var serializedRoot = new SerializedObject(gameRoot);
            serializedRoot.FindProperty("characterSkills").objectReferenceValue = skills;
            serializedRoot.FindProperty("skillBar").objectReferenceValue = skillBar;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Adds Gameplay/Flare (Q) and Gameplay/SlowMo (E) to the game's input actions if they're missing.</summary>
        private static void EnsureSkillActions()
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            if (actions.FindAction($"{GameplayMap}/{FlareAction}") != null && actions.FindAction($"{GameplayMap}/{SlowMoAction}") != null)
            {
                return;
            }

            var edited = InputActionAsset.FromJson(actions.ToJson());
            var gameplay = edited.FindActionMap(GameplayMap, throwIfNotFound: true);
            if (gameplay.FindAction(FlareAction) == null)
            {
                gameplay.AddAction(FlareAction, InputActionType.Button, FlareBinding, groups: KeyboardMouseGroup);
            }

            if (gameplay.FindAction(SlowMoAction) == null)
            {
                gameplay.AddAction(SlowMoAction, InputActionType.Button, SlowMoBinding, groups: KeyboardMouseGroup);
            }

            System.IO.File.WriteAllText(controlsPath, edited.ToJson());
            Object.DestroyImmediate(edited);
            AssetDatabase.ImportAsset(controlsPath);
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
