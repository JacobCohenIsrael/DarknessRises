using DarknessRises.Game.Boot;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DarknessRises.Game.EditModeTests.Boot
{
    /// <summary>
    /// The built boot scene opens on its loading screen, so a Level or the Shop is never seen before it's ready: the
    /// screen is wired to the boot, built covering everything, and sits over every scene's own canvases.
    /// </summary>
    public sealed class BootSceneTests
    {
        private const string ScenePath = "Assets/DarknessRises/Scenes/Boot.unity";
        private const string DefinitionPath = "Assets/DarknessRises/Boot/BootDefinition.asset";

        private Scene scene;
        private bool openedHere;

        [SetUp]
        public void SetUp()
        {
            scene = SceneManager.GetSceneByPath(ScenePath);
            openedHere = !scene.isLoaded;
            if (openedHere)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (openedHere)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private T Find<T>() where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        [Test]
        public void The_boot_has_its_loading_screen()
        {
            var bootstrap = Find<Bootstrap>();
            var loadingScreen = Find<LoadingScreenView>();

            Assert.That(loadingScreen, Is.Not.Null, "Rebuild the boot scene: it has no loading screen.");
            Assert.That(new SerializedObject(bootstrap).FindProperty("loadingScreen").objectReferenceValue, Is.SameAs(loadingScreen));
        }

        [Test]
        public void The_loading_screen_is_built_covering_the_whole_screen_with_its_label_off()
        {
            var loadingScreen = Find<LoadingScreenView>();
            var canvas = loadingScreen.GetComponent<Canvas>();
            var cover = loadingScreen.transform.Find("Cover").GetComponent<Image>();
            var coverRect = cover.rectTransform;

            Assert.That(canvas.enabled, Is.True, "The app opens covered, before the boot's first frame.");
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(cover.color.a, Is.EqualTo(1f), "Nothing shows through.");
            Assert.That(coverRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(coverRect.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(coverRect.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(coverRect.offsetMax, Is.EqualTo(Vector2.zero));
            Assert.That(loadingScreen.transform.Find("Label").gameObject.activeSelf, Is.False);
            Assert.That(loadingScreen.transform.Find("Download Bar").gameObject.activeSelf, Is.False);
            Assert.That(loadingScreen.transform.Find("Sizes").gameObject.activeSelf, Is.False);
        }

        [Test]
        public void The_download_bar_fills_from_its_left_edge()
        {
            var loadingScreen = Find<LoadingScreenView>();
            var fill = loadingScreen.transform.Find("Download Bar/Fill") as RectTransform;

            Assert.That(fill, Is.Not.Null);
            Assert.That(fill.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(fill.anchorMax, Is.EqualTo(new Vector2(0f, 1f)), "Empty until bytes arrive.");
            Assert.That(fill.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(fill.offsetMax, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void The_download_bar_is_authored_thin_and_under_the_label()
        {
            var settings = AssetDatabase.LoadAssetAtPath<BootDefinition>(DefinitionPath).LoadingScreen;

            Assert.That(settings.BarWidth, Is.GreaterThan(0f));
            Assert.That(settings.BarHeight, Is.GreaterThan(0f).And.LessThan(settings.FontSize), "A thin line, not a block.");
            Assert.That(settings.BarOffset, Is.GreaterThan(0f));
            Assert.That(settings.ProgressPollSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void The_loading_screen_takes_no_taps()
        {
            var loadingScreen = Find<LoadingScreenView>();

            Assert.That(loadingScreen.GetComponent<GraphicRaycaster>(), Is.Null);
            foreach (var graphic in loadingScreen.GetComponentsInChildren<Graphic>(true))
            {
                Assert.That(graphic.raycastTarget, Is.False, graphic.name);
            }
        }

        [Test]
        public void The_loading_screen_sits_over_every_scenes_canvases_and_under_the_version_label()
        {
            var definition = AssetDatabase.LoadAssetAtPath<BootDefinition>(DefinitionPath);
            var loading = definition.LoadingScreen.SortingOrder;

            Assert.That(loading, Is.LessThan(definition.VersionLabel.SortingOrder));
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/DarknessRises/Scenes" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == ScenePath)
                {
                    continue;
                }

                var other = SceneManager.GetSceneByPath(path);
                var opened = !other.isLoaded;
                if (opened)
                {
                    other = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }

                try
                {
                    foreach (var root in other.GetRootGameObjects())
                    {
                        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                        {
                            Assert.That(canvas.sortingOrder, Is.LessThan(loading), $"{path}: {canvas.name}");
                        }
                    }
                }
                finally
                {
                    if (opened)
                    {
                        EditorSceneManager.CloseScene(other, true);
                    }
                }
            }
        }

        [Test]
        public void The_label_is_authored()
        {
            var definition = AssetDatabase.LoadAssetAtPath<BootDefinition>(DefinitionPath);

            Assert.That(definition.LoadingScreen.Label, Is.Not.Empty);
            Assert.That(definition.LoadingScreen.LabelDelaySeconds, Is.GreaterThan(0f), "A quick change between Levels stays plain black.");
        }
    }
}
