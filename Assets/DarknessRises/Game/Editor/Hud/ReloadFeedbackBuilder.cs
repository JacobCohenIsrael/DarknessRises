using DarknessRises.Game.Audio;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Hud
{
    /// <summary>
    /// Builds the reload feedback: the radial meter on the HUD that follows the character, and the 2D source on the
    /// character that plays the reload sequence. Authors the sound if none is set.
    /// Call it once the character, the camera and the HUD are built.
    /// </summary>
    public static class ReloadFeedbackBuilder
    {
        // A whole reload sequence in one clip (Weapons of Choice - FREE, Komposite Sound), approved for the game.
        private const string ClipPath = "Assets/Weapons of Choice FREE - Komposite Sound/GUN/Handling_Gun_01_Reload_Sq_SFX.wav";

        // The Free Pack placeholders it replaced, a magazine-out and a magazine-in sound. Nothing else uses them.
        private static readonly string[] FormerClipPaths =
        {
            "Assets/Free Pack/Bullet Impact 21.wav",
            "Assets/Free Pack/Metal Impact 7.wav",
        };

        private const string DiscSpritePath = "UI/Skin/Knob.psd";

        public static void Build(GameRoot gameRoot, Transform character, Camera worldCamera)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var configuration = (GameConfiguration)serializedRoot.FindProperty("configuration").objectReferenceValue;
            var hud = ((Component)serializedRoot.FindProperty("ammoHud").objectReferenceValue).transform.parent;

            AuthorSound(configuration);
            serializedRoot.FindProperty("reloadMeter").objectReferenceValue = BuildMeter(hud, worldCamera);
            serializedRoot.FindProperty("reloadSound").objectReferenceValue = BuildSound(character);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Points the reload sound at the reload sequence, unless another clip was chosen in the Inspector, with its
        /// import settings, and takes the former placeholders out of the content so no bundle carries them.
        /// </summary>
        public static void AuthorSound(GameConfiguration configuration)
        {
            var settings = AddressableContent.Setup();
            foreach (var path in FormerClipPaths)
            {
                settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(path));
            }

            var serialized = new SerializedObject(configuration);
            // Loaded through Addressables with the Level's other content.
            AddressableContent.ReferenceIfEmpty(
                serialized.FindProperty("reloadFeedback.clip"), () => GameplaySoundImport.Load(ClipPath), AddressableContent.SharedGroup);
            GameplaySoundImport.ApplyToReferenced(serialized.FindProperty("reloadFeedback.clip"));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Written to disk with the settings' defaults, even when the clip was already set.
            EditorUtility.SetDirty(configuration);
        }

        private static ReloadMeterView BuildMeter(Transform hud, Camera worldCamera)
        {
            var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(DiscSpritePath);

            // Stretched over the HUD and drawn first, behind the rest of the HUD.
            var root = new GameObject("Reload Meter", typeof(RectTransform), typeof(ReloadMeterView));
            var area = root.GetComponent<RectTransform>();
            area.SetParent(hud, false);
            area.SetAsFirstSibling();
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = area.offsetMax = Vector2.zero;

            var meterObject = new GameObject("Meter", typeof(RectTransform));
            var meter = meterObject.GetComponent<RectTransform>();
            meter.SetParent(area, false);
            meter.anchorMin = meter.anchorMax = meter.pivot = new Vector2(0.5f, 0.5f);

            var background = AddImage(meter, "Background", sprite);
            var fill = AddImage(meter, "Fill", sprite);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillClockwise = true;
            fill.fillAmount = 0f;

            var view = root.GetComponent<ReloadMeterView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("area").objectReferenceValue = area;
            serialized.FindProperty("meter").objectReferenceValue = meter;
            serialized.FindProperty("worldCamera").objectReferenceValue = worldCamera;
            serialized.FindProperty("background").objectReferenceValue = background;
            serialized.FindProperty("fill").objectReferenceValue = fill;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            meterObject.SetActive(false);
            return view;
        }

        private static Image AddImage(RectTransform parent, string name, Sprite sprite)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static ReloadSoundView BuildSound(Transform character)
        {
            var soundObject = new GameObject("Reload Sound", typeof(AudioSource), typeof(ReloadSoundView));
            soundObject.transform.SetParent(character, false);
            var source = soundObject.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            AudioRouting.Route(source, AudioCategory.Sound);

            var view = soundObject.GetComponent<ReloadSoundView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("source").objectReferenceValue = source;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
