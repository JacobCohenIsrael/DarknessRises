using UnityEngine;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// Holds the scene's AudioListener at the character, facing the way the fixed camera looks, so positional
    /// sounds come from where they are on screen and fall off with their distance from the character rather than
    /// from the camera high above. The rotation never follows the character, so turning doesn't swing the sound.
    /// </summary>
    [RequireComponent(typeof(AudioListener))]
    public sealed class EarsView : MonoBehaviour
    {
        [SerializeField, Tooltip("Who hears: the ears follow its position.")]
        private Transform character;

        [SerializeField, Tooltip("Whose heading the ears keep: the fixed-angle camera.")]
        private Transform view;

        [SerializeField, Min(0f), Tooltip("Metres above the character's feet the ears sit.")]
        private float height = 1.6f;

        private void LateUpdate()
        {
            var forward = view.forward;
            forward.y = 0f;
            var rotation = forward.sqrMagnitude > 0f ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
            transform.SetPositionAndRotation(character.position + Vector3.up * height, rotation);
        }
    }
}
