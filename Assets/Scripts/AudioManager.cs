using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioMixer audioMixer;
    public static float globalPitch = 1.0f;

    private void Update()
    {
        audioMixer.SetFloat("GlobalPitch", globalPitch);
    }
}
