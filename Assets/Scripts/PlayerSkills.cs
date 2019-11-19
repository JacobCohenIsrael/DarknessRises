using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkills : MonoBehaviour
{
    [SerializeField] private Material originalMaterial;
    [SerializeField] private Material glowMaterial;
    Light light;
    float lightMaxIntencity = 0.0f;
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
        Invoke("Reset", 3);
    }

    private void Update()
    {
        light.intensity = Mathf.MoveTowards(light.intensity, lightMaxIntencity, 0.1f);
        Color color = new Color(originalMaterial.color.r, originalMaterial.color.g, originalMaterial.color.b, originalMaterial.color.a) * light.intensity/5;
        myRenderer.material.SetColor("_EmissionColor", color);
    }

    private void Reset()
    {
        lightMaxIntencity = 0.0f;
    }
}
