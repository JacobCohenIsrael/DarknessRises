using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarknessRises.Game.Shop
{
    /// <summary>
    /// Reports a tap on a button that can't be pressed now (a ware the bank can't cover), which the button itself
    /// ignores, so the Shop can answer it with a "no". Added beside the button when its card or row is bound.
    /// </summary>
    public sealed class RefusedPress : MonoBehaviour, IPointerClickHandler
    {
        private Button button;

        /// <summary>Raised when the button is tapped while it can't be pressed.</summary>
        public event Action Refused;

        /// <summary>The refused presses of <paramref name="target"/>: the one beside it, added if there's none yet.</summary>
        public static RefusedPress On(Button target)
        {
            if (!target.TryGetComponent<RefusedPress>(out var refused))
            {
                refused = target.gameObject.AddComponent<RefusedPress>();
            }

            refused.button = target;
            return refused;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null && !button.interactable && eventData.button == PointerEventData.InputButton.Left)
            {
                Refused?.Invoke();
            }
        }
    }
}
