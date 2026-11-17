using DarknessRises.Game.Controls;
using DarknessRises.Game.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace DarknessRises.Game.Editor.Weapons
{
    /// <summary>
    /// Builds the Pistol: its definition asset and pooled Shot prefab, the weapon mount on the character,
    /// and the player's controls. Existing assets are kept, so tuning done in the Inspector survives a rebuild.
    /// </summary>
    public static class PistolBuilder
    {
        private const string Root = "Assets/DarknessRises";
        private const string WeaponsConfigurationFolder = Root + "/Configuration/Weapons";
        private const string WeaponsPrefabsFolder = Root + "/Prefabs/Weapons";
        private const string MaterialsFolder = Root + "/Materials";

        public const string PistolDefinitionPath = WeaponsConfigurationFolder + "/Pistol.asset";
        public const string ControlsPath = Root + "/Input/Controls.inputactions";
        private const string ShotPrefabPath = WeaponsPrefabsFolder + "/Shot.prefab";

        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        private const string ParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        private const string AimActionPath = "Gameplay/Aim";
        private const string FireActionPath = "Gameplay/Fire";
        private const string ReloadActionPath = "Gameplay/Reload";

        // Placeholder weapon layout on the character, in metres.
        private const float MountHeight = 1.3f;
        private static readonly Vector3 PlaceholderPistolSize = new(0.12f, 0.14f, 0.45f);
        private const float PlaceholderPistolForward = 0.5f;
        private const float MuzzleForward = 0.75f;
        private const float ShotTrailMinVertexDistance = 0.05f;

        /// <summary>Loads the Pistol definition, creating it and its Shot prefab if they're missing.</summary>
        public static WeaponDefinition LoadOrCreatePistol()
        {
            EnsureFolder(WeaponsConfigurationFolder);
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(PistolDefinitionPath);
            if (pistol == null)
            {
                // The definition's defaults are the Pistol: 12-shot magazine, 4 shots per second.
                pistol = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(pistol, PistolDefinitionPath);
            }

            var serialized = new SerializedObject(pistol);
            var prefab = serialized.FindProperty("shot.prefab");
            if (prefab.objectReferenceValue == null)
            {
                prefab.objectReferenceValue = LoadOrCreateShotPrefab();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return pistol;
        }

        /// <summary>
        /// Adds the player's controls and the Primary weapon to the character: a mount that turns toward the aim point,
        /// a placeholder Pistol and the muzzle Shots leave from.
        /// </summary>
        public static PrimaryWeaponView BuildPrimaryWeapon(Transform character, Camera viewCamera)
        {
            var controls = character.gameObject.AddComponent<PlayerControls>();
            var serializedControls = new SerializedObject(controls);
            serializedControls.FindProperty("aim").objectReferenceValue = LoadActionReference(AimActionPath);
            serializedControls.FindProperty("fire").objectReferenceValue = LoadActionReference(FireActionPath);
            serializedControls.FindProperty("reload").objectReferenceValue = LoadActionReference(ReloadActionPath);
            serializedControls.FindProperty("viewCamera").objectReferenceValue = viewCamera;
            serializedControls.ApplyModifiedPropertiesWithoutUndo();

            var mount = new GameObject("Weapon Mount").transform;
            mount.SetParent(character, false);
            mount.localPosition = Vector3.up * MountHeight;

            var placeholder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placeholder.name = "Placeholder Pistol";
            Object.DestroyImmediate(placeholder.GetComponent<Collider>());
            placeholder.transform.SetParent(mount, false);
            placeholder.transform.localPosition = Vector3.forward * PlaceholderPistolForward;
            placeholder.transform.localScale = PlaceholderPistolSize;
            placeholder.GetComponent<MeshRenderer>().sharedMaterial =
                LoadOrCreateMaterial("Weapon", LitShaderName, new Color(0.12f, 0.12f, 0.13f));

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(mount, false);
            muzzle.localPosition = Vector3.forward * MuzzleForward;

            var view = character.gameObject.AddComponent<PrimaryWeaponView>();
            var serializedView = new SerializedObject(view);
            serializedView.FindProperty("controls").objectReferenceValue = controls;
            serializedView.FindProperty("mount").objectReferenceValue = mount;
            serializedView.FindProperty("muzzle").objectReferenceValue = muzzle;
            serializedView.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static ShotView LoadOrCreateShotPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ShotView>(ShotPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(WeaponsPrefabsFolder);

            var shot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shot.name = "Shot";
            Object.DestroyImmediate(shot.GetComponent<Collider>());

            var body = shot.GetComponent<MeshRenderer>();
            body.sharedMaterial = LoadOrCreateMaterial("ShotGlow", UnlitShaderName, Color.white);
            body.shadowCastingMode = ShadowCastingMode.Off;
            body.receiveShadows = false;

            var trail = shot.AddComponent<TrailRenderer>();
            trail.sharedMaterial = LoadOrCreateMaterial("ShotTrail", ParticlesUnlitShaderName, Color.white);
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.minVertexDistance = ShotTrailMinVertexDistance;
            trail.autodestruct = false;

            var view = shot.AddComponent<ShotView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("body").objectReferenceValue = body;
            serialized.FindProperty("trail").objectReferenceValue = trail;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(shot, ShotPrefabPath);
            Object.DestroyImmediate(shot);
            return prefab.GetComponent<ShotView>();
        }

        private static InputActionReference LoadActionReference(string actionPath)
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            var action = actions.FindAction(actionPath, throwIfNotFound: true);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControlsPath))
            {
                if (asset is InputActionReference reference && reference.action != null && reference.action.id == action.id)
                {
                    return reference;
                }
            }

            throw new System.InvalidOperationException($"No action reference for {actionPath} in {ControlsPath}.");
        }

        private static Material LoadOrCreateMaterial(string name, string shaderName, Color baseColor)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(shaderName)) { name = name };
            material.SetColor("_BaseColor", baseColor);
            AssetDatabase.CreateAsset(material, path);
            return material;
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
