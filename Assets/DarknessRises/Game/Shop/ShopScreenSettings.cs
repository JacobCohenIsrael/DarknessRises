using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DarknessRises.Game.Shop
{
    /// <summary>How the Shop screen reads, looks and sounds: its wording, its colours, and the pop and sound of a purchase.</summary>
    [Serializable]
    public sealed class ShopScreenSettings
    {
        [Header("Wording")]
        [SerializeField] private string title = "SHOP";

        [SerializeField, Tooltip("The small word beside the Credits in the bank.")]
        private string creditsCaption = "CREDITS";

        [SerializeField, Tooltip("The big button that starts a new Run.")]
        private string playLabel = "PLAY";

        [SerializeField, Tooltip("The line that names the Primary weapon the next Run carries. {0} is its name.")]
        private string primaryFormat = "NEXT RUN: {0}";

        [SerializeField, Tooltip("The button of a Weapon that isn't owned yet. {0} is its price.")]
        private string unlockFormat = "UNLOCK  {0}";

        [SerializeField, Tooltip("Under that button when the bank can't cover it. {0} is the Credits still missing.")]
        private string missingFormat = "{0} more Credits needed";

        [SerializeField, Tooltip("The button of an owned Weapon that isn't the chosen Primary.")]
        private string equipLabel = "EQUIP";

        [SerializeField, Tooltip("The button of the chosen Primary. It can't be pressed.")]
        private string equippedLabel = "EQUIPPED";

        [Header("Colours")]
        [SerializeField, Tooltip("A button that buys something the bank can cover.")]
        private Color buyColor = new(0.07f, 0.3f, 0.24f, 1f);

        [SerializeField, Tooltip("A button the bank can't cover. It can't be pressed.")]
        private Color cannotBuyColor = new(0.12f, 0.12f, 0.13f, 1f);

        [SerializeField, Tooltip("The text on a button the bank can't cover.")]
        private Color cannotBuyTextColor = new(0.5f, 0.5f, 0.5f, 1f);

        [SerializeField, Tooltip("A button that equips an owned Weapon.")]
        private Color equipColor = new(0.2f, 0.2f, 0.22f, 1f);

        [SerializeField, Tooltip("The chosen Primary: its button's text and the frame around its card.")]
        private Color equippedColor = new(1f, 0.85f, 0.2f, 1f);

        [SerializeField, Tooltip("The frame around every other card.")]
        private Color frameColor = new(0.85f, 0.85f, 0.85f, 0.18f);

        [Header("Feel")]
        [SerializeField, Min(0f), Tooltip("Seconds the Credits in the bank take to count to their new number after a purchase.")]
        private float creditsCountSeconds = 0.35f;

        [SerializeField, Min(1f), Tooltip("How many times its size a card jumps to when something on it is bought or equipped.")]
        private float popScale = 1.08f;

        [SerializeField, Min(0f), Tooltip("Seconds that jump takes to settle back.")]
        private float popSeconds = 0.18f;

        [Header("Sound")]
        [SerializeField, Tooltip("Played when something is bought. Loaded with the Shop and released with it. Empty for no sound.")]
        private AssetReferenceT<AudioClip> buyClip = new(string.Empty);

        [SerializeField, Min(0f), Tooltip("Seconds into the clip where the sound starts, to skip a quiet lead-in.")]
        private float buyClipStartSeconds;

        [SerializeField, Range(0f, 1f)] private float buyVolume = 0.8f;

        public string Title => title;
        public string CreditsCaption => creditsCaption;
        public string PlayLabel => playLabel;
        public string PrimaryFormat => primaryFormat;
        public string UnlockFormat => unlockFormat;
        public string MissingFormat => missingFormat;
        public string EquipLabel => equipLabel;
        public string EquippedLabel => equippedLabel;
        public Color BuyColor => buyColor;
        public Color CannotBuyColor => cannotBuyColor;
        public Color CannotBuyTextColor => cannotBuyTextColor;
        public Color EquipColor => equipColor;
        public Color EquippedColor => equippedColor;
        public Color FrameColor => frameColor;
        public float CreditsCountSeconds => creditsCountSeconds;
        public float PopScale => popScale;
        public float PopSeconds => popSeconds;
        public AssetReferenceT<AudioClip> BuyClip => buyClip;
        public float BuyClipStartSeconds => buyClipStartSeconds;
        public float BuyVolume => buyVolume;
    }
}
