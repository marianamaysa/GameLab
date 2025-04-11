using System.Collections;
using TMPro;
using UnityEngine;

public class TimerLevel : MonoBehaviour
{
    [SerializeField] private float startTimer;
    private float currentTime;

    [SerializeField] private TMP_Text timerText;
    [SerializeField] private GameObject objectToActivate; // Objeto a ser ativado

    private bool hasActivated = false;

    void Start()
    {
        currentTime = startTimer;

        if (objectToActivate != null)
        {
            objectToActivate.SetActive(false); // Garante que come�a desativado
        }
    }

    void Update()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            if (currentTime > 60)
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
            if (!hasActivated && objectToActivate != null)
            {
                objectToActivate.SetActive(true);
                hasActivated = true; // Impede de ativar m�ltiplas vezes
            }
        }
    }

    void FormatTimer()
    {
        float mins = Mathf.FloorToInt(currentTime / 60);
        float secs = Mathf.FloorToInt(currentTime % 60);

        timerText.text = string.Format("{0:00}:{1:00}", mins, secs);
    }

    public void AddTime(float timer)
    {
        currentTime += timer;
        hasActivated = false; // Permite que a ativa��o aconte�a de novo se voc� resetar o tempo
    }
}
