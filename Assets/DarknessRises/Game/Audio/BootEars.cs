using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarknessRises.Game.Audio
{
    /// <summary>
    /// The boot scene's AudioListener, which hears the sounds that carry across scenes (the storm, ADR 0011) while no
    /// other scene is listening. A Level or the Shop brings its own ears ("Ears", at the character or in the Shop) and
    /// those go with its scene, so without these the storm went silent the moment a scene unloaded and stayed silent
    /// until the next one loaded: a dead cut at every Level change and on the way to the Shop.
    /// <para>
    /// Exactly one listener is ever on. A scene that loads with ears of its own takes over the moment it loads (before the
    /// frame's audio update, so the two never overlap), and the boot calls <see cref="TakeFrom"/> before it unloads a
    /// scene, so these hear on through the unload and the next load.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(AudioListener))]
    public sealed class BootEars : MonoBehaviour
    {
        // Reused by every look through a scene, so handing over allocates nothing once warmed up.
        private static readonly List<GameObject> Roots = new();
        private static readonly List<AudioListener> Found = new();

        private AudioListener listener;

        /// <summary>Whether these ears are the ones hearing now.</summary>
        public bool IsListening => Listener.enabled;

        private AudioListener Listener => listener != null ? listener : listener = GetComponent<AudioListener>();

        /// <summary>
        /// Hears in place of <paramref name="scene"/>, which is about to unload: its listeners are switched off and these
        /// on in the same frame, so there's never a frame with no ears or with two.
        /// </summary>
        public void TakeFrom(Scene scene)
        {
            FindEars(scene, switchOff: true);
            Listener.enabled = true;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Raised while the scene activates, in the same frame as its objects wake: its ears take over before any audio
        // is mixed with both on.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene != gameObject.scene && FindEars(scene, switchOff: false))
            {
                Listener.enabled = false;
            }
        }

        // Whether the scene has ears on (an enabled listener on an active object); switches them off if asked.
        private static bool FindEars(Scene scene, bool switchOff)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            var any = false;
            scene.GetRootGameObjects(Roots);
            foreach (var root in Roots)
            {
                root.GetComponentsInChildren(false, Found);
                foreach (var found in Found)
                {
                    if (found.enabled)
                    {
                        any = true;
                        found.enabled = !switchOff;
                    }
                }
            }

            Roots.Clear();
            Found.Clear();
            return any;
        }
    }
}
