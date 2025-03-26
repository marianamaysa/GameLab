using System.Collections;
using UnityEngine;

public class ComputerPopup : MonoBehaviour
{
    [System.Serializable]
    public class PopupData
    {
        public GameObject popupPrefab;  // Prefab do pop-up
        public string requiredPawnTag;  // Tag do pe�o que resolve esse pop-up
    }

    public PopupData[] popups = new PopupData[3]; // Lista com 3 pop-ups diferentes
    public float popupIntervalMin = 5f; 
    public float popupIntervalMax = 15f; 
    public float resolutionTime = 3f; 

    private bool hasPopup = false;
    private GameObject currentPopupObject = null;
    private PopupData currentPopup; 

    private void Start()
    {
        StartCoroutine(SpawnPopupRoutine());
    }

    IEnumerator SpawnPopupRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(popupIntervalMin, popupIntervalMax));
            if (!hasPopup)
            {
                ShowPopup();
            }
        }
    }

    void ShowPopup()
    {
        hasPopup = true;
        currentPopup = popups[Random.Range(0, popups.Length)]; // Escolhe um dos 3 pop-ups

        // Instancia o pop-up na posi��o do computador
        currentPopupObject = Instantiate(currentPopup.popupPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity);
        Debug.Log($"Pop-up '{currentPopupObject.name}' apareceu no {gameObject.name}! Requer: {currentPopup.requiredPawnTag}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"hasPopup: {hasPopup}, currentPopup.requiredPawnTag: {currentPopup?.requiredPawnTag}");
        Debug.Log("Tag do objeto colidido: " + other.tag);
        Debug.Log("Colidu com o popup");
        if (hasPopup && other.CompareTag(currentPopup.requiredPawnTag))
        {
            Debug.Log("Resolvendo");
            StartCoroutine(ResolvePopup());
        }
    }

    private IEnumerator ResolvePopup()
    {
        Debug.Log($"Resolvendo '{currentPopupObject.name}'...");
        yield return new WaitForSeconds(resolutionTime);

        // Remove o pop-up da cena
        Destroy(currentPopupObject);
        hasPopup = false;
        Debug.Log("Pop-up resolvido!");
    }
}
