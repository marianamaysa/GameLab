using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public void Change(string scene)
    {

        if(scene == "Fase 1" && AudioManager.Instance != null)
        {
            AudioManager.Instance.GetComponent<AudioSource>().Stop();
        }
        SceneManager.LoadScene(scene);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
