using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarknessRises.Game.Editor.Lightning
{
    /// <summary>
    /// The shadow settings of every URP asset, kept as code so they're the source of truth: release builds touch the
    /// assets, and their side effects get reverted, so <see cref="ProjectSetup"/> and every player build apply these.
    /// Lightning's key light is the only real-time shadow caster (AGENTS.md rule 9), so these are its shadows.
    /// </summary>
    /// <remarks>
    /// The shadow map covers only what the Ride and Boss Room cameras see: the far edge of the Boss Room's view is about
    /// 30 m deep, so the map's texels spread over 32 m instead of the template's 50 m, and one cascade covers it. The
    /// Mobile asset (Android, iOS and WebGL) doubles its map to 2048; the PC asset keeps two cascades for its desktop
    /// budget. Soft shadows filter the edges (Medium: a 5x5 tent), so the long shadows of a low strike no longer
    /// stair-step while staying sharp enough to read as a flash; Low left visible steps on a looming shadow. The shadow
    /// pass only runs while a flash lights the key light, a fraction of a second every few seconds. Biases are in
    /// shadow-map texels, so the sharper map needs less world offset for the same: low enough that thin limbs keep their
    /// shadows, high enough that lit floors don't acne.
    /// </remarks>
    public static class LightningShadowSetup
    {
        private const string MobileAssetName = "Mobile_RPAsset";

        private const float ShadowDistanceMetres = 32f;
        private const int MobileShadowmapResolution = 2048;
        private const int PcShadowmapResolution = 2048;
        private const int MobileCascadeCount = 1;
        private const int PcCascadeCount = 2;
        private const float PcSecondCascadeSplit = 0.45f;
        private const float DepthBiasTexels = 0.6f;
        private const float NormalBiasTexels = 0.5f;
        private const SoftShadowQuality SoftShadowEdges = SoftShadowQuality.Medium;

        /// <summary>Applies the shadow settings to every URP asset in the project.</summary>
        public static void Apply()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                Apply(asset, asset.name == MobileAssetName);
            }
        }

        private static void Apply(UniversalRenderPipelineAsset asset, bool mobile)
        {
            var serialized = new SerializedObject(asset);

            // Only Lightning (the main light) casts real-time shadows, so a stray shadowed point light costs nothing.
            serialized.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;

            serialized.FindProperty("m_MainLightShadowmapResolution").intValue = mobile ? MobileShadowmapResolution : PcShadowmapResolution;
            serialized.FindProperty("m_ShadowDistance").floatValue = ShadowDistanceMetres;
            serialized.FindProperty("m_ShadowCascadeCount").intValue = mobile ? MobileCascadeCount : PcCascadeCount;
            serialized.FindProperty("m_Cascade2Split").floatValue = PcSecondCascadeSplit;
            serialized.FindProperty("m_ShadowDepthBias").floatValue = DepthBiasTexels;
            serialized.FindProperty("m_ShadowNormalBias").floatValue = NormalBiasTexels;
            serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
            serialized.FindProperty("m_SoftShadowQuality").intValue = (int)SoftShadowEdges;

            if (serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(asset);
                Debug.Log($"[LightningShadowSetup] Applied Lightning's shadow settings to {asset.name}.");
            }
        }
    }
}
