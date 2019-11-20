using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class Setup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private void Awake()
    {
        Screen.orientation = ScreenOrientation.Landscape;
        QualitySettings.SetQualityLevel(2);
    }

    private void Start()
    {
        StartCoroutine(FPS());
    }

    private IEnumerator FPS()
    {
        while (true)
        {
            text.text = (1 / Time.unscaledDeltaTime).ToString();
            yield return new WaitForSeconds(0.25f);
        }
    }
}
