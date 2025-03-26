using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopUpController : MonoBehaviour
{
    [Header("Configura��es do Popup")]
    public string requiredPawnTag;     // Tag do pe�o que resolve esse popup
    public float resolutionTime = 3f;    // Tempo para resolver o popup

    [Header("Configura��es de Reposicionamento do Pe�o")]
    public Transform newPawnSpawnPoint;  // Ponto para reposicionar o pe�o, se necess�rio

    private bool isResolving = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isResolving) return; // Verifica se o popup j� n�o est� sendo resolvido


        Debug.Log("Colis�o detectada com: " + other.tag);

        if (other.CompareTag(requiredPawnTag)) 
        {
            if (other.CompareTag(requiredPawnTag)) // Checa se a tag espec�fica do pe�o bate com o que o popup requer

            {
                Debug.Log("Pe�o correto detectado. Iniciando resolu��o...");
                StartCoroutine(ResolvePopup(other.gameObject));
            }
            else
            {
                Debug.LogError("Pe�o incorreto! A resolu��o n�o pode ser conclu�da.");

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

        Debug.Log("Popup conclu�do com sucesso!");
        Destroy(gameObject);

        if (newPawnSpawnPoint != null) // reposicionar o pe�o para outro local:

        {
            pawn.transform.position = newPawnSpawnPoint.position;
        }
    }
}
