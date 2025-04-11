using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public AudioClip fase1Music;
    public void Change(string scene)
    {
        if (scene == "Fase 1Mariana" && AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic(); 
            AudioManager.Instance.PlayMusic(fase1Music);
        }

        SceneManager.LoadScene(scene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
