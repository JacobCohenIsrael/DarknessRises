using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Splash
{
    /// <summary>
    /// What the player sees while the game opens, kept as code so a build can't drift from it: The Gamefather's logo on
    /// black, in place of Unity's logo and "Made with Unity" (issue #122). <see cref="ProjectSetup"/> and every player
    /// build apply it, as they do <see cref="Lightning.LightningShadowSetup"/>.
    /// </summary>
    /// <remarks>
    /// Two screens carry the logo. Unity's splash screen (Android and Web) shows it once the engine runs, and the Web
    /// page's own loader (<see cref="WebTemplateName"/>, under <c>Assets/WebGLTemplates</c>) shows it while the engine
    /// downloads, with a thin bar like the loading screen's. Both are black, like the loading screen that follows them.
    /// Unity 6 lets every plan turn the Unity logo and the whole splash off; <see cref="Apply"/> reads the settings back
    /// and fails loudly if the Editor refused them.
    /// </remarks>
    public static class SplashScreenSetup
    {
        public const string LogoPath = "Assets/DarknessRises/Sprites/the-gamefather-white-v2.png";
        public const string WebTemplateName = "Gamefather";

        private const string WebTemplateSetting = "PROJECT:" + WebTemplateName;
        private const string WebTemplateLogoPath = "Assets/WebGLTemplates/" + WebTemplateName + "/TemplateData/gamefather-logo.png";

        // Seconds the logo is on screen, fades included: long enough to read the wordmark, short enough not to keep anyone waiting.
        private const float LogoSeconds = 2f;

        // The logo is a 1672 x 941 wordmark shown full-screen on a phone: full size, no mipmaps, and high-quality compression
        // (ASTC 4x4 on Android), which keeps a flat white wordmark's edges clean at a quarter of RGBA32's 6 MB.
        private const int LogoMaxTextureSize = 2048;
        private const TextureImporterCompression LogoCompression = TextureImporterCompression.CompressedHQ;

        // No background image, so nothing to dim: the black shows as it is.
        private const float OverlayOpacity = 0f;

        // Static: the logo fades in and out without zooming.
        private const PlayerSettings.SplashScreen.AnimationMode Animation = PlayerSettings.SplashScreen.AnimationMode.Static;

        /// <summary>The splash's background and the Web page's: black, like the loading screen that comes next.</summary>
        public static readonly Color BackgroundColor = Color.black;

        [MenuItem("Darkness Rises/Apply Splash Screen")]
        public static void Apply()
        {
            var logo = ImportLogo();

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            PlayerSettings.SplashScreen.animationMode = Animation;
            PlayerSettings.SplashScreen.backgroundColor = BackgroundColor;
            PlayerSettings.SplashScreen.background = null;
            PlayerSettings.SplashScreen.backgroundPortrait = null;
            PlayerSettings.SplashScreen.overlayOpacity = OverlayOpacity;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(LogoSeconds, logo) };

            PlayerSettings.WebGL.template = WebTemplateSetting;
            CopyLogoToWebTemplate();

            Verify(logo);
            Debug.Log($"[SplashScreenSetup] {Describe()}");
        }

        /// <summary>The splash settings as the Editor holds them now, for build logs.</summary>
        public static string Describe()
        {
            var logos = PlayerSettings.SplashScreen.logos;
            var names = new string[logos.Length];
            for (var i = 0; i < logos.Length; i++)
            {
                names[i] = logos[i].logo != null ? $"{logos[i].logo.name} {logos[i].duration:0.##} s" : $"(Unity logo) {logos[i].duration:0.##} s";
            }

            return $"show={PlayerSettings.SplashScreen.show}, showUnityLogo={PlayerSettings.SplashScreen.showUnityLogo}, " +
                   $"drawMode={PlayerSettings.SplashScreen.drawMode}, animation={PlayerSettings.SplashScreen.animationMode}, " +
                   $"background={PlayerSettings.SplashScreen.backgroundColor}, overlayOpacity={PlayerSettings.SplashScreen.overlayOpacity}, " +
                   $"logos=[{string.Join(", ", names)}], webTemplate={PlayerSettings.WebGL.template}";
        }

        private static Sprite ImportLogo()
        {
            if (AssetImporter.GetAtPath(LogoPath) is not TextureImporter importer)
            {
                throw new FileNotFoundException($"The Gamefather logo isn't in the project at {LogoPath}.");
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            var changed = importer.textureType != TextureImporterType.Sprite
                          || settings.spriteMode != (int)SpriteImportMode.Single
                          || importer.mipmapEnabled
                          || !importer.alphaIsTransparency
                          || importer.textureCompression != LogoCompression
                          || importer.maxTextureSize != LogoMaxTextureSize
                          || importer.wrapMode != TextureWrapMode.Clamp
                          || importer.filterMode != FilterMode.Bilinear;
            if (changed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = LogoCompression;
                importer.maxTextureSize = LogoMaxTextureSize;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
                Debug.Log("[SplashScreenSetup] Imported The Gamefather logo as a full-screen sprite.");
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
            if (sprite == null)
            {
                throw new FileNotFoundException($"The Gamefather logo at {LogoPath} didn't import as a sprite.");
            }

            return sprite;
        }

        /// <summary>The Web page shows the same file; the sprite is its one source.</summary>
        private static void CopyLogoToWebTemplate()
        {
            var source = File.ReadAllBytes(LogoPath);
            if (File.Exists(WebTemplateLogoPath) && File.ReadAllBytes(WebTemplateLogoPath).AsSpan().SequenceEqual(source))
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(WebTemplateLogoPath)!);
            File.WriteAllBytes(WebTemplateLogoPath, source);
            AssetDatabase.ImportAsset(WebTemplateLogoPath);
            Debug.Log($"[SplashScreenSetup] Copied the logo to the Web template: {WebTemplateLogoPath}.");
        }

        /// <summary>Fails if the Editor didn't keep a setting, as it would if a licence still forced Unity's logo.</summary>
        private static void Verify(Sprite logo)
        {
            var logos = PlayerSettings.SplashScreen.logos;
            if (!PlayerSettings.SplashScreen.show || PlayerSettings.SplashScreen.showUnityLogo || logos.Length != 1
                || logos[0].logo != logo || PlayerSettings.SplashScreen.backgroundColor != BackgroundColor
                || PlayerSettings.WebGL.template != WebTemplateSetting)
            {
                throw new InvalidOperationException(
                    $"The Editor didn't keep the splash settings (a licence that forces Unity's logo?): {Describe()}");
            }
        }
    }
}
