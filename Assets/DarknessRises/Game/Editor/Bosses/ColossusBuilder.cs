using DarknessRises.Game.Audio;
using DarknessRises.Game.Bosses;
using DarknessRises.Game.Controls;
using DarknessRises.Game.Debugging;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Editor.Enemies;
using DarknessRises.Game.Editor.Health;
using DarknessRises.Game.Editor.Weapons;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DarknessRises.Game.Editor.Bosses
{
    /// <summary>
    /// Builds the Colossus: its definition asset (with placeholder Free Pack sounds), and its rig in a Boss Room: the
    /// foot that comes down with its glowing weak spot, the shadow the flash shows, the impact's light, shockwave and
    /// camera shake, the marked Positions on the floor and the Boss Room camera. Also adds the Relocate action
    /// (WASD and the arrow keys) and the skip-to-Boss debug key to the game's input actions.
    /// </summary>
    public static class ColossusBuilder
    {
        private const string DefinitionPath = "Assets/DarknessRises/Configuration/Bosses/Colossus.asset";
        private const string MaterialsFolder = "Assets/DarknessRises/Materials/Bosses";

        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        // Approved Free Pack placeholders.
        private const string GrowlClipPath = "Assets/Free Pack/Ghost Manifestation 2.wav";
        private const string StompClipPath = "Assets/Free Pack/Cannon impact 9.wav";
        private const string HurtClipPath = "Assets/Free Pack/Metal impact 5.wav";
        private const string DefeatClipPath = "Assets/Free Pack/Tsunami hitting a Large City.wav";
        // A whoosh that rises in a tenth of a second and peaks through the dash: the character dashing to its next
        // Position (issue #94). A placeholder until the user picks one by ear.
        private const string RelocateClipPath = "Assets/Free Pack/Magic Spell_Simple Swoosh_6.wav";

        // The dash's first placeholder, a punch, which never read as a dash. Replaced wherever it's still referenced.
        private const string FormerRelocateClipPath = "Assets/Free Pack/Indiana Jones Punch.wav";

        private const string GameplayMap = "Gameplay";
        private const string RelocateAction = "Relocate";
        private const string KeyboardMouseGroup = "Keyboard&Mouse";
        private const string SkipToBossBinding = "<Keyboard>/f3";

        // The placeholder foot: a dark block of a sole with the stump of a leg (kept short, so it never hides a Position
        // from the high Boss Room camera), and the weak spot a glowing bulge on the ankle at Shot height (the muzzle is
        // 1.3 m up), above the sole so it can be hit from any side.
        private static readonly Vector3 SoleSize = new(3f, 1f, 4f);
        private const float LegRadius = 0.9f;
        private const float LegHeight = 4.5f;
        private const float LegBackOffset = 0.7f;
        private static readonly Vector3 WeakSpotCentre = new(0f, 1.3f, -0.6f);
        private const float WeakSpotRadius = 0.6f;
        private const float WeakSpotHitRadius = 0.8f;

        private const float MarkerRadius = 0.5f;
        private const float FlatHeight = 0.02f;
        private static readonly Color FootColor = new(0.12f, 0.11f, 0.1f);
        private static readonly Color MarkerColor = new(0.2f, 0.26f, 0.3f);
        private static readonly Color ShockwaveColor = new(0.55f, 0.5f, 0.42f, 0.55f);

        // Where Shamblers drop from its back: around the Boss Room's edges, clear of its pillars, in metres from its centre.
        private static readonly Vector2[] AddDropPoints =
        {
            new(-10f, 8f),
            new(10f, 8f),
            new(-10f, -8f),
            new(10f, -8f),
            new(0f, 10f),
            new(-12.5f, 1f),
            new(12.5f, 1f),
        };

        public static ColossusDefinition LoadOrCreateColossus(string contentGroup)
        {
            TestRoomBuilder.EnsureFolder(System.IO.Path.GetDirectoryName(DefinitionPath).Replace('\\', '/'));
            var colossus = TestRoomBuilder.LoadOrCreate<ColossusDefinition>(DefinitionPath);
            var serialized = new SerializedObject(colossus);
            Reference(serialized, "growlClip", GrowlClipPath, contentGroup);
            Reference(serialized, "stompClip", StompClipPath, contentGroup);
            Reference(serialized, "hurtClip", HurtClipPath, contentGroup);
            Reference(serialized, "defeatClip", DefeatClipPath, contentGroup);
            // The Enemies swing with it too, so it stays in the Enemies group, where their builder keeps it.
            ReplaceFormerRelocateClip(serialized.FindProperty("relocateClip"));
            Reference(serialized, "relocateClip", RelocateClipPath, AddressableContent.EnemiesGroup);

            // From phase 2, Shamblers drop from its back.
            var addType = serialized.FindProperty("addType");
            if (addType.objectReferenceValue == null)
            {
                addType.objectReferenceValue = EnemyBuilder.LoadOrCreateShambler();
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Re-saved so fields added since it was created are written to disk with their defaults.
            EditorUtility.SetDirty(colossus);
            return colossus;
        }

        // The punch is replaced by the current dash sound and leaves the Level's content, which nothing else plays it in.
        private static void ReplaceFormerRelocateClip(SerializedProperty reference)
        {
            var referenced = AddressableContent.Referenced<AudioClip>(reference);
            if (referenced == null || AssetDatabase.GetAssetPath(referenced) != FormerRelocateClipPath)
            {
                return;
            }

            AddressableContent.Reference(reference, AssetDatabase.LoadAssetAtPath<AudioClip>(RelocateClipPath), AddressableContent.EnemiesGroup);
            AddressableContent.Remove(FormerRelocateClipPath);
        }

        private static void Reference(SerializedObject definition, string property, string clipPath, string group)
        {
            AddressableContent.ReferenceIfEmpty(definition.FindProperty(property), () => AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath), group);
        }

        /// <summary>
        /// Builds the Colossus's rig in a Boss Room whose floor's middle is <paramref name="roomCentre"/>, with a marker on
        /// each of the definition's Positions.
        /// </summary>
        public static ColossusView BuildRig(Transform parent, Vector3 roomCentre, ColossusDefinition colossus)
        {
            EnsureMaterialsFolder();
            var root = new GameObject("Colossus", typeof(ColossusView), typeof(AudioSource));
            root.transform.SetParent(parent, false);

            var centre = new GameObject("Room Centre").transform;
            centre.SetParent(root.transform, false);
            centre.position = roomCentre;

            BuildPositionMarkers(root.transform, colossus.PositionsAround(roomCentre));
            var bossCamera = BuildBossCamera(root.transform);
            var foot = BuildFoot(root.transform, out var footCollider, out var weakSpotCollider, out var weakSpotGlow, out var weakSpotLight);
            var shadowMaterial = LoadOrCreateTransparentUnlit("Stomp Shadow", Color.black);
            var shadow = Flat("Stomp Shadow", root.transform, shadowMaterial);
            var secondShadow = Flat("Second Stomp Shadow", root.transform, shadowMaterial);
            var drops = BuildAddDropPoints(root.transform, roomCentre);
            var shockwave = Flat("Shockwave", root.transform, LoadOrCreateTransparentUnlit("Shockwave", ShockwaveColor));
            var impactLight = ShadowlessLight("Impact Light", root.transform);

            var shake = new GameObject("Stomp Shake", typeof(CinemachineImpulseSource));
            shake.transform.SetParent(root.transform, false);

            var voice = root.GetComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 0f;
            AudioRouting.Route(voice, AudioCategory.Sound);

            var view = root.GetComponent<ColossusView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("roomCentre").objectReferenceValue = centre;
            serialized.FindProperty("bossCamera").objectReferenceValue = bossCamera;
            serialized.FindProperty("foot").objectReferenceValue = foot;
            serialized.FindProperty("footCollider").objectReferenceValue = footCollider;
            serialized.FindProperty("shadow").objectReferenceValue = shadow;
            serialized.FindProperty("secondShadow").objectReferenceValue = secondShadow;
            var dropsProperty = serialized.FindProperty("addDropPoints");
            dropsProperty.arraySize = drops.Length;
            for (var i = 0; i < drops.Length; i++)
            {
                dropsProperty.GetArrayElementAtIndex(i).objectReferenceValue = drops[i];
            }

            serialized.FindProperty("weakSpotCollider").objectReferenceValue = weakSpotCollider;
            serialized.FindProperty("weakSpotGlow").objectReferenceValue = weakSpotGlow;
            serialized.FindProperty("weakSpotLight").objectReferenceValue = weakSpotLight;
            serialized.FindProperty("impactLight").objectReferenceValue = impactLight;
            serialized.FindProperty("shockwave").objectReferenceValue = shockwave;
            serialized.FindProperty("impulseSource").objectReferenceValue = shake.GetComponent<CinemachineImpulseSource>();
            serialized.FindProperty("voice").objectReferenceValue = voice;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            TestRoomBuilder.SetReference(weakSpotCollider.GetComponent<ColossusWeakSpot>(), "colossus", view);
            return view;
        }

        /// <summary>Gives the character's controls the Gameplay/Relocate action, adding it to the input actions if it's missing.</summary>
        internal static void WireRelocateInput(PlayerControls controls)
        {
            EnsureRelocateAction();
            TestRoomBuilder.SetReference(controls, "relocate", LoadActionReference($"{GameplayMap}/{RelocateAction}"));
        }

        /// <summary>Gives the Game Root the Debug/SkipToBoss action (F3), adding it to the input actions if it's missing.</summary>
        internal static void WireDebugSkip(GameRoot gameRoot)
        {
            var action = HealthBuilder.LoadOrAddDebugAction(BossSkipDebug.ActionName, SkipToBossBinding);
            TestRoomBuilder.SetReference(gameRoot, "skipToBoss", action);
        }

        // A faint mark on the floor at each Position: barely there in the Darkness, plain in a flash.
        private static void BuildPositionMarkers(Transform parent, Vector3[] positions)
        {
            var markers = new GameObject("Positions").transform;
            markers.SetParent(parent, false);
            var material = LoadOrCreate("Position Marker", UnlitShaderName, MarkerColor);
            for (var i = 0; i < positions.Length; i++)
            {
                var marker = Flat($"Position {i + 1}", markers, material);
                marker.transform.position = positions[i] + Vector3.up * FlatHeight;
                marker.transform.localScale = new Vector3(MarkerRadius * 2f, FlatHeight, MarkerRadius * 2f);
                marker.enabled = true;
                GameObjectUtility.SetStaticEditorFlags(marker.gameObject, StaticEditorFlags.BatchingStatic);
            }
        }

        private static Transform[] BuildAddDropPoints(Transform parent, Vector3 roomCentre)
        {
            var drops = new GameObject("Add Drop Points").transform;
            drops.SetParent(parent, false);
            var points = new Transform[AddDropPoints.Length];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = new GameObject($"Add Drop Point {i + 1}").transform;
                points[i].SetParent(drops, false);
                points[i].position = roomCentre + new Vector3(AddDropPoints[i].x, 0f, AddDropPoints[i].y);
            }

            return points;
        }

        // Off until the fight starts. It has no follow target: the fight frames it once, over every Position.
        private static CinemachineCamera BuildBossCamera(Transform parent)
        {
            var cameraObject = new GameObject("Boss Room Camera", typeof(CinemachineCamera));
            cameraObject.transform.SetParent(parent, false);

            var listener = cameraObject.AddComponent<CinemachineImpulseListener>();
            listener.ApplyAfter = CinemachineCore.Stage.Noise;
            listener.ChannelMask = 1;
            listener.Gain = 1f;
            listener.UseCameraSpace = true;
            listener.ReactionSettings = new CinemachineImpulseListener.ImpulseReaction
            {
                AmplitudeGain = 1f,
                FrequencyGain = 1f,
                Duration = 1f,
            };

            cameraObject.SetActive(false);
            return cameraObject.GetComponent<CinemachineCamera>();
        }

        private static Transform BuildFoot(
            Transform parent,
            out Collider footCollider,
            out Collider weakSpotCollider,
            out Renderer weakSpotGlow,
            out Light weakSpotLight)
        {
            var footMaterial = LoadOrCreate("Colossus Foot", LitShaderName, FootColor);
            var foot = new GameObject("Foot").transform;
            foot.SetParent(parent, false);

            var sole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sole.name = "Sole";
            sole.transform.SetParent(foot, false);
            sole.transform.localPosition = Vector3.up * (SoleSize.y / 2f);
            sole.transform.localScale = SoleSize;
            Paint(sole, footMaterial);
            footCollider = sole.GetComponent<Collider>();
            sole.AddComponent<ColossusFoot>();

            var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "Leg";
            Object.DestroyImmediate(leg.GetComponent<Collider>());
            leg.transform.SetParent(foot, false);
            leg.transform.localPosition = new Vector3(0f, SoleSize.y + LegHeight / 2f, LegBackOffset);
            leg.transform.localScale = new Vector3(LegRadius * 2f, LegHeight / 2f, LegRadius * 2f);
            Paint(leg, footMaterial);

            var weakSpot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            weakSpot.name = "Weak Spot";
            weakSpot.transform.SetParent(foot, false);
            weakSpot.transform.localPosition = WeakSpotCentre;
            weakSpot.transform.localScale = Vector3.one * (WeakSpotRadius * 2f);
            var glow = weakSpot.GetComponent<MeshRenderer>();
            glow.sharedMaterial = LoadOrCreate("Colossus Weak Spot", UnlitShaderName, Color.white);
            glow.shadowCastingMode = ShadowCastingMode.Off;
            var hitArea = (SphereCollider)weakSpot.GetComponent<Collider>();
            hitArea.radius = 0.5f * WeakSpotHitRadius / WeakSpotRadius;
            weakSpot.AddComponent<ColossusWeakSpot>();
            weakSpotCollider = hitArea;
            weakSpotGlow = glow;

            weakSpotLight = ShadowlessLight("Weak Spot Light", foot);
            weakSpotLight.transform.localPosition = WeakSpotCentre + Vector3.back * WeakSpotRadius * 2f;

            foot.gameObject.SetActive(false);
            return foot;
        }

        // A thin disc on the floor without a collider: a shadow, a shockwave or a marker.
        private static Renderer Flat(string name, Transform parent, Material material)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(parent, false);
            disc.transform.localScale = new Vector3(1f, FlatHeight, 1f);
            var renderer = disc.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }

        private static Light ShadowlessLight(string name, Transform parent)
        {
            var light = new GameObject(name, typeof(Light)).GetComponent<Light>();
            light.transform.SetParent(parent, false);
            light.type = LightType.Point;
            light.shadows = LightShadows.None;
            light.intensity = 0f;
            light.enabled = false;
            light.GetUniversalAdditionalLightData();
            return light;
        }

        private static void Paint(GameObject gameObject, Material material)
        {
            var renderer = gameObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static Material LoadOrCreate(string name, string shaderName, Color baseColor)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(shaderName)) { name = name };
            material.SetColor("_BaseColor", baseColor);
            if (shaderName == LitShaderName)
            {
                material.SetFloat("_Smoothness", 0.1f);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Kept as an asset so its shader variant ships.
        private static Material LoadOrCreateTransparentUnlit(string name, Color baseColor)
        {
            var path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find(UnlitShaderName)) { name = name };
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureMaterialsFolder()
        {
            TestRoomBuilder.EnsureFolder(MaterialsFolder);
        }

        /// <summary>Adds Gameplay/Relocate (a direction from WASD or the arrow keys) to the input actions if it's missing.</summary>
        private static void EnsureRelocateAction()
        {
            const string controlsPath = PistolBuilder.ControlsPath;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(controlsPath);
            if (actions.FindAction($"{GameplayMap}/{RelocateAction}") != null)
            {
                return;
            }

            var edited = InputActionAsset.FromJson(actions.ToJson());
            var gameplay = edited.FindActionMap(GameplayMap, throwIfNotFound: true);
            var relocate = gameplay.AddAction(RelocateAction, InputActionType.Value, expectedControlLayout: "Vector2");
            relocate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", KeyboardMouseGroup)
                .With("Down", "<Keyboard>/s", KeyboardMouseGroup)
                .With("Left", "<Keyboard>/a", KeyboardMouseGroup)
                .With("Right", "<Keyboard>/d", KeyboardMouseGroup);
            relocate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow", KeyboardMouseGroup)
                .With("Down", "<Keyboard>/downArrow", KeyboardMouseGroup)
                .With("Left", "<Keyboard>/leftArrow", KeyboardMouseGroup)
                .With("Right", "<Keyboard>/rightArrow", KeyboardMouseGroup);

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
    }
}
