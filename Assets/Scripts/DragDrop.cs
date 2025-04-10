using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging = false;
    private Rigidbody rb;
    private GameObject ghost; // Ghost (clone semi-transparente do peão)

    [SerializeField] private string computerZoneTag = "ComputerZone";
    [SerializeField] private Vector3 alignedOffset = new Vector3(0, 0, -1);
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private LayerMask draggableLayer;
    [SerializeField] private LayerMask computerZoneLayer;
    [SerializeField] private Material transparentMaterial; // Material usado para o ghost

    // Tempo para resolver o popup (configurável via Inspector)
    [SerializeField] private float popupResolutionTime = 3f;

    private Transform currentComputerZone = null;
    private bool isResolvingPopup = false;

    [SerializeField] private Animator animator; // Referência ao Animator no filho

    // Variáveis para debug visual (usadas via OnGUI)
    private string debugMessage = "";
    private float debugMessageTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = true;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Se estiver resolvendo, exibe a mensagem e força o personagem a permanecer "sentado"
        if (isResolvingPopup)
        {
            SetAnimationStates(true, false); // Sentado = true, Correndo = false
            ShowDebugMessage("Resolvendo popup...", 0.5f);
            return;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, draggableLayer) && hit.transform == transform)
                    {
                        offset = transform.position - GetTouchWorldPosition(touch);
                        dragging = true;
                        rb.useGravity = false;
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;

                        // Cria o ghost semi-transparente
                        ghost = Instantiate(gameObject, transform.position, transform.rotation);
                        ApplyTransparency(ghost);
                        ghost.layer = LayerMask.NameToLayer("draggableLayer");

                        // Desativa a colisão entre o ghost e o objeto original
                        Collider ghostCollider = ghost.GetComponent<Collider>();
                        Collider pawnCollider = GetComponent<Collider>();
                        if (ghostCollider != null && pawnCollider != null)
                        {
                            Physics.IgnoreCollision(ghostCollider, pawnCollider, true);
                        }

                        // Verifica imediatamente se o ghost está numa zona
                        CheckForComputerZone(ghost.transform.position);
                    }
                    break;

                case TouchPhase.Moved:
                    if (dragging && ghost != null)
                    {
                        Vector3 targetPos = GetTouchWorldPosition(touch) + offset;
                        ghost.transform.position = targetPos;
                        CheckForComputerZone(ghost.transform.position);
                        SetAnimationStates(false, true); // Correndo = true
                        ShowDebugMessage("Arrastando...", 0.5f);
                    }
                    break;

                case TouchPhase.Ended:
                    if (dragging)
                    {
                        dragging = false;
                        rb.useGravity = true;
                        SetAnimationStates(false, false); // Zera a animação "Correndo"

                        // Em vez de usar a posição do ghost, utilize a posição final do toque
                        Vector3 finalTouchPos = GetTouchWorldPosition(touch);
                        CheckForComputerZone(finalTouchPos);

                        // Armazena o nome da zona que foi detectada
                        string zonaChecada = (currentComputerZone != null) ? currentComputerZone.name : "Nenhuma zona detectada";

                        if (currentComputerZone != null)
                        {
                            Debug.Log("Soltou dentro da zona: " + zonaChecada);
                            ComputerPopup compPopup = currentComputerZone.GetComponent<ComputerPopup>();
                            if (compPopup != null && compPopup.CanResolvePopup(gameObject.tag))
                            {
                                AlignWithComputer(currentComputerZone);
                                StartCoroutine(compPopup.ResolvePopup(popupResolutionTime));
                                isResolvingPopup = true;
                                StartCoroutine(ResetResolvingFlag(popupResolutionTime));
                                SetAnimationStates(true, false); // Sentado = true
                                ShowDebugMessage("Iniciando resolução do popup (Sentado)", 1f);
                            }
                            else
                            {
                                AlignWithComputer(currentComputerZone);
                                ShowDebugMessage("Zona válida, mas sem popup ou peão incorreto", 1f);
                            }
                        }
                        else
                        {
                            ShowDebugMessage("Zona não detectada", 1f);
                        }

                        // Exibe na tela o resultado da detecção antes de destruir o ghost
                        ShowDebugMessage("Ghost checou: " + zonaChecada, 2f);

                        // Restaura colisões e destrói o ghost
                        if (ghost != null)
                        {
                            Collider ghostCollider = ghost.GetComponent<Collider>();
                            Collider pawnCollider = GetComponent<Collider>();
                            if (ghostCollider != null && pawnCollider != null)
                            {
                                Physics.IgnoreCollision(ghostCollider, pawnCollider, false);
                            }
                            Destroy(ghost);
                            ghost = null;
                        }

                        currentComputerZone = null;
                    }
                    break;
            }
        }
        else
        {
            SetAnimationStates(false, false); // Nenhuma animação ativa
        }
    }

    // Exibe uma mensagem de debug na tela por um período determinado
    private void ShowDebugMessage(string message, float duration)
    {
        debugMessage = message;
        debugMessageTimer = duration;
    }

    // Exibe a mensagem na tela usando OnGUI
    private void OnGUI()
    {
        if (debugMessageTimer > 0)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, 10, 600, 30), debugMessage, style);
            debugMessageTimer -= Time.deltaTime;
        }
    }

    Vector3 GetTouchWorldPosition(Touch touch)
    {
        Plane plane = new Plane(Vector3.up, transform.position);
        Ray ray = Camera.main.ScreenPointToRay(touch.position);
        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return transform.position;
    }

    /// <summary>
    /// Verifica se há uma zona de computador na posição informada,
    /// atualizando currentComputerZone se encontrar um Collider com a tag correta.
    /// </summary>
    private void CheckForComputerZone(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f, computerZoneLayer);
        currentComputerZone = null;
        foreach (var col in colliders)
        {
            if (col.CompareTag(computerZoneTag))
            {
                currentComputerZone = col.transform;
                break;
            }
        }
    }

    /// <summary>
    /// Alinha o objeto com a zona (computador) de destino de forma suave.
    /// </summary>
    private void AlignWithComputer(Transform computer)
    {
        Vector3 targetPosition = computer.position + alignedOffset;
        Quaternion targetRotation = Quaternion.LookRotation(computer.forward);
        StartCoroutine(SmoothAlignment(targetPosition, targetRotation));
    }

    private IEnumerator SmoothAlignment(Vector3 targetPosition, Quaternion targetRotation)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * alignmentSpeed;
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime);
            yield return null;
        }
        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }

    private IEnumerator ResetResolvingFlag(float time)
    {
        yield return new WaitForSeconds(time);
        isResolvingPopup = false;
        SetAnimationStates(false, false);
    }

    // Aplica o material semi-transparente a todos os renderers do objeto (para criar o efeito ghost)
    private void ApplyTransparency(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.material = transparentMaterial;
        }
    }

    // Atualiza os parâmetros do Animator para controlar as animações
    private void SetAnimationStates(bool sentado, bool correndo)
    {
        if (animator != null)
        {
            animator.SetBool("Sentado", sentado);
            animator.SetBool("Correndo", correndo);
        }
    }
}
