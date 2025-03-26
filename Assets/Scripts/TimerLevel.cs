using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TimerLevel : MonoBehaviour
{
    [SerializeField] private float startTimer;
    private float currentTime;

    [SerializeField] TMP_Text timerText;

    // Start is called before the first frame update
    void Start()
    {
        currentTime = startTimer;
    }

    // Update is called once per frame
    void Update()
    {
        if(currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if(currentTime > 60)
            {
                FormatTimer();
            }
            else
            {
                timerText.text = currentTime.ToString("0:00");

            }
        }
        else
        {

        }
    }

    void FormatTimer()
    {
        float mins = Mathf.FloorToInt(currentTime / 60);
        float secs = Mathf.FloorToInt(currentTime % 60);

        timerText.text = string.Format("{0:00}:{1:00}", mins, secs);
    }

    public void AddTime(float seconds)
    {
        currentTime += seconds;
    }
}
