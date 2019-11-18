using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static int score;

    [SerializeField] TextMeshProUGUI text;


    void Awake()
    {
        score = 0;
    }


    void Update()
    {
        text.text = "Score: " + score;
    }
}
