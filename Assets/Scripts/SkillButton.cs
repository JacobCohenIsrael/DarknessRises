using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    [SerializeField] private float skillCooldownSeconds = 3.0f;

    private Button myButton;
    private Image myButtonImage;

    private void Start()
    {
        myButton = GetComponent<Button>();
        myButton.onClick.AddListener(OnClick);
        myButtonImage = GetComponent<Image>();
    }

    private void OnClick()
    {
        myButton.interactable = false;
        myButtonImage.fillAmount = 0.0f;
        StartCoroutine(Cooldown());
    }

    private IEnumerator Cooldown()
    {
        float timer = 0.0f;
        while (timer < skillCooldownSeconds)
        {
            timer += Time.deltaTime;
            myButtonImage.fillAmount = timer / skillCooldownSeconds;
            yield return null;
        }
        myButton.interactable = true;
    }
}
