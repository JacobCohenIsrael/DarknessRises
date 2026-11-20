using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// One Primary weapon's card in the Shop: its name and pitch, and one button that unlocks it, equips it, or says it's
    /// the Primary the next Run carries. The Shop's rules decide which; the card only shows it, and writes its texts
    /// only when what it shows changes.
    /// </summary>
    public sealed class WeaponCardView : MonoBehaviour
    {
        // What the card shows, so it's rewritten only on change.
        private enum Look
        {
            None,
            CanUnlock,
            CannotUnlock,
            Owned,
            Equipped,
        }

        [SerializeField, Tooltip("The outline around the card, lit in the chosen Primary's colour.")]
        private Image frame;

        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Button button;
        [SerializeField] private Image buttonImage;
        [SerializeField] private TMP_Text buttonLabel;

        [SerializeField, Tooltip("Under the button: the Credits still missing, while the bank can't cover the price.")]
        private TMP_Text hint;

        private Look shown;
        private int shownMissing = -1;
        private float popSeconds;
        private float popSecondsLeft;
        private float popScale = 1f;

        /// <summary>Raised when the card's button is pressed.</summary>
        public event Action<WeaponCardView> Pressed;

        public WeaponOffer Offer { get; private set; }

        /// <summary>The Weapon as the Shop's rules see it.</summary>
        public ShopItem Item { get; private set; }

        /// <summary>Makes this the card of <paramref name="offer"/>.</summary>
        public void Bind(WeaponOffer offer, ShopItem item)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));
            Item = item ?? throw new ArgumentNullException(nameof(item));
            title.text = offer.DisplayName;
            description.text = offer.Description;
            shown = Look.None;
            shownMissing = -1;
            button.onClick.AddListener(OnPressed);
        }

        /// <summary>Shows where the Weapon stands: locked, owned, or the Primary the next Run carries.</summary>
        public void Show(ShopKeeper keeper, bool isPrimary, ShopScreenSettings settings, Color creditsColor)
        {
            var look = !keeper.Owns(Item) ? keeper.CanBuy(Item) ? Look.CanUnlock : Look.CannotUnlock
                : isPrimary ? Look.Equipped
                : Look.Owned;
            var missing = look == Look.CannotUnlock ? keeper.NextPrice(Item) - keeper.Credits : 0;
            if (look == shown && missing == shownMissing)
            {
                return;
            }

            shown = look;
            shownMissing = missing;
            button.interactable = look == Look.CanUnlock || look == Look.Owned;
            frame.color = look == Look.Equipped ? settings.EquippedColor : settings.FrameColor;
            hint.gameObject.SetActive(look == Look.CannotUnlock);
            switch (look)
            {
                case Look.CanUnlock:
                    buttonLabel.SetText(settings.UnlockFormat, keeper.NextPrice(Item));
                    buttonLabel.color = creditsColor;
                    buttonImage.color = settings.BuyColor;
                    break;
                case Look.CannotUnlock:
                    buttonLabel.SetText(settings.UnlockFormat, keeper.NextPrice(Item));
                    buttonLabel.color = settings.CannotBuyTextColor;
                    buttonImage.color = settings.CannotBuyColor;
                    hint.SetText(settings.MissingFormat, missing);
                    break;
                case Look.Owned:
                    buttonLabel.text = settings.EquipLabel;
                    buttonLabel.color = Color.white;
                    buttonImage.color = settings.EquipColor;
                    break;
                default:
                    buttonLabel.text = settings.EquippedLabel;
                    buttonLabel.color = settings.EquippedColor;
                    buttonImage.color = settings.CannotBuyColor;
                    break;
            }
        }

        /// <summary>The card jumps to <paramref name="scale"/> times its size and settles back over <paramref name="seconds"/>.</summary>
        public void Pop(float scale, float seconds)
        {
            popScale = scale;
            popSeconds = seconds;
            popSecondsLeft = seconds;
        }

        /// <summary>Advances the pop. The Shop calls it every frame; it does nothing while the card is at rest.</summary>
        public void Tick(float deltaSeconds)
        {
            var rect = (RectTransform)transform;
            if (popSecondsLeft <= 0f && rect.localScale.x == 1f)
            {
                return;
            }

            popSecondsLeft = Mathf.Max(0f, popSecondsLeft - deltaSeconds);
            var scale = Mathf.Lerp(1f, popScale, popSeconds > 0f ? popSecondsLeft / popSeconds : 0f);
            rect.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnPressed);
        }

        private void OnPressed()
        {
            Pressed?.Invoke(this);
        }
    }
}
