using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class PopupData
{
    public GameObject popupPrefab;  // Prefab do pop-up
    public string requiredPawnTag;  // Tag do pe�o que resolve esse pop-up
    public AudioClip popupSound;    // �udio do pop-up
    public AudioClip resolvedSound; // �udio ao resolver o pop-up
}

public class ComputerPopup : MonoBehaviour
{
    public PopupData[] popups = new PopupData[3]; // Lista com 3 pop-ups diferentes
    [SerializeField] private AudioClip expiredSound; // Som ao expirar o pop-up


    // Vari�vel para definir a escala exata do pop-up
    public Vector3 popupScale = Vector3.one;

    private bool hasPopup = false;
    private GameObject currentPopupObject = null;
    private PopupData currentPopup;
    private AudioSource audioSource;
    [SerializeField] private float popupExpireTime = 10f; //tempo limite do popup na tela sem resolucao


    private void Start()
    {
        // Garante que haja um AudioSource no computador
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        // Removemos o StartCoroutine(SpawnPopupRoutine()) pois o manager ser� o respons�vel.
    }

    /// <summary>
    /// M�todo para for�ar a exibi��o de um pop-up neste computador.
    /// S� funciona se n�o houver outro pop-up ativo.
    /// </summary>
    public void ShowPopup()
    {
        // Se j� tiver um pop-up ativo, n�o faz nada.
        if (hasPopup) return;

        hasPopup = true;
        // Escolhe aleatoriamente um dos pop-ups
        currentPopup = popups[Random.Range(0, popups.Length)];

        // Instancia o pop-up na posi��o deste computador (um pouco acima)
        currentPopupObject = Instantiate(
            currentPopup.popupPrefab,
            transform.position + Vector3.up * 1f - transform.forward * -2f,
            Quaternion.identity,
            transform.parent  // Torna o popup filho do pai do computador
        );
        currentPopupObject.transform.localScale = popupScale;

        Debug.Log($"[ComputerPopup] Pop-up '{currentPopupObject.name}' apareceu em {transform.parent.name}! Requer: {currentPopup.requiredPawnTag}");

        // Toca o som do pop-up, se houver
        if (currentPopup.popupSound != null)
        {
            audioSource.PlayOneShot(currentPopup.popupSound);
        }
        StartCoroutine(PopupExpireRoutine());
    }

    /// <summary>
    /// Verifica se o pe�o com a tag indicada pode resolver o pop-up.
    /// </summary>
    public bool CanResolvePopup(string pawnTag)
    {
        bool canResolve = hasPopup && currentPopup != null && pawnTag == currentPopup.requiredPawnTag;
        Debug.Log($"[ComputerPopup] CanResolvePopup? Pawn tag: {pawnTag} | Required: {currentPopup?.requiredPawnTag} | Result: {canResolve}");
        return canResolve;
    }

    /// <summary>
    /// Inicia a resolu��o do pop-up, destruindo-o ap�s um tempo determinado.
    /// </summary>
    public IEnumerator ResolvePopup(float resolutionTime)
    {
        Debug.Log($"[ComputerPopup] Iniciando resolu��o do pop-up '{currentPopupObject?.name}'...");
        yield return new WaitForSeconds(resolutionTime);

        // Toca o som de resolu��o, se houver
        if (currentPopup.resolvedSound != null)
        {
            audioSource.PlayOneShot(currentPopup.resolvedSound);
        }

        // Se houver algum sistema de tempo (ex: TimerLevel), pode-se adicionar tempo aqui
        TimerLevel timer = FindObjectOfType<TimerLevel>();
        if (timer != null)
        {
            timer.AddTime(2f);
        }

        if (currentPopupObject != null)
        {
            Destroy(currentPopupObject);
            currentPopupObject = null;
        }
        hasPopup = false;
        Debug.Log("[ComputerPopup] Pop-up resolvido!");

        yield return new WaitForSeconds(resolutionTime);
    }

    private IEnumerator PopupExpireRoutine()
    {
        yield return new WaitForSeconds(popupExpireTime);

        if (hasPopup && currentPopupObject != null)
        {
            // Toca o som
            if (expiredSound != null)
            {
                audioSource.PlayOneShot(expiredSound);
            }

            Destroy(currentPopupObject);
            currentPopupObject = null;
            hasPopup = false;
            Debug.Log("[ComputerPopup] Pop-up expirado e removido.");

            // Reduz tempo do timer
            TimerLevel timer = FindObjectOfType<TimerLevel>();
            if (timer != null)
            {
                timer.AddTime(-3f);
            }
        }

    }
}
