using DarknessRises.Game.Audio;
using UnityEditor;
using UnityEngine;

namespace DarknessRises.Game.Editor.Audio
{
    /// <summary>
    /// Moves the scene's AudioListener off the camera onto "Ears" that follow the character, so positional sounds
    /// are heard from the character's position.
    /// </summary>
    public static class EarsBuilder
    {
        private const float EarHeight = 1.6f;

        public static EarsView BuildEars(Transform character, Camera mainCamera)
        {
            var cameraListener = mainCamera.GetComponent<AudioListener>();
            if (cameraListener != null)
            {
                Object.DestroyImmediate(cameraListener);
            }

            var ears = new GameObject("Ears", typeof(AudioListener), typeof(EarsView));
            ears.transform.position = character.position + Vector3.up * EarHeight;

            var view = ears.GetComponent<EarsView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("character").objectReferenceValue = character;
            serialized.FindProperty("view").objectReferenceValue = mainCamera.transform;
            serialized.FindProperty("height").floatValue = EarHeight;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }
    }
}
