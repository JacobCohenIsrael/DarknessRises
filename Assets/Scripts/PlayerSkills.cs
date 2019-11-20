using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkills : MonoBehaviour
{
    [SerializeField] private Material originalMaterial;
    [SerializeField] private float lightSkillEffectSpeed = 0.5f;
    [SerializeField] private float slowSkillEffectSpeed = 0.5f;

    Light light;
    float lightMaxIntencity = 0.0f;
    float timeMaxScale = 1.0f;
    private Renderer myRenderer;

    private void Start()
    {
        light = GetComponent<Light>();
        myRenderer = GetComponent<Renderer>();
    }

    public void Glow()
    {
        
        light.enabled = true;
        lightMaxIntencity = 10f;
        Invoke("ResetGlow", 3);
    }

    public void Slow()
    {
        timeMaxScale = 0.5f;
        Invoke("ResetSlow", 3);
    }

    private void Update()
    {
        light.intensity = Mathf.MoveTowards(light.intensity, lightMaxIntencity, lightSkillEffectSpeed * Time.deltaTime);
        Color color = new Color(originalMaterial.color.r, originalMaterial.color.g, originalMaterial.color.b, originalMaterial.color.a) * light.intensity/5;
        myRenderer.material.SetColor("_EmissionColor", color);
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, timeMaxScale, slowSkillEffectSpeed * Time.deltaTime);
        AudioManager.globalPitch = Time.timeScale;
    }

    private void ResetGlow()
    {
        lightMaxIntencity = 0.0f;
    }

    private void ResetSlow()
    {
        timeMaxScale = 1.0f;
    }
}
