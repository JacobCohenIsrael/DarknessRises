using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Configuration;
using DarknessRises.Game.Hud;
using DarknessRises.Game.Upgrades;
using DarknessRises.Game.Weapons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// The Shop screen: the Credits in the bank at the top, two pages of wares, and a big PLAY that starts the Run.
    /// The Weapons page has a column for each Primary weapon: its card (unlock it; an owned Weapon comes along on every
    /// Run) over its Upgrades. The Gear page has the Upgrades of the Grenade, the Skills and the character.
    /// Buying is instant: the card or row pops, the Credits count down and a sound plays. The
    /// <see cref="ShopKeeper"/> holds the rules; this shows them, inside the screen's safe area, with buttons that work
    /// by touch and by mouse.
    /// </summary>
    /// <remarks>
    /// Cards and rows are made once, when the Shop opens. Afterwards texts are rewritten only when something is bought
    /// or chosen; numbers are written with TMP's number formatting, so nothing is allocated per frame.
    /// </remarks>
    public sealed class ShopView : MonoBehaviour
    {
        private const string NumberFormat = "{0}";

        [SerializeField, Tooltip("Fitted to the screen's safe area; everything on the screen is inside it.")]
        private RectTransform safeArea;

        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text creditsCaption;

        [SerializeField, Tooltip("The Credits in the bank.")]
        private TMP_Text creditsValue;

        [SerializeField, Tooltip("Says what owning a Weapon means: it comes along on every Run.")]
        private TMP_Text hintLine;

        [Header("Pages")]
        [SerializeField, Tooltip("The page of the Primary weapons: a column for each goes here, side by side.")]
        private RectTransform weaponsPage;

        [SerializeField, Tooltip("The page of the Grenade's, the Skills' and the character's Upgrades, a column each.")]
        private RectTransform gearPage;

        [SerializeField] private Button weaponsTab;
        [SerializeField] private TMP_Text weaponsTabLabel;
        [SerializeField] private Button gearTab;
        [SerializeField] private TMP_Text gearTabLabel;

        [Header("Templates (hidden; copied when the Shop opens)")]
        [SerializeField, Tooltip("A column of a page: what's put in it stacks from the top.")]
        private RectTransform columnTemplate;

        [SerializeField, Tooltip("A Primary weapon's card, at the top of its column.")]
        private WeaponCardView cardTemplate;

        [SerializeField, Tooltip("The heading of a Gear column.")]
        private TMP_Text headingTemplate;

        [SerializeField, Tooltip("One Upgrade's row.")]
        private UpgradeRowView rowTemplate;

        [Header("Play")]
        [SerializeField, Tooltip("Starts a new Run.")]
        private Button playButton;

        [SerializeField] private TMP_Text playLabel;

        [SerializeField, Tooltip("Plays the sound of a purchase.")]
        private AudioSource sound;

        private readonly List<WeaponCardView> weaponCards = new();
        private readonly List<UpgradeRowView> rows = new();
        private readonly CountUp creditsCount = new();

        private GameConfiguration configuration;
        private ShopScreenSettings settings;
        private ShopCatalog catalog;
        private ShopKeeper keeper;
        private Color creditsColor;
        private AudioClip buyClip;
        private Rect fittedSafeArea;
        private Action<WeaponCardView> onCardPressed;
        private Action<UpgradeRowView> onRowPressed;
        private Action onSaveChanged;
        private bool isShown;

        /// <summary>Whether the Shop is showing a keeper's wares.</summary>
        public bool IsShown => isShown;

        /// <summary>
        /// Opens the Shop: lays out <paramref name="shopCatalog"/> as <paramref name="shopKeeper"/> has it. Each
        /// Upgrade's starting value is read from <paramref name="gameConfiguration"/>'s definitions;
        /// <paramref name="buySound"/> plays for a purchase.
        /// </summary>
        public void Show(GameConfiguration gameConfiguration, ShopCatalog shopCatalog, ShopKeeper shopKeeper, AudioClip buySound)
        {
            if (isShown)
            {
                throw new InvalidOperationException("The Shop is already open.");
            }

            configuration = gameConfiguration ? gameConfiguration : throw new ArgumentNullException(nameof(gameConfiguration));
            catalog = shopCatalog ?? throw new ArgumentNullException(nameof(shopCatalog));
            keeper = shopKeeper ?? throw new ArgumentNullException(nameof(shopKeeper));
            settings = configuration.Shop.Screen;
            creditsColor = configuration.Credits.Color;
            buyClip = buySound;
            isShown = true;

            title.text = settings.Title;
            playLabel.text = settings.PlayLabel;
            creditsCaption.text = settings.CreditsCaption;
            creditsCaption.color = creditsColor;
            creditsValue.color = creditsColor;
            hintLine.text = settings.CarryHint;
            weaponsTabLabel.text = settings.WeaponsTab;
            gearTabLabel.text = settings.GearTab;

            onCardPressed = OnCardPressed;
            onRowPressed = OnRowPressed;
            BuildWeaponsPage();
            BuildGearPage();
            weaponsTab.onClick.AddListener(ShowWeaponsPage);
            gearTab.onClick.AddListener(ShowGearPage);
            ShowWeaponsPage();

            creditsCount.ValueChanged += ShowCredits;
            creditsCount.Set(keeper.Credits);
            ShowCredits(keeper.Credits);
            onSaveChanged = OnSaveChanged;
            keeper.Changed += onSaveChanged;
            FitSafeArea();
            Refresh();
        }

        /// <summary>Waits until the player presses PLAY.</summary>
        public UniTask PlayAsync(CancellationToken cancellationToken)
        {
            return playButton.OnClickAsync(cancellationToken);
        }

        // A column per Primary weapon: its card, then its Upgrades.
        private void BuildWeaponsPage()
        {
            for (var i = 0; i < catalog.Weapons.Count; i++)
            {
                var weapon = catalog.Weapons[i];
                var column = AddColumn(weaponsPage, weapon.Offer.DisplayName);
                var card = Instantiate(cardTemplate, column);
                card.name = $"{weapon.Offer.DisplayName} Card";
                card.Bind(weapon.Offer, weapon.Item);
                card.Pressed += onCardPressed;
                card.gameObject.SetActive(true);
                weaponCards.Add(card);
                AddRows(column, weapon.Upgrades, weapon.Offer.Weapon);
            }
        }

        // A column each for the Grenade, the Skills and the character: a heading, then its Upgrades.
        private void BuildGearPage()
        {
            AddGearColumn(settings.GrenadeTitle, catalog.Grenade);
            AddGearColumn(settings.SkillsTitle, catalog.Skills);
            AddGearColumn(settings.CharacterTitle, catalog.Character);
        }

        private void AddGearColumn(string heading, IReadOnlyList<ShopCatalog.UpgradeEntry> upgrades)
        {
            var column = AddColumn(gearPage, heading);
            var label = Instantiate(headingTemplate, column);
            label.name = "Heading";
            label.text = heading;
            label.gameObject.SetActive(true);
            AddRows(column, upgrades, null);
        }

        private RectTransform AddColumn(RectTransform page, string columnName)
        {
            var column = Instantiate(columnTemplate, page);
            column.name = $"{columnName} Column";
            column.gameObject.SetActive(true);
            return column;
        }

        private void AddRows(RectTransform column, IReadOnlyList<ShopCatalog.UpgradeEntry> upgrades, WeaponDefinition weapon)
        {
            for (var i = 0; i < upgrades.Count; i++)
            {
                var entry = upgrades[i];
                var row = Instantiate(rowTemplate, column);
                row.name = $"{entry.Definition.DisplayName} Row";
                row.Bind(
                    entry.Definition, entry.Upgrade, ConfiguredRunStats.BaseValue(entry.Upgrade.Stat, configuration, weapon), settings);
                row.Pressed += onRowPressed;
                row.gameObject.SetActive(true);
                rows.Add(row);
            }
        }

        private void ShowWeaponsPage() => ShowPage(true);

        private void ShowGearPage() => ShowPage(false);

        private void ShowPage(bool showsWeapons)
        {
            weaponsPage.gameObject.SetActive(showsWeapons);
            gearPage.gameObject.SetActive(!showsWeapons);
            weaponsTab.targetGraphic.color = showsWeapons ? settings.ActiveTabColor : settings.InactiveTabColor;
            gearTab.targetGraphic.color = showsWeapons ? settings.InactiveTabColor : settings.ActiveTabColor;
        }

        private void Update()
        {
            if (!isShown)
            {
                return;
            }

            // A rotation or a resized browser window can move the safe area.
            FitSafeArea();

            var deltaSeconds = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
            creditsCount.Tick(deltaSeconds);
            for (var i = 0; i < weaponCards.Count; i++)
            {
                weaponCards[i].Tick(deltaSeconds);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Tick(deltaSeconds);
            }
        }

        private void OnDestroy()
        {
            if (!isShown)
            {
                return;
            }

            creditsCount.ValueChanged -= ShowCredits;
            keeper.Changed -= onSaveChanged;
            weaponsTab.onClick.RemoveListener(ShowWeaponsPage);
            gearTab.onClick.RemoveListener(ShowGearPage);
            for (var i = 0; i < weaponCards.Count; i++)
            {
                if (weaponCards[i] != null)
                {
                    weaponCards[i].Pressed -= onCardPressed;
                }
            }

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    rows[i].Pressed -= onRowPressed;
                }
            }
        }

        // A locked Weapon's button unlocks it. From then on it comes along on every Run.
        private void OnCardPressed(WeaponCardView card)
        {
            if (!keeper.Buy(card.Item))
            {
                return;
            }

            PlayBuySound();
            card.Pop(settings.PopScale, settings.PopSeconds);
            Refresh();
        }

        // An Upgrade's button buys its next level.
        private void OnRowPressed(UpgradeRowView row)
        {
            if (!keeper.Buy(row.Upgrade.Item))
            {
                return;
            }

            PlayBuySound();
            row.Pop(settings.PopScale, settings.PopSeconds);
            Refresh();
        }

        // The save changed, by a purchase here or from outside (the dev tools): the Credits count to their new number,
        // and what is owned or can be afforded may differ.
        private void OnSaveChanged()
        {
            if (creditsCount.Target != keeper.Credits)
            {
                creditsCount.CountTo(keeper.Credits, settings.CreditsCountSeconds);
            }

            Refresh();
        }

        // Everything that depends on the bank or on what is owned. Called when one of them changed.
        private void Refresh()
        {
            for (var i = 0; i < weaponCards.Count; i++)
            {
                weaponCards[i].Show(keeper, settings, creditsColor);
            }

            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].Show(keeper, settings, creditsColor);
            }
        }

        private void PlayBuySound()
        {
            if (buyClip == null)
            {
                return;
            }

            sound.clip = buyClip;
            sound.volume = settings.BuyVolume;
            sound.time = Mathf.Clamp(settings.BuyClipStartSeconds, 0f, Mathf.Max(0f, buyClip.length - 0.01f));
            sound.Play();
        }

        private void ShowCredits(int credits)
        {
            creditsValue.SetText(NumberFormat, (float)credits);
        }

        private void FitSafeArea()
        {
            var area = Screen.safeArea;
            if (area == fittedSafeArea || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            fittedSafeArea = area;
            safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        }
    }
}
