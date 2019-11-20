using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkills : MonoBehaviour
{
    [SerializeField] private Material originalMaterial;
    [SerializeField] private float glowSkillEffectSpeed = 0.5f;
    [SerializeField] private float slowSkillEffectSpeed = 0.5f;
    [SerializeField] private Light spotLight;

    Light light;
    float lightMaxIntencity = 0.0f;
    private float spotLightRadius = 90f;
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
        lightMaxIntencity = 5;
        spotLightRadius = 170f;
        Invoke("ResetGlow", 5);
    }

    public void Slow()
    {
        timeMaxScale = 0.5f;
        Invoke("ResetSlow", 3);
    }

    public void Push()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 5f, LayerMask.GetMask("Shootable"));
        foreach (Collider hit in colliders)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(2000f, transform.position, 5f, 3.0f);
            }
                
        }
    }

    private void Update()
    {
        light.intensity = Mathf.MoveTowards(light.intensity, lightMaxIntencity, glowSkillEffectSpeed * Time.deltaTime);
        spotLight.spotAngle = Mathf.MoveTowards(spotLight.spotAngle, spotLightRadius, glowSkillEffectSpeed * Time.deltaTime);
        Color color = new Color(originalMaterial.color.r, originalMaterial.color.g, originalMaterial.color.b, originalMaterial.color.a) * light.intensity/5;
        myRenderer.material.SetColor("_EmissionColor", color);
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, timeMaxScale, slowSkillEffectSpeed * Time.deltaTime);
        AudioManager.globalPitch = Time.timeScale;
    }

    private void ResetGlow()
    {
        lightMaxIntencity = 0.0f;
        spotLightRadius = 90f;
    }

    private void ResetSlow()
    {
        timeMaxScale = 1.0f;
    }
}
