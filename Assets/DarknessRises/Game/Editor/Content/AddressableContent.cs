using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace DarknessRises.Game.Editor.Content
{
    /// <summary>
    /// The game's Addressables setup, kept as code: the groups, the profile paths and a catalog that's ready to be
    /// served remotely. Builders mark their content addressable through here and point definitions at it, so nothing
    /// is marked by hand.
    /// </summary>
    /// <remarks>
    /// Groups:
    /// <list type="bullet">
    /// <item><b>Shared</b>: what every Level needs, shipped with the build: the definitions, the character's
    /// Weapons, Grenade and sounds, and Lightning's storm loop.</item>
    /// <item><b>Enemies</b>: each Enemy type's prefab and sounds, packed one asset per bundle so a Level loads only the
    /// Enemy types it uses. Can change after release.</item>
    /// <item><b>Level 1</b> (one group per Level): the Level's scene. Can change after release.</item>
    /// <item><b>Shop</b>: the Shop's scene, loaded between Runs. Can change after release.</item>
    /// <item><b>Sandbox</b>: the Test Room.</item>
    /// </list>
    /// Groups that can change after release build and load through the profile's Remote paths, and the catalog is built
    /// as a remote catalog too. For now the Default profile points the Remote paths at the local ones, so everything
    /// ships inside the build; pointing <c>Remote.LoadPath</c> at a server (and <c>Remote.BuildPath</c> at
    /// <c>ServerData/[BuildTarget]</c>) makes those groups and the catalog update over the air.
    /// </remarks>
    public static class AddressableContent
    {
        public const string SharedGroup = "Shared";
        public const string EnemiesGroup = "Enemies";
        public const string SandboxGroup = "Sandbox";
        private const string ShopGroupName = "Shop";

        private const string DefaultLocalGroupName = "Default Local Group";
        private const string AssetGuidProperty = "m_AssetGUID";
        private const string SubObjectNameProperty = "m_SubObjectName";
        private const string SubObjectTypeProperty = "m_SubObjectType";

        // Set up once per domain, before the first asset is marked.
        private static AddressableAssetSettings setUp;

        /// <summary>The project's Addressables settings, set up as described above. Safe to call again.</summary>
        public static AddressableAssetSettings Setup()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            // Remote paths point at the local ones for now: everything ships inside the build.
            var profiles = settings.profileSettings;
            var profileId = settings.activeProfileId;
            profiles.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, AddressableAssetSettings.kLocalBuildPathValue);
            profiles.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, AddressableAssetSettings.kLocalLoadPathValue);

            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

            var shared = Group(SharedGroup, remote: false, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            Group(EnemiesGroup, remote: true, BundledAssetGroupSchema.BundlePackingMode.PackSeparately);
            Group(SandboxGroup, remote: false, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            settings.DefaultGroup = shared;

            var unused = settings.FindGroup(DefaultLocalGroupName);
            if (unused != null && unused.entries.Count == 0)
            {
                settings.RemoveGroup(unused);
            }

            EditorUtility.SetDirty(settings);
            setUp = settings;
            return settings;
        }

        /// <summary>
        /// Marks what every scene shares as Shared content, so each scene's bundle depends on one copy of it instead of
        /// carrying its own: the definitions under <paramref name="configurationFolder"/> (new ones join by being put
        /// there) and the input actions.
        /// </summary>
        public static void MarkShared(string configurationFolder, params string[] assetPaths)
        {
            Add(AssetDatabase.LoadAssetAtPath<DefaultAsset>(configurationFolder), SharedGroup);
            foreach (var path in assetPaths)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset != null)
                {
                    Add(asset, SharedGroup);
                }
            }
        }

        /// <summary>The group holding one Level's scene, set up for content updates.</summary>
        public static string LevelGroup(string levelName)
        {
            var name = levelName;
            Group(name, remote: true, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            return name;
        }

        /// <summary>The group holding the Shop's scene, set up for content updates like a Level's.</summary>
        public static string ShopGroup()
        {
            Group(ShopGroupName, remote: true, BundledAssetGroupSchema.BundlePackingMode.PackTogether);
            return ShopGroupName;
        }

        /// <summary>Marks <paramref name="asset"/> addressable in <paramref name="groupName"/>, addressed by its file name.</summary>
        public static void Add(Object asset, string groupName)
        {
            var settings = setUp != null ? setUp : Setup();
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
            {
                throw new System.ArgumentException($"{asset} isn't an asset, so it can't be addressable.", nameof(asset));
            }

            var guid = AssetDatabase.AssetPathToGUID(path);
            var entry = settings.CreateOrMoveEntry(guid, settings.FindGroup(groupName), readOnly: false, postEvent: false);
            entry.SetAddress(Path.GetFileNameWithoutExtension(path), postEvent: false);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(entry.parentGroup);
        }

        /// <summary>Takes the asset at <paramref name="assetPath"/> out of Addressables, for content nothing plays any more.</summary>
        public static void Remove(string assetPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings.RemoveAssetEntry(AssetDatabase.AssetPathToGUID(assetPath), postEvent: false))
            {
                EditorUtility.SetDirty(settings);
            }
        }

        /// <summary>Marks <paramref name="asset"/> addressable in <paramref name="groupName"/> and points the reference at it.</summary>
        public static void Reference(SerializedProperty reference, Object asset, string groupName)
        {
            Add(asset, groupName);
            reference.FindPropertyRelative(AssetGuidProperty).stringValue =
                AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset));
            reference.FindPropertyRelative(SubObjectNameProperty).stringValue = string.Empty;
            reference.FindPropertyRelative(SubObjectTypeProperty).stringValue = string.Empty;
        }

        /// <summary>
        /// Points an empty reference at the asset <paramref name="create"/> gives; a set reference only gets its asset
        /// (re)marked addressable, so a choice made in the Inspector survives a rebuild.
        /// </summary>
        public static void ReferenceIfEmpty(SerializedProperty reference, System.Func<Object> create, string groupName)
        {
            var asset = Referenced<Object>(reference);
            if (asset == null)
            {
                asset = create();
            }

            if (asset == null)
            {
                Debug.LogWarning($"[AddressableContent] Nothing to reference at {reference.propertyPath}; left empty.");
                return;
            }

            Reference(reference, asset, groupName);
        }

        /// <summary>The asset a reference points at, or null when it's empty or the asset is gone.</summary>
        public static T Referenced<T>(SerializedProperty reference) where T : Object
        {
            var guid = reference.FindPropertyRelative(AssetGuidProperty).stringValue;
            return string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static AddressableAssetGroup Group(string name, bool remote, BundledAssetGroupSchema.BundlePackingMode packing)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var group = settings.FindGroup(name)
                ?? settings.CreateGroup(name, false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

            var bundled = group.GetSchema<BundledAssetGroupSchema>() ?? group.AddSchema<BundledAssetGroupSchema>();
            bundled.BuildPath.SetVariableByName(settings, remote ? AddressableAssetSettings.kRemoteBuildPath : AddressableAssetSettings.kLocalBuildPath);
            bundled.LoadPath.SetVariableByName(settings, remote ? AddressableAssetSettings.kRemoteLoadPath : AddressableAssetSettings.kLocalLoadPath);
            bundled.BundleMode = packing;
            bundled.IncludeInBuild = true;

            // What ships inside the build can't change after release; the rest can, through content updates.
            var contentUpdate = group.GetSchema<ContentUpdateGroupSchema>() ?? group.AddSchema<ContentUpdateGroupSchema>();
            contentUpdate.StaticContent = !remote;

            EditorUtility.SetDirty(bundled);
            EditorUtility.SetDirty(contentUpdate);
            EditorUtility.SetDirty(group);
            return group;
        }
    }
}
