using DarknessRises.Game.Health;
using UnityEngine;
using UnityEngine.UI;

namespace DarknessRises.Game.Hud
{
    /// <summary>
    /// Shows the character's hit points as pips at the top left of the HUD: one per hit point, lit while it's left.
    /// Updates only when the hit points change, never every frame.
    /// </summary>
    public sealed class HitPointsHudView : MonoBehaviour
    {
        [SerializeField, Tooltip("The first pip. The others are copies of it, made when the hit points are shown.")]
        private Image pip;

        [SerializeField] private Color fullColor = new(0.85f, 0.2f, 0.18f, 0.9f);

        [SerializeField] private Color emptyColor = new(0.3f, 0.3f, 0.3f, 0.35f);

        private Image[] pips = new Image[0];
        private CharacterHealth health;

        public void Show(CharacterHealth characterHealth)
        {
            Unbind();

            health = characterHealth;
            health.Hurt += Refresh;
            health.Died += Refresh;
            MakePips(health.MaxHitPoints);
            Refresh();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Unbind()
        {
            if (health == null)
            {
                return;
            }

            health.Hurt -= Refresh;
            health.Died -= Refresh;
            health = null;
        }

        // Once per Show: the first pip, plus a copy of it for each further hit point.
        private void MakePips(int count)
        {
            if (pips.Length == count)
            {
                return;
            }

            for (var i = 1; i < pips.Length; i++)
            {
                Destroy(pips[i].gameObject);
            }

            pips = new Image[count];
            pips[0] = pip;
            for (var i = 1; i < count; i++)
            {
                pips[i] = Instantiate(pip, pip.transform.parent);
                pips[i].name = $"{pip.name} {i + 1}";
            }
        }

        private void Refresh()
        {
            for (var i = 0; i < pips.Length; i++)
            {
                pips[i].color = i < health.HitPoints ? fullColor : emptyColor;
            }
        }
    }
}
