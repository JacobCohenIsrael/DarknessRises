using System;
using DarknessRises.Game.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// One Upgrade's row in the Shop: its name, its level, what the next level does in plain numbers ("12 > 15") and a
    /// button with the price. The button can't be pressed when the bank can't cover the price, when the Upgrade is maxed,
    /// or while the Weapon it belongs to is still locked. The Shop's rules decide which; the row shows it, and writes its
    /// texts only when what it shows changes.
    /// </summary>
    public sealed class UpgradeRowView : MonoBehaviour
    {
        // What the row shows, so it's rewritten only on change.
        private enum Look
        {
            None,
            CanBuy,
            CannotBuy,
            Maxed,
            Locked,
        }

        [SerializeField] private TMP_Text title;

        [SerializeField, Tooltip("The level the player has, out of how many there are.")]
        private TMP_Text level;

        [SerializeField, Tooltip("The number now and what the next level sets it to.")]
        private TMP_Text effect;

        [SerializeField] private Button button;
        [SerializeField] private Image buttonImage;
        [SerializeField] private TMP_Text buttonLabel;

        private float baseValue;
        private string valueFormat;
        private string stepFormat;
        private Look shown;
        private int shownLevel = -1;
        private float popSeconds;
        private float popSecondsLeft;
        private float popScale = 1f;

        /// <summary>Raised when the row's button is pressed.</summary>
        public event Action<UpgradeRowView> Pressed;

        /// <summary>The Upgrade as the Shop's rules see it.</summary>
        public Upgrade Upgrade { get; private set; }

        /// <summary>
        /// Makes this the row of <paramref name="definition"/>. <paramref name="startValue"/> is the number before any
        /// level is bought: the one authored on the definition it upgrades.
        /// </summary>
        public void Bind(UpgradeDefinition definition, Upgrade upgrade, float startValue, ShopScreenSettings settings)
        {
            Upgrade = upgrade ?? throw new ArgumentNullException(nameof(upgrade));
            baseValue = startValue;
            title.text = definition.DisplayName;

            // Made once: "{0} s" and "{0} s > {1} s", filled in with TMP's number formatting when shown.
            valueFormat = definition.ValueFormat;
            stepFormat = valueFormat + settings.StepSeparator + valueFormat.Replace("{0", "{1");
            shown = Look.None;
            shownLevel = -1;
            button.onClick.AddListener(OnPressed);
        }

        /// <summary>Shows where the Upgrade stands: its level, its next step and whether it can be bought.</summary>
        public void Show(ShopKeeper keeper, ShopScreenSettings settings, Color creditsColor)
        {
            var item = Upgrade.Item;
            var owned = keeper.LevelOf(item);
            var look = keeper.IsMaxed(item) ? Look.Maxed
                : keeper.IsLocked(item) ? Look.Locked
                : keeper.CanBuy(item) ? Look.CanBuy
                : Look.CannotBuy;
            if (look == shown && owned == shownLevel)
            {
                return;
            }

            shown = look;
            shownLevel = owned;
            level.SetText(settings.LevelFormat, (float)owned, (float)item.MaxLevel);
            button.interactable = look == Look.CanBuy;

            var current = Upgrade.ValueAt(owned, baseValue);
            if (look == Look.Maxed)
            {
                effect.SetText(valueFormat, current);
            }
            else
            {
                effect.SetText(stepFormat, current, Upgrade.ValueAt(owned + 1, baseValue));
            }

            var isDim = look == Look.Locked;
            title.color = isDim ? settings.CannotBuyTextColor : Color.white;
            effect.color = isDim ? settings.CannotBuyTextColor : Color.white;
            switch (look)
            {
                case Look.CanBuy:
                    buttonLabel.SetText(settings.PriceFormat, keeper.NextPrice(item));
                    buttonLabel.color = creditsColor;
                    buttonImage.color = settings.BuyColor;
                    break;
                case Look.CannotBuy:
                    buttonLabel.SetText(settings.PriceFormat, keeper.NextPrice(item));
                    buttonLabel.color = settings.CannotBuyTextColor;
                    buttonImage.color = settings.CannotBuyColor;
                    break;
                case Look.Maxed:
                    buttonLabel.text = settings.MaxLabel;
                    buttonLabel.color = settings.OwnedColor;
                    buttonImage.color = settings.CannotBuyColor;
                    break;
                default:
                    buttonLabel.text = settings.LockedLabel;
                    buttonLabel.color = settings.CannotBuyTextColor;
                    buttonImage.color = settings.CannotBuyColor;
                    break;
            }
        }

        /// <summary>The row jumps to <paramref name="scale"/> times its size and settles back over <paramref name="seconds"/>.</summary>
        public void Pop(float scale, float seconds)
        {
            popScale = scale;
            popSeconds = seconds;
            popSecondsLeft = seconds;
        }

        /// <summary>Advances the pop. The Shop calls it every frame; it does nothing while the row is at rest.</summary>
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
