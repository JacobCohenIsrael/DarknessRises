using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lightning : MonoBehaviour
{
    private AudioSource audioSource;
    private Animator animator;
    private Light light;

    private float timer = 0.0f;

    private float lightningDelay = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        light = GetComponent<Light>();
        animator.SetBool("isStriking", false);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lightningDelay)
        {
            animator.SetBool("isStriking", true);
            timer = 0.0f;
            lightningDelay = Random.Range(0.8f, 2.5f);
            light.transform.eulerAngles = new Vector3(Random.Range(20f, 30f), Random.Range(60f, 80f), 0);
        }
        else
        {
            animator.SetBool("isStriking", false);
        }
        audioSource.pitch = AudioManager.globalPitch;
    }
}
