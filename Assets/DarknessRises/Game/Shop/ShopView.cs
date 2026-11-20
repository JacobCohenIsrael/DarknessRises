using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DarknessRises.Game.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// The Shop screen: the Credits in the bank at the top, a card for each Primary weapon (unlock it, or choose it as the
    /// Primary the next Run carries), and a big PLAY that starts the Run. Buying is instant: the card pops, the Credits
    /// count down and a sound plays. The <see cref="ShopKeeper"/> holds the rules; this shows them, inside the screen's
    /// safe area, with buttons that work by touch and by mouse.
    /// </summary>
    /// <remarks>
    /// Cards are made once, when the Shop opens. Afterwards texts are rewritten only when something is bought or
    /// chosen; the Credits are written with TMP's number formatting as they count, so nothing is allocated per frame.
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

        [SerializeField, Tooltip("Names the Primary weapon the next Run carries.")]
        private TMP_Text primaryLine;

        [SerializeField, Tooltip("Where the Weapons' cards go, side by side.")]
        private RectTransform cards;

        [SerializeField, Tooltip("The card each Weapon's card is copied from. Hidden itself.")]
        private WeaponCardView cardTemplate;

        [SerializeField, Tooltip("Starts a new Run.")]
        private Button playButton;

        [SerializeField] private TMP_Text playLabel;

        [SerializeField, Tooltip("Plays the sound of a purchase.")]
        private AudioSource sound;

        private readonly List<WeaponCardView> weaponCards = new();
        private readonly CountUp creditsCount = new();

        private ShopDefinition definition;
        private ShopScreenSettings settings;
        private ShopKeeper keeper;
        private ShopItem[] weaponItems;
        private Color creditsColor;
        private AudioClip buyClip;
        private ShopItem shownPrimary;
        private Rect fittedSafeArea;
        private Action<WeaponCardView> onCardPressed;
        private Action<int> onCreditsChanged;
        private bool isShown;

        /// <summary>Whether the Shop is showing a keeper's wares.</summary>
        public bool IsShown => isShown;

        /// <summary>
        /// Opens the Shop: lays out <paramref name="shop"/>'s wares as <paramref name="shopKeeper"/> has them, with
        /// Credits in <paramref name="creditsTint"/> and <paramref name="buySound"/> for a purchase.
        /// </summary>
        public void Show(ShopDefinition shop, ShopKeeper shopKeeper, Color creditsTint, AudioClip buySound)
        {
            if (isShown)
            {
                throw new InvalidOperationException("The Shop is already open.");
            }

            definition = shop ? shop : throw new ArgumentNullException(nameof(shop));
            keeper = shopKeeper ?? throw new ArgumentNullException(nameof(shopKeeper));
            settings = definition.Screen;
            creditsColor = creditsTint;
            buyClip = buySound;
            isShown = true;

            title.text = settings.Title;
            playLabel.text = settings.PlayLabel;
            creditsCaption.text = settings.CreditsCaption;
            creditsCaption.color = creditsColor;
            creditsValue.color = creditsColor;

            weaponItems = definition.ToWeaponItems();
            onCardPressed = OnCardPressed;
            cardTemplate.gameObject.SetActive(false);
            for (var i = 0; i < weaponItems.Length; i++)
            {
                var card = Instantiate(cardTemplate, cards);
                card.name = $"{definition.Weapons[i].DisplayName} Card";
                card.Bind(definition.Weapons[i], weaponItems[i]);
                card.Pressed += onCardPressed;
                card.gameObject.SetActive(true);
                weaponCards.Add(card);
            }

            creditsCount.ValueChanged += ShowCredits;
            creditsCount.Set(keeper.Credits);
            ShowCredits(keeper.Credits);
            onCreditsChanged = OnCreditsChanged;
            keeper.CreditsChanged += onCreditsChanged;
            FitSafeArea();
            Refresh();
        }

        /// <summary>Waits until the player presses PLAY.</summary>
        public UniTask PlayAsync(CancellationToken cancellationToken)
        {
            return playButton.OnClickAsync(cancellationToken);
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
        }

        private void OnDestroy()
        {
            if (!isShown)
            {
                return;
            }

            creditsCount.ValueChanged -= ShowCredits;
            keeper.CreditsChanged -= onCreditsChanged;
            for (var i = 0; i < weaponCards.Count; i++)
            {
                if (weaponCards[i] != null)
                {
                    weaponCards[i].Pressed -= onCardPressed;
                }
            }
        }

        // A locked Weapon's button unlocks it, and the new Weapon becomes the Primary; an owned one's equips it.
        private void OnCardPressed(WeaponCardView card)
        {
            var item = card.Item;
            if (!keeper.Owns(item))
            {
                if (!keeper.Buy(item))
                {
                    return;
                }

                keeper.ChoosePrimary(item);
                PlayBuySound();
            }
            else if (!keeper.ChoosePrimary(item))
            {
                return;
            }

            card.Pop(settings.PopScale, settings.PopSeconds);
            Refresh();
        }

        // The bank changed, by a purchase here or from outside: the number counts to it, and what can be afforded may differ.
        private void OnCreditsChanged(int credits)
        {
            creditsCount.CountTo(credits, settings.CreditsCountSeconds);
            Refresh();
        }

        // Everything that depends on the bank or the chosen Primary. Called when something was bought or chosen.
        private void Refresh()
        {
            var primary = keeper.PrimaryOf(weaponItems);
            for (var i = 0; i < weaponCards.Count; i++)
            {
                weaponCards[i].Show(keeper, weaponItems[i] == primary, settings, creditsColor);
            }

            if (primary != shownPrimary)
            {
                shownPrimary = primary;
                primaryLine.text = string.Format(settings.PrimaryFormat, definition.Weapons[Array.IndexOf(weaponItems, primary)].DisplayName);
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
