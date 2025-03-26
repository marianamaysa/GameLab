using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopUpController : MonoBehaviour
{
    [Header("Configurações do Popup")]
    public string requiredPawnTag;     // Tag do peão que resolve esse popup
    public float resolutionTime = 3f;    // Tempo para resolver o popup

    [Header("Configurações de Reposicionamento do Peão")]
    public Transform newPawnSpawnPoint;  // Ponto para reposicionar o peão, se necessário

    private bool isResolving = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isResolving) return; // Verifica se o popup já não está sendo resolvido


        Debug.Log("Colisão detectada com: " + other.tag);

        if (other.CompareTag(requiredPawnTag)) 
        {
            if (other.CompareTag(requiredPawnTag)) // Checa se a tag específica do peão bate com o que o popup requer

            {
                Debug.Log("Peão correto detectado. Iniciando resolução...");
                StartCoroutine(ResolvePopup(other.gameObject));
            }
            else
            {
                Debug.LogError("Peão incorreto! A resolução não pode ser concluída.");

            }
        }
    }

    private IEnumerator ResolvePopup(GameObject pawn)
    {
        isResolving = true;
        Debug.Log("Resolvendo popup...");
        yield return new WaitForSeconds(resolutionTime);

        TimerLevel timer = FindObjectOfType<TimerLevel>();
        if (timer != null)
        {
            timer.AddTime(5f);
            Debug.Log("+5 segundos adicionados ao timer!");
        }

        Debug.Log("Popup concluído com sucesso!");
        Destroy(gameObject);

        if (newPawnSpawnPoint != null) // reposicionar o peão para outro local:

        {
            pawn.transform.position = newPawnSpawnPoint.position;
        }
    }
}
