using DarknessRises.Game.Audio;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Editor.Audio;
using DarknessRises.Game.Editor.Content;
using DarknessRises.Game.Editor.Health;
using DarknessRises.Game.Editor.Hud;
using DarknessRises.Game.Shop;
using DarknessRises.Game.Upgrades;
using DarknessRises.Game.Weapons;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DarknessRises.Game.Editor.Shop
{
    /// <summary>
    /// Builds the Shop: its definition asset (the Primary weapons and every Upgrade it sells, with first-pass levels and
    /// prices) and its scene, which the boot loads through Addressables between Runs. The scene is a screen and nothing
    /// else: one overlay canvas laid out for a landscape phone inside the safe area, an event system, a listener, a
    /// sound and the scene's volumes, with the volume panel behind a SOUND button. It has no camera, lights or world, so nothing of a Level is behind it. Everything is built from code; there
    /// is no imported art.
    /// </summary>
    public static class ShopBuilder
    {
        public const string ScenePath = "Assets/DarknessRises/Scenes/Shop.unity";

        private const string ShopDefinitionPath = "Assets/DarknessRises/Configuration/ShopDefinition.asset";

        // An approved Free Pack placeholder, shared with the Clear banner: its coins start after a quiet lead-in.
        private const string BuyClipPath = "Assets/Free Pack/Magic Spell_Coins_2.wav";
        private const float BuyClipStartSeconds = 1.3f;

        // First-pass prices. A new player who dies in the second Room earns about 125 Credits (250 with the end-of-Run ad)
        // and a cleared Level 1 pays about 575 (1150 with the ad). So the cheapest Upgrades (250 to 400) come after one or
        // two Runs, the ad halves that wait, and a Weapon takes a few Runs that clear a Level.
        private const int MachineGunPrice = 2500;
        private const int FlamethrowerPrice = 6000;

        private const string PistolId = "pistol";
        private const string MachineGunId = "machine-gun";
        private const string FlamethrowerId = "flamethrower";

        // How each kind of number reads on a row.
        private const string Whole = "{0}";
        private const string Seconds = "{0} s";
        private const string PreciseSeconds = "{0:2} s";
        private const string PerSecond = "{0}/s";
        private const string Metres = "{0:1} m";

        // Above the Level's HUD order, below the boot's version label.
        private const int SortingOrder = 10;

        private static readonly Color BackgroundColor = new(0.025f, 0.025f, 0.035f, 1f);
        private static readonly Color CardColor = new(0.075f, 0.075f, 0.09f, 1f);
        private static readonly Color RowColor = new(0.055f, 0.055f, 0.068f, 1f);
        private static readonly Color FrameColor = new(0.85f, 0.85f, 0.85f, 0.18f);
        private static readonly Color TextColor = new(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color DimTextColor = new(0.6f, 0.6f, 0.62f, 1f);
        private static readonly Color PlayColor = new(0.8f, 0.12f, 0.1f, 1f);
        private static readonly Color ButtonColor = new(0.2f, 0.2f, 0.22f, 1f);
        private static readonly Color TabColor = new(0.14f, 0.14f, 0.16f, 1f);

        // Until the Credits definition colours them at runtime.
        private static readonly Color CreditsColor = new(0.35f, 1f, 0.78f, 1f);

        // Margins from the safe area's edges, in HUD units (1080 tall).
        private const float SideMargin = 80f;
        private const float TopBarHeight = 150f;
        private const float BottomBarHeight = 160f;

        private const float TitleFontSize = 110f;
        private static readonly Vector2 TitleSize = new(420f, 130f);
        private const float CreditsCaptionFontSize = 34f;
        private const float CreditsValueFontSize = 88f;
        private static readonly Vector2 CreditsSize = new(460f, 100f);
        private const float CreditsCaptionTop = 16f;
        private const float CreditsValueTop = 46f;

        // The two pages' tabs, side by side in the middle of the top bar.
        private static readonly Vector2 TabSize = new(290f, 78f);
        private const float TabGap = 16f;
        private const float TabFontSize = 38f;

        // A page is columns side by side; a column stacks a card or a heading over its rows.
        private const float ColumnSpacing = 36f;
        private const float RowSpacing = 10f;

        private const float CardHeight = 190f;
        private const float CardFrameWidth = 3f;
        private const float CardPadding = 22f;
        private const float CardTitleFontSize = 46f;
        private const float CardTitleTop = 12f;
        private const float CardTitleHeight = 50f;
        private const float CardDescriptionFontSize = 24f;
        private const float CardDescriptionTop = 64f;
        private const float CardDescriptionHeight = 30f;
        private const float CardButtonHeight = 58f;
        private const float CardButtonBottom = 34f;
        private const float CardButtonFontSize = 32f;
        private const float CardHintFontSize = 22f;
        private const float CardHintBottom = 6f;
        private const float CardHintHeight = 26f;

        private const float HeadingHeight = 64f;
        private const float HeadingFontSize = 44f;

        private const float UpgradeRowHeight = 104f;
        private const float UpgradeRowPadding = 16f;
        private const float UpgradeTitleFontSize = 28f;
        private const float UpgradeTitleTop = 10f;
        private const float UpgradeTitleHeight = 36f;
        private const float UpgradeEffectFontSize = 30f;
        private const float UpgradeEffectBottom = 10f;
        private const float UpgradeEffectHeight = 38f;
        private const float UpgradeLevelFontSize = 20f;
        private const float UpgradeLevelTop = 6f;
        private const float UpgradeLevelHeight = 24f;
        private static readonly Vector2 UpgradeButtonSize = new(170f, 58f);
        private const float UpgradeButtonBottom = 10f;
        private const float UpgradeButtonFontSize = 32f;

        private const float HintLineFontSize = 30f;
        // The hint fills the left half of the bottom bar, short of the middle, where debug builds put their DEV button.
        // It's one line on a wide screen and wraps onto two on a narrow one.
        private const float HintLineHeight = 90f;
        private const float HintLineGapToMiddle = 80f;
        private static readonly Vector2 PlayButtonSize = new(460f, 120f);

        // The best Score, one small line under the title, clear of the tabs at 4:3 and of the debug button at the bottom.
        private const float BestScoreTop = 112f;
        private const float BestScoreCaptionFontSize = 22f;
        private const float BestScoreValueFontSize = 26f;
        private static readonly Vector2 BestScoreCaptionSize = new(150f, 34f);
        private static readonly Vector2 BestScoreValueSize = new(200f, 34f);
        private const float PlayFontSize = 84f;

        // The SOUND button that opens the volume panel, at the left of the bottom bar, before the hint line.
        private static readonly Vector2 SoundButtonSize = new(240f, 100f);
        private const float SoundButtonFontSize = 40f;
        private const float SoundButtonGap = 32f;

        /// <summary>
        /// Loads the Shop definition, creating it if it's missing. A definition with no Weapons gets the three Primaries
        /// (the Pistol owned from the start, the Machine gun and the Flamethrower to unlock); every list of Upgrades that
        /// is still empty gets its first-pass levels. Lists that already hold something are left as they were tuned.
        /// </summary>
        public static ShopDefinition LoadOrCreateShop(WeaponDefinition pistol, WeaponDefinition machineGun, WeaponDefinition flamethrower)
        {
            var definition = TestRoomBuilder.LoadOrCreate<ShopDefinition>(ShopDefinitionPath);
            var serialized = new SerializedObject(definition);
            var weapons = serialized.FindProperty("weapons");
            if (weapons.arraySize == 0)
            {
                weapons.arraySize = 3;
                Author(weapons.GetArrayElementAtIndex(0), PistolId, "PISTOL", "Steady and accurate. Yours from the start.", pistol, 0);
                Author(weapons.GetArrayElementAtIndex(1), MachineGunId, "MACHINE GUN", "Fast fire and a deep magazine.", machineGun, MachineGunPrice);
                Author(weapons.GetArrayElementAtIndex(2), FlamethrowerId, "FLAMETHROWER", "A stream of fire that lights the Room.", flamethrower, FlamethrowerPrice);
            }

            AuthorWeaponUpgrades(weapons);
            AuthorReserveUpgrades(weapons);
            AuthorGearUpgrades(serialized);

            var clip = serialized.FindProperty("screen.buyClip");
            if (AddressableContent.Referenced<AudioClip>(clip) == null)
            {
                serialized.FindProperty("screen.buyClipStartSeconds").floatValue = BuyClipStartSeconds;
            }

            AddressableContent.ReferenceIfEmpty(clip, () => GameplaySoundImport.Load(BuyClipPath), AddressableContent.SharedGroup);
            GameplaySoundImport.ApplyToReferenced(clip);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Re-saved so fields added since it was created are written to disk with their defaults.
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void Author(SerializedProperty offer, string id, string displayName, string description, WeaponDefinition weapon, int price)
        {
            offer.FindPropertyRelative("id").stringValue = id;
            offer.FindPropertyRelative("displayName").stringValue = displayName;
            offer.FindPropertyRelative("description").stringValue = description;
            offer.FindPropertyRelative("weapon").objectReferenceValue = weapon;
            offer.FindPropertyRelative("price").intValue = price;
        }

        // Each Weapon's Upgrades, by the Weapon's id. Each level sets the number to a value; the starting value is the
        // Weapon definition's own (Pistol 1 damage, 12 Shots, 1.2 s, 4/s; Machine gun 1, 30, 2.5 s, 10/s; Flamethrower
        // 1, 60, 2.5 s). Damage is a whole number, so a level of it is a big step and priced like one. The Flamethrower
        // has no fire rate Upgrade: its stream is its look.
        private static void AuthorWeaponUpgrades(SerializedProperty weapons)
        {
            for (var i = 0; i < weapons.arraySize; i++)
            {
                var offer = weapons.GetArrayElementAtIndex(i);
                var upgrades = offer.FindPropertyRelative("upgrades");
                if (upgrades.arraySize > 0)
                {
                    continue;
                }

                switch (offer.FindPropertyRelative("id").stringValue)
                {
                    case PistolId:
                        Author(upgrades,
                            ("pistol.damage", UpgradeStat.Damage, "DAMAGE", Whole, new[] { (900, 2f), (2400, 3f) }),
                            ("pistol.magazine", UpgradeStat.MagazineSize, "MAGAZINE", Whole, new[] { (250, 15f), (500, 18f), (900, 22f) }),
                            ("pistol.reload", UpgradeStat.ReloadSeconds, "RELOAD", PreciseSeconds, new[] { (300, 1f), (600, 0.85f), (1000, 0.7f) }),
                            ("pistol.fire-rate", UpgradeStat.ShotsPerSecond, "FIRE RATE", PerSecond, new[] { (350, 5f), (700, 6f), (1200, 7f) }));
                        break;
                    case MachineGunId:
                        Author(upgrades,
                            ("machine-gun.damage", UpgradeStat.Damage, "DAMAGE", Whole, new[] { (2000, 2f), (4500, 3f) }),
                            ("machine-gun.magazine", UpgradeStat.MagazineSize, "MAGAZINE", Whole, new[] { (400, 40f), (800, 50f), (1400, 60f) }),
                            ("machine-gun.reload", UpgradeStat.ReloadSeconds, "RELOAD", PreciseSeconds, new[] { (400, 2.1f), (800, 1.8f), (1300, 1.5f) }),
                            ("machine-gun.fire-rate", UpgradeStat.ShotsPerSecond, "FIRE RATE", PerSecond, new[] { (500, 11f), (1000, 13f), (1600, 15f) }));
                        break;
                    case FlamethrowerId:
                        Author(upgrades,
                            ("flamethrower.damage", UpgradeStat.Damage, "DAMAGE", Whole, new[] { (3000, 2f), (6000, 3f) }),
                            ("flamethrower.tank", UpgradeStat.MagazineSize, "TANK", Whole, new[] { (500, 80f), (1000, 100f), (1700, 120f) }),
                            ("flamethrower.reload", UpgradeStat.ReloadSeconds, "RELOAD", PreciseSeconds, new[] { (500, 2.1f), (900, 1.8f), (1500, 1.5f) }));
                        break;
                }
            }
        }

        // The ammo reserve Upgrade of the two Primaries whose ammo runs out (Machine gun 240 rounds, Flamethrower 300),
        // added after the others if the Weapon's list doesn't have it yet. The top level is a little under twice the
        // reserve a Weapon starts with.
        private static void AuthorReserveUpgrades(SerializedProperty weapons)
        {
            for (var i = 0; i < weapons.arraySize; i++)
            {
                var offer = weapons.GetArrayElementAtIndex(i);
                var upgrades = offer.FindPropertyRelative("upgrades");
                switch (offer.FindPropertyRelative("id").stringValue)
                {
                    case MachineGunId:
                        AddIfMissing(upgrades, ("machine-gun.reserve", UpgradeStat.AmmoReserve, "RESERVE", Whole, new[] { (500, 300f), (1000, 360f), (1700, 450f) }));
                        break;
                    case FlamethrowerId:
                        AddIfMissing(upgrades, ("flamethrower.reserve", UpgradeStat.AmmoReserve, "RESERVE", Whole, new[] { (600, 380f), (1200, 460f), (2000, 560f) }));
                        break;
                }
            }
        }

        // The Grenade (3 charges, 20 s refill, 3.5 m blast), the Skills (Flare and Adrenaline: 4 s, 15 s cooldown; the Lure
        // further down) and the
        // character (5 hit points, Aura 4.5 m). The Aura's levels are small on purpose: 0.3 m each, 20% in all, so
        // Darkness stays dark.
        private static void AuthorGearUpgrades(SerializedObject serialized)
        {
            var grenade = serialized.FindProperty("grenadeUpgrades");
            if (grenade.arraySize == 0)
            {
                Author(grenade,
                    ("grenade.charges", UpgradeStat.GrenadeCharges, "CHARGES", Whole, new[] { (600, 4f), (1500, 5f) }),
                    ("grenade.refill", UpgradeStat.GrenadeRefillSeconds, "REFILL", Seconds, new[] { (300, 17f), (700, 14f), (1200, 11f) }),
                    ("grenade.blast", UpgradeStat.GrenadeBlastRadius, "BLAST RADIUS", Metres, new[] { (400, 4f), (900, 4.5f), (1500, 5f) }));
            }

            var skills = serialized.FindProperty("skillUpgrades");
            if (skills.arraySize == 0)
            {
                Author(skills,
                    ("flare.duration", UpgradeStat.FlareDurationSeconds, "FLARE TIME", Seconds, new[] { (300, 5f), (700, 6f), (1200, 7f) }),
                    ("flare.cooldown", UpgradeStat.FlareCooldownSeconds, "FLARE COOLDOWN", Seconds, new[] { (300, 13f), (700, 11f), (1200, 9f) }),
                    ("adrenaline.duration", UpgradeStat.AdrenalineDurationSeconds, "ADRENALINE TIME", Seconds, new[] { (300, 5f), (700, 6f), (1200, 7f) }),
                    ("adrenaline.cooldown", UpgradeStat.AdrenalineCooldownSeconds, "ADRENALINE COOLDOWN", Seconds, new[] { (300, 13f), (700, 11f), (1200, 9f) }));
            }

            // Slow-mo became Adrenaline (#117): its Upgrades keep their tuning under the new ids and names. Saves bought
            // under the old ids are migrated to the new ones (SaveMigration).
            Rename(skills, "slow-mo.duration", "adrenaline.duration", "ADRENALINE TIME");
            Rename(skills, "slow-mo.cooldown", "adrenaline.cooldown", "ADRENALINE COOLDOWN");

            // The Lure (5 s on the floor, 18 s cooldown), priced like the other Skills.
            AddIfMissing(skills, ("lure.duration", UpgradeStat.LureDurationSeconds, "LURE TIME", Seconds, new[] { (300, 6f), (700, 7f), (1200, 8f) }));
            AddIfMissing(skills, ("lure.cooldown", UpgradeStat.LureCooldownSeconds, "LURE COOLDOWN", Seconds, new[] { (300, 16f), (700, 14f), (1200, 12f) }));

            var character = serialized.FindProperty("characterUpgrades");
            if (character.arraySize == 0)
            {
                Author(character,
                    ("character.hit-points", UpgradeStat.MaxHitPoints, "HIT POINTS", Whole, new[] { (500, 6f), (1200, 7f), (2200, 8f) }),
                    ("character.aura", UpgradeStat.AuraRange, "AURA", Metres, new[] { (400, 4.8f), (900, 5.1f), (1600, 5.4f) }));
            }
        }

        // Gives the Upgrade with id <from> the id <to> and a new display name, keeping its levels as they were tuned.
        private static void Rename(SerializedProperty list, string from, string to, string displayName)
        {
            for (var i = 0; i < list.arraySize; i++)
            {
                var upgrade = list.GetArrayElementAtIndex(i);
                if (upgrade.FindPropertyRelative("id").stringValue == from)
                {
                    upgrade.FindPropertyRelative("id").stringValue = to;
                    upgrade.FindPropertyRelative("displayName").stringValue = displayName;
                }
            }
        }

        // Appends an Upgrade to a list that doesn't hold one with its id yet. The ones already there keep their tuning.
        private static void AddIfMissing(
            SerializedProperty list, (string id, UpgradeStat stat, string name, string format, (int price, float value)[] levels) upgrade)
        {
            for (var i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == upgrade.id)
                {
                    return;
                }
            }

            list.arraySize++;
            Write(list.GetArrayElementAtIndex(list.arraySize - 1), upgrade);
        }

        private static void Author(
            SerializedProperty list, params (string id, UpgradeStat stat, string name, string format, (int price, float value)[] levels)[] upgrades)
        {
            list.arraySize = upgrades.Length;
            for (var i = 0; i < upgrades.Length; i++)
            {
                Write(list.GetArrayElementAtIndex(i), upgrades[i]);
            }
        }

        private static void Write(
            SerializedProperty upgrade, (string id, UpgradeStat stat, string name, string format, (int price, float value)[] levels) authored)
        {
            var (id, stat, name, format, levels) = authored;
            upgrade.FindPropertyRelative("id").stringValue = id;
            upgrade.FindPropertyRelative("stat").intValue = (int)stat;
            upgrade.FindPropertyRelative("displayName").stringValue = name;
            upgrade.FindPropertyRelative("valueFormat").stringValue = format;
            var levelList = upgrade.FindPropertyRelative("levels");
            levelList.arraySize = levels.Length;
            for (var level = 0; level < levels.Length; level++)
            {
                var step = levelList.GetArrayElementAtIndex(level);
                step.FindPropertyRelative("price").intValue = levels[level].price;
                step.FindPropertyRelative("value").floatValue = levels[level].value;
            }
        }

        [MenuItem("Darkness Rises/Rebuild Shop Scene")]
        public static void Build()
        {
            HudBuilder.RequireTextMeshProEssentials();

            // A new single scene unloads unused assets, so load the assets only after creating it.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            TestRoomBuilder.MakeDark();

            // The configuration the Level and the Test Room were built with. Authoring it again here would only move
            // content shared with the Enemies between Addressables groups; it's created only when there is none yet.
            var configuration = AssetDatabase.LoadAssetAtPath<GameConfiguration>(TestRoomBuilder.GameConfigurationPath);
            if (configuration == null || configuration.Shop == null)
            {
                configuration = TestRoomBuilder.LoadOrCreateConfiguration();
            }

            var volumes = AudioVolumesBuilder.Build(configuration);
            var view = BuildScreen(volumes, out var volumePanel);

            // The screen's buttons are clicked through the Input System's UI module, by touch or by mouse.
            new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // The Shop has no character to carry the ears.
            new GameObject("Ears", typeof(AudioListener));

            var root = new GameObject("Shop Root", typeof(ShopRoot)).GetComponent<ShopRoot>();
            var serializedRoot = new SerializedObject(root);
            serializedRoot.FindProperty("configuration").objectReferenceValue = configuration;
            serializedRoot.FindProperty("view").objectReferenceValue = view;
            serializedRoot.FindProperty("volumePanel").objectReferenceValue = volumePanel;
            serializedRoot.FindProperty("audioVolumes").objectReferenceValue = volumes;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();

            AudioRouting.RequireRouted(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            // Loaded additively through Addressables by the boot scene; only the boot scene is in Build Settings.
            AddressableContent.Add(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath), AddressableContent.ShopGroup());
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShopBuilder] Built {ScenePath}.");
        }

        private static ShopView BuildScreen(SceneAudioVolumes volumes, out VolumePanelView volumePanel)
        {
            var canvasObject = new GameObject("Shop", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ShopView));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudBuilder.ReferenceResolution;
            scaler.matchWidthOrHeight = HudBuilder.MatchHeight;

            // Behind the whole screen, the notch and the rounded corners included.
            var background = HealthBuilder.AddPanel(canvasObject.transform, "Background", BackgroundColor);
            background.GetComponent<Image>().raycastTarget = false;

            var safeArea = AddRect("Safe Area", canvasObject.transform);
            Stretch(safeArea, 0f, 0f, 0f, 0f);

            // The top bar: the title on the left, the pages' tabs in the middle, the Credits in the bank on the right.
            var title = AddText(safeArea, "Title", "SHOP", TitleFontSize, HealthBuilder.TitleColor, TextAlignmentOptions.Left);
            Corner(title.rectTransform, new Vector2(0f, 1f), new Vector2(SideMargin, -(TopBarHeight - TitleSize.y) / 2f), TitleSize);
            title.fontStyle = FontStyles.Bold;

            var tabY = -(TopBarHeight - TabSize.y) / 2f;
            var weaponsTab = AddButton(safeArea, "Weapons Tab", "WEAPONS", TabFontSize, TabColor, Color.white, out var weaponsTabLabel);
            Corner((RectTransform)weaponsTab.transform, new Vector2(0.5f, 1f), new Vector2(-(TabSize.x + TabGap) / 2f, tabY), TabSize);
            var gearTab = AddButton(safeArea, "Gear Tab", "GEAR", TabFontSize, TabColor, Color.white, out var gearTabLabel);
            Corner((RectTransform)gearTab.transform, new Vector2(0.5f, 1f), new Vector2((TabSize.x + TabGap) / 2f, tabY), TabSize);
            weaponsTabLabel.fontStyle = gearTabLabel.fontStyle = FontStyles.Bold;

            var creditsCaption = AddText(safeArea, "Credits Caption", "CREDITS", CreditsCaptionFontSize, CreditsColor, TextAlignmentOptions.Right);
            Corner(creditsCaption.rectTransform, new Vector2(1f, 1f), new Vector2(-SideMargin, -CreditsCaptionTop), new Vector2(CreditsSize.x, CreditsCaptionFontSize * 1.3f));
            var creditsValue = AddText(safeArea, "Credits Value", "0", CreditsValueFontSize, CreditsColor, TextAlignmentOptions.Right);
            Corner(creditsValue.rectTransform, new Vector2(1f, 1f), new Vector2(-SideMargin, -CreditsValueTop), CreditsSize);
            creditsValue.fontStyle = FontStyles.Bold;

            // The pages, between the bars. Only one shows at a time; each is columns side by side, sharing the width.
            var weaponsPage = AddPage(safeArea, "Weapons Page");
            var gearPage = AddPage(safeArea, "Gear Page");

            // What the Shop copies into the pages when it opens. Hidden, and never shown themselves.
            var templates = AddRect("Templates", safeArea);
            var columnTemplate = BuildColumnTemplate(templates);
            var cardTemplate = BuildCardTemplate(templates);
            var headingTemplate = BuildHeadingTemplate(templates);
            var rowTemplate = BuildRowTemplate(templates);

            // The bottom bar: SOUND and what owning a Weapon means on the left, the big PLAY on the right.
            var soundButton = AddButton(safeArea, "Sound Button", "SOUND", SoundButtonFontSize, ButtonColor, Color.white, out var soundLabel);
            Corner((RectTransform)soundButton.transform, Vector2.zero, new Vector2(SideMargin, (BottomBarHeight - SoundButtonSize.y) / 2f), SoundButtonSize);
            soundLabel.fontStyle = FontStyles.Bold;

            var hintLine = AddText(safeArea, "Hint Line", "Every unlocked Weapon comes along on a Run.", HintLineFontSize, DimTextColor, TextAlignmentOptions.Left);
            var hintRect = hintLine.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0f, 0f);
            hintRect.offsetMin = new Vector2(SideMargin + SoundButtonSize.x + SoundButtonGap, (BottomBarHeight - HintLineHeight) / 2f);
            hintRect.offsetMax = new Vector2(-HintLineGapToMiddle, (BottomBarHeight + HintLineHeight) / 2f);
            hintLine.textWrappingMode = TextWrappingModes.Normal;

            var playButton = AddButton(safeArea, "Play Button", "PLAY", PlayFontSize, PlayColor, Color.white, out var playLabel);
            Corner((RectTransform)playButton.transform, new Vector2(1f, 0f), new Vector2(-SideMargin, (BottomBarHeight - PlayButtonSize.y) / 2f), PlayButtonSize);
            playLabel.fontStyle = FontStyles.Bold;

            // The best Score, small, under the title: it buys nothing, so it stays out of the Credits' way.
            var bestCaption = AddText(safeArea, "Best Score Caption", "BEST SCORE", BestScoreCaptionFontSize, DimTextColor, TextAlignmentOptions.Left);
            Corner(bestCaption.rectTransform, new Vector2(0f, 1f), new Vector2(SideMargin + 6f, -BestScoreTop), BestScoreCaptionSize);
            var bestValue = AddText(safeArea, "Best Score Value", "0", BestScoreValueFontSize, TextColor, TextAlignmentOptions.Left);
            Corner(bestValue.rectTransform, new Vector2(0f, 1f), new Vector2(SideMargin + 6f + BestScoreCaptionSize.x, -BestScoreTop), BestScoreValueSize);
            bestValue.fontStyle = FontStyles.Bold;

            var sound = new GameObject("Sound", typeof(AudioSource)).GetComponent<AudioSource>();
            sound.transform.SetParent(canvasObject.transform, false);
            sound.playOnAwake = false;
            sound.spatialBlend = 0f;
            AudioRouting.Route(sound, AudioCategory.Sound);

            // Over everything else on the screen.
            volumePanel = VolumePanelBuilder.Build(canvasObject.transform, soundButton, soundLabel, volumes);

            var view = canvasObject.GetComponent<ShopView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("safeArea").objectReferenceValue = safeArea;
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("creditsCaption").objectReferenceValue = creditsCaption;
            serialized.FindProperty("creditsValue").objectReferenceValue = creditsValue;
            serialized.FindProperty("hintLine").objectReferenceValue = hintLine;
            serialized.FindProperty("bestScoreCaption").objectReferenceValue = bestCaption;
            serialized.FindProperty("bestScoreValue").objectReferenceValue = bestValue;
            serialized.FindProperty("weaponsPage").objectReferenceValue = weaponsPage;
            serialized.FindProperty("gearPage").objectReferenceValue = gearPage;
            serialized.FindProperty("weaponsTab").objectReferenceValue = weaponsTab;
            serialized.FindProperty("weaponsTabLabel").objectReferenceValue = weaponsTabLabel;
            serialized.FindProperty("gearTab").objectReferenceValue = gearTab;
            serialized.FindProperty("gearTabLabel").objectReferenceValue = gearTabLabel;
            serialized.FindProperty("columnTemplate").objectReferenceValue = columnTemplate;
            serialized.FindProperty("cardTemplate").objectReferenceValue = cardTemplate;
            serialized.FindProperty("headingTemplate").objectReferenceValue = headingTemplate;
            serialized.FindProperty("rowTemplate").objectReferenceValue = rowTemplate;
            serialized.FindProperty("playButton").objectReferenceValue = playButton;
            serialized.FindProperty("playLabel").objectReferenceValue = playLabel;
            serialized.FindProperty("sound").objectReferenceValue = sound;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static RectTransform AddPage(RectTransform safeArea, string name)
        {
            var page = AddRect(name, safeArea);
            Stretch(page, SideMargin, SideMargin, TopBarHeight, BottomBarHeight);
            var columns = page.gameObject.AddComponent<HorizontalLayoutGroup>();
            columns.spacing = ColumnSpacing;
            columns.childAlignment = TextAnchor.UpperCenter;
            columns.childControlWidth = columns.childControlHeight = true;
            columns.childForceExpandWidth = columns.childForceExpandHeight = true;
            return page;
        }

        // A column: what's put in it stacks from the top, each as wide as the column and as tall as it asks to be.
        private static RectTransform BuildColumnTemplate(RectTransform templates)
        {
            var column = AddRect("Column", templates);
            var stack = column.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = RowSpacing;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            // Every column gets the same share of the page, whatever is written in it.
            var share = column.gameObject.AddComponent<LayoutElement>();
            share.minWidth = 0f;
            share.preferredWidth = 0f;
            share.flexibleWidth = 1f;
            column.gameObject.SetActive(false);
            return column;
        }

        // One Weapon's card: a framed panel with its name, its pitch, one button and a hint under it.
        private static WeaponCardView BuildCardTemplate(RectTransform templates)
        {
            var card = AddRect("Weapon Card", templates);
            var frame = card.gameObject.AddComponent<Image>();
            frame.color = FrameColor;
            frame.raycastTarget = false;
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = CardHeight;

            var fill = AddRect("Fill", card);
            Stretch(fill, CardFrameWidth, CardFrameWidth, CardFrameWidth, CardFrameWidth);
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = CardColor;
            fillImage.raycastTarget = false;

            var title = AddText(card, "Title", "WEAPON", CardTitleFontSize, Color.white, TextAlignmentOptions.Left);
            TopStrip(title.rectTransform, CardPadding, CardPadding, CardTitleTop, CardTitleHeight);
            title.fontStyle = FontStyles.Bold;
            FitOnOneLine(title, CardTitleFontSize);

            var description = AddText(card, "Description", "What it's like to fire.", CardDescriptionFontSize, DimTextColor, TextAlignmentOptions.Left);
            TopStrip(description.rectTransform, CardPadding, CardPadding, CardDescriptionTop, CardDescriptionHeight);
            FitOnOneLine(description, CardDescriptionFontSize);

            var button = AddButton(card, "Button", "UNLOCK", CardButtonFontSize, ButtonColor, Color.white, out var buttonLabel);
            BottomStrip((RectTransform)button.transform, CardPadding, CardPadding, CardButtonBottom, CardButtonHeight);
            buttonLabel.fontStyle = FontStyles.Bold;

            var hint = AddText(card, "Hint", string.Empty, CardHintFontSize, DimTextColor, TextAlignmentOptions.Center);
            BottomStrip(hint.rectTransform, CardPadding, CardPadding, CardHintBottom, CardHintHeight);

            var view = card.gameObject.AddComponent<WeaponCardView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("frame").objectReferenceValue = frame;
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("description").objectReferenceValue = description;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("buttonImage").objectReferenceValue = button.targetGraphic;
            serialized.FindProperty("buttonLabel").objectReferenceValue = buttonLabel;
            serialized.FindProperty("hint").objectReferenceValue = hint;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            card.gameObject.SetActive(false);
            return view;
        }

        // The heading over a Gear column: as tall as it needs, so the rows under it line up across columns.
        private static TextMeshProUGUI BuildHeadingTemplate(RectTransform templates)
        {
            var heading = AddText(templates, "Heading", "GEAR", HeadingFontSize, TextColor, TextAlignmentOptions.Left);
            heading.fontStyle = FontStyles.Bold;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = HeadingHeight;
            heading.gameObject.SetActive(false);
            return heading;
        }

        // One Upgrade's row: its name over its numbers on the left ("12 > 15"), and its level over the buy button with the
        // price on the right.
        private static UpgradeRowView BuildRowTemplate(RectTransform templates)
        {
            var row = AddRect("Upgrade Row", templates);
            var panel = row.gameObject.AddComponent<Image>();
            panel.color = RowColor;
            panel.raycastTarget = false;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = UpgradeRowHeight;

            // Everything left of the button.
            var textRight = UpgradeRowPadding + UpgradeButtonSize.x + UpgradeRowPadding;

            var title = AddText(row, "Title", "UPGRADE", UpgradeTitleFontSize, Color.white, TextAlignmentOptions.Left);
            TopStrip(title.rectTransform, UpgradeRowPadding, textRight, UpgradeTitleTop, UpgradeTitleHeight);
            title.fontStyle = FontStyles.Bold;
            FitOnOneLine(title, UpgradeTitleFontSize);

            var effect = AddText(row, "Effect", "0 > 0", UpgradeEffectFontSize, Color.white, TextAlignmentOptions.Left);
            BottomStrip(effect.rectTransform, UpgradeRowPadding, textRight, UpgradeEffectBottom, UpgradeEffectHeight);
            FitOnOneLine(effect, UpgradeEffectFontSize);

            // On the right: the level over the buy button, so the numbers on the left keep the whole line on a narrow screen.
            var level = AddText(row, "Level", "LV 0/3", UpgradeLevelFontSize, DimTextColor, TextAlignmentOptions.Center);
            Corner(level.rectTransform, Vector2.one, new Vector2(-UpgradeRowPadding, -UpgradeLevelTop), new Vector2(UpgradeButtonSize.x, UpgradeLevelHeight));

            var button = AddButton(row, "Button", "0", UpgradeButtonFontSize, ButtonColor, Color.white, out var buttonLabel);
            Corner((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(-UpgradeRowPadding, UpgradeButtonBottom), UpgradeButtonSize);
            buttonLabel.fontStyle = FontStyles.Bold;

            var view = row.gameObject.AddComponent<UpgradeRowView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("level").objectReferenceValue = level;
            serialized.FindProperty("effect").objectReferenceValue = effect;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("buttonImage").objectReferenceValue = button.targetGraphic;
            serialized.FindProperty("buttonLabel").objectReferenceValue = buttonLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            row.gameObject.SetActive(false);
            return view;
        }

        private static RectTransform AddRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        // Fills its parent, inset by the given margins.
        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        // Sits in a corner or on an edge of its parent: anchored and pivoted there, offset from it.
        private static void Corner(RectTransform rect, Vector2 corner, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
        }

        // Spans its parent's width, inset at the sides, a fixed height down from the top.
        private static void TopStrip(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        // Spans its parent's width, inset at the sides, a fixed height up from the bottom.
        private static void BottomStrip(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        // Stays on one line, shrinking where a narrow screen leaves it less room.
        private static void FitOnOneLine(TextMeshProUGUI label, float fontSize)
        {
            label.enableAutoSizing = true;
            label.fontSizeMin = fontSize * 0.55f;
            label.fontSizeMax = fontSize;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private static TextMeshProUGUI AddText(
            RectTransform parent, string name, string text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.rectTransform.SetParent(parent, false);
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static Button AddButton(
            RectTransform parent, string name, string text, float fontSize, Color color, Color textColor, out TextMeshProUGUI label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = color;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            // The view sets each state's colours itself, so a disabled button isn't also tinted by the button.
            var colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;

            label = AddText(rect, "Label", text, fontSize, textColor, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 0f, 0f, 0f, 0f);
            return button;
        }
    }
}
