using DarknessRises.Game.Configuration;
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
    /// character that plays the magazine-out and magazine-in sounds. Authors the placeholder sounds if none are set.
    /// Call it once the character, the camera and the HUD are built.
    /// </summary>
    public static class ReloadFeedbackBuilder
    {
        // Approved Free Pack placeholders, until a proper reload sound is approved. Short, sharp click out; ringing clack in.
        private const string MagazineOutClipPath = "Assets/Free Pack/Bullet Impact 21.wav";
        private const string MagazineInClipPath = "Assets/Free Pack/Metal Impact 7.wav";

        private const string DiscSpritePath = "UI/Skin/Knob.psd";

        public static void Build(GameRoot gameRoot, Transform character, Camera worldCamera)
        {
            var serializedRoot = new SerializedObject(gameRoot);
            var configuration = (GameConfiguration)serializedRoot.FindProperty("configuration").objectReferenceValue;
            var hud = ((Component)serializedRoot.FindProperty("ammoHud").objectReferenceValue).transform.parent;

            AuthorPlaceholderSounds(configuration);
            serializedRoot.FindProperty("reloadMeter").objectReferenceValue = BuildMeter(hud, worldCamera);
            serializedRoot.FindProperty("reloadSound").objectReferenceValue = BuildSound(character);
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AuthorPlaceholderSounds(GameConfiguration configuration)
        {
            var serialized = new SerializedObject(configuration);
            ReferenceIfEmpty(serialized.FindProperty("reloadFeedback.magazineOutClip"), MagazineOutClipPath);
            ReferenceIfEmpty(serialized.FindProperty("reloadFeedback.magazineInClip"), MagazineInClipPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Written to disk with the settings' defaults, even when the clips were already set.
            EditorUtility.SetDirty(configuration);
        }

        // Loaded through Addressables with the Level's other content.
        private static void ReferenceIfEmpty(SerializedProperty clip, string path)
        {
            AddressableContent.ReferenceIfEmpty(clip, () => AssetDatabase.LoadAssetAtPath<AudioClip>(path), AddressableContent.SharedGroup);
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

            var view = soundObject.GetComponent<ReloadSoundView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("source").objectReferenceValue = source;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
