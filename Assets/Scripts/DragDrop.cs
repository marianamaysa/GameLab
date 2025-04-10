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
                        // Quando iniciar o drag, garante que o Rigidbody esteja dinâmico
                        rb.isKinematic = false;
                        rb.useGravity = false;
                        offset = transform.position - GetTouchWorldPosition(touch);
                        dragging = true;
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

                        // Usa a posição final do toque para verificação
                        Vector3 finalTouchPos = GetTouchWorldPosition(touch);
                        CheckForComputerZone(finalTouchPos);

                        string zonaChecada = (currentComputerZone != null) ? currentComputerZone.name : "Nenhuma zona detectada";

                        if (currentComputerZone != null)
                        {
                            Debug.Log("Soltou dentro da zona: " + zonaChecada);
                            ComputerPopup compPopup = currentComputerZone.GetComponent<ComputerPopup>();
                            if (compPopup != null && compPopup.CanResolvePopup(gameObject.tag))
                            {
                                // Inicia o alinhamento do objeto para a zona, garantindo que o peão fique fixo lá
                                StartCoroutine(AlignAndHoldAtComputer(currentComputerZone));
                                StartCoroutine(compPopup.ResolvePopup(popupResolutionTime));
                                isResolvingPopup = true;
                                StartCoroutine(ResetResolvingFlag(popupResolutionTime));
                                SetAnimationStates(true, false); // Sentado = true
                                ShowDebugMessage("Iniciando resolução do popup (Sentado)", 1f);
                            }
                            else
                            {
                                // Apenas alinha se a zona for válida mas não houver pop-up ou se o peão estiver errado
                                StartCoroutine(AlignAndHoldAtComputer(currentComputerZone));
                                ShowDebugMessage("Zona válida, mas sem popup ou peão incorreto", 1f);
                            }
                        }
                        else
                        {
                            ShowDebugMessage("Zona não detectada", 1f);
                        }

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

    private void ShowDebugMessage(string message, float duration)
    {
        debugMessage = message;
        debugMessageTimer = duration;
    }

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
    /// Verifica se há uma zona de computador na posição informada.
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
    /// Alinha o peão com a zona de destino de forma suave e, ao final, fixa sua posição.
    /// </summary>
    private IEnumerator AlignAndHoldAtComputer(Transform computer)
    {
        Vector3 targetPosition = computer.position + alignedOffset;
        Quaternion targetRotation = Quaternion.LookRotation(computer.forward);

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
        // Garante que a posição final seja exatamente o destino
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        // Torna o Rigidbody kinematic para "fixar" o peão na posição final
        rb.isKinematic = true;
    }

    private IEnumerator ResetResolvingFlag(float time)
    {
        yield return new WaitForSeconds(time);
        isResolvingPopup = false;
        SetAnimationStates(false, false);
        // Permite que o peão seja movido novamente
        rb.isKinematic = false;
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
