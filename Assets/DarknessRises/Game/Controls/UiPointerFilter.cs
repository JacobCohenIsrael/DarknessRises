using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DarknessRises.Game.Controls
{
    /// <summary>
    /// Tells whether a screen point is on a UI control the player can press (the pause button, a menu button),
    /// so a click or tap meant for the UI never also counts as a gameplay command.
    /// Only interactable controls count: HUD text and images never block Fire. Reuses its buffers.
    /// </summary>
    public sealed class UiPointerFilter
    {
        private readonly List<RaycastResult> hits = new(8);
        private EventSystem owner;
        private PointerEventData pointer;

        public bool IsOverControl(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (pointer == null || owner != eventSystem)
            {
                owner = eventSystem;
                pointer = new PointerEventData(eventSystem);
            }

            pointer.Reset();
            pointer.position = screenPosition;
            hits.Clear();
            eventSystem.RaycastAll(pointer, hits);

            var isOverControl = false;
            for (var i = 0; i < hits.Count && !isOverControl; i++)
            {
                isOverControl = IsInteractable(hits[i].gameObject);
            }

            hits.Clear();
            return isOverControl;
        }

        private static bool IsInteractable(GameObject hit)
        {
            for (var current = hit != null ? hit.transform : null; current != null; current = current.parent)
            {
                if (current.TryGetComponent<Selectable>(out var selectable))
                {
                    return selectable.IsInteractable();
                }
            }

            return false;
        }
    }
}
