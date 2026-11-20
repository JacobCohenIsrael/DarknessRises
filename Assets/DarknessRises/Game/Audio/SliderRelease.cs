using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarknessRises.Game.Audio
{
    /// <summary>Reports when the finger or mouse lets go of the slider it sits on, after a drag or a tap.</summary>
    public sealed class SliderRelease : MonoBehaviour, IPointerUpHandler
    {
        public event Action Released;

        public void OnPointerUp(PointerEventData eventData) => Released?.Invoke();
    }
}
