using System.Collections;
using UnityEngine;

[System.Serializable]
public class PopupData
{
    public GameObject popupPrefab;  // Prefab do pop-up
    public string requiredPawnTag;  // Tag do pe�o que resolve esse pop-up
}

public class ComputerPopup : MonoBehaviour
{
    public PopupData[] popups = new PopupData[3]; // Lista com 3 pop-ups diferentes
    public float popupIntervalMin = 5f;
    public float popupIntervalMax = 15f;
    public float resolutionTime = 3f;

    // Vari�vel para definir a escala exata do pop-up
    public Vector3 popupScale = Vector3.one;

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
        currentPopup = popups[Random.Range(0, popups.Length)]; // Escolhe aleatoriamente um dos 3 pop-ups

        // Instancia o pop-up na posi��o do computador (levemente acima)
        // Alteramos o pai para transform.parent para garantir que o pop-up seja filho do computador e n�o do filho.
        currentPopupObject = Instantiate(
            currentPopup.popupPrefab,
            transform.position + Vector3.up * 2f,
            Quaternion.identity,
            transform.parent
        );
        // Define a escala exata desejada
        currentPopupObject.transform.localScale = popupScale;

        Debug.Log($"[ComputerPopup] Pop-up '{currentPopupObject.name}' apareceu em {transform.parent.name}! Requer: {currentPopup.requiredPawnTag}");
    }

    public bool CanResolvePopup(string pawnTag)
    {
        // Verifica se h� um pop-up ativo e se o pe�o tem a tag necess�ria para resolv�-lo
        bool canResolve = hasPopup && currentPopup != null && pawnTag == currentPopup.requiredPawnTag;
        Debug.Log($"[ComputerPopup] CanResolvePopup? Pawn tag: {pawnTag} | Required: {currentPopup?.requiredPawnTag} | Result: {canResolve}");
        return canResolve;
    }

    public IEnumerator ResolvePopup()
    {
        Debug.Log($"[ComputerPopup] Iniciando resolu��o do pop-up '{currentPopupObject?.name}'...");
        yield return new WaitForSeconds(resolutionTime);

        if (currentPopupObject != null)
        {
            Destroy(currentPopupObject);
            currentPopupObject = null;
        }
        hasPopup = false;
        Debug.Log("[ComputerPopup] Pop-up resolvido!");
    }
}
