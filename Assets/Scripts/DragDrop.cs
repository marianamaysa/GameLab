using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging = false;
    private Rigidbody rb;
    private GameObject ghost; // ghost semi-transparente do peão

    [SerializeField] private string computerZoneTag = "ComputerZone";
    [SerializeField] private Vector3 alignedOffset = new Vector3(0, 0, -1);
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private LayerMask draggableLayer;
    [SerializeField] private LayerMask computerZoneLayer;
    [SerializeField] private Material transparentMaterial; // Material semi-transparente para o ghost

    // Tempo para resolver o popup (configurável via Inspector)
    [SerializeField] private float popupResolutionTime = 3f;

    private Transform currentComputerZone = null;
    private bool isResolvingPopup = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = true;
    }

    void Update()
    {
        if (isResolvingPopup) return;

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

                        // Instancia o ghost semi-transparente
                        ghost = Instantiate(gameObject, transform.position, transform.rotation);
                        ApplyTransparency(ghost);
                        ghost.layer = LayerMask.NameToLayer("draggableLayer");

                        // Desativa a colisão entre o peão e o ghost
                        Collider ghostCollider = ghost.GetComponent<Collider>();
                        Collider pawnCollider = GetComponent<Collider>();
                        if (ghostCollider != null && pawnCollider != null)
                        {
                            Physics.IgnoreCollision(ghostCollider, pawnCollider, true);
                        }

                        // Opcional: verificar imediatamente se o ghost está em uma zona (caso comece dentro)
                        CheckForComputerZone(ghost.transform.position);
                    }
                    break;

                case TouchPhase.Moved:
                    if (dragging && ghost != null)
                    {
                        Vector3 targetPos = GetTouchWorldPosition(touch) + offset;
                        ghost.transform.position = targetPos;

                        // Durante o arrasto, verifica se o ghost está dentro da zona do computador
                        CheckForComputerZone(ghost.transform.position);
                        if (currentComputerZone != null)
                        {
                            Debug.Log("Peão (ghost) está dentro da zona do computador enquanto arrasta.");
                        }
                    }
                    break;

                case TouchPhase.Ended:
                    if (dragging)
                    {
                        dragging = false;
                        rb.useGravity = true;

                        // Use a posição final do ghost, se existir, para a verificação da zona
                        if (ghost != null)
                        {
                            CheckForComputerZone(ghost.transform.position);
                        }
                        else
                        {
                            CheckForComputerZone(transform.position);
                        }

                        if (currentComputerZone != null)
                        {
                            Debug.Log("Soltou dentro da zona.");
                            ComputerPopup compPopup = currentComputerZone.GetComponent<ComputerPopup>();
                            if (compPopup != null && compPopup.CanResolvePopup(gameObject.tag))
                            {
                                Debug.Log("Resolvendo pop-up!");
                                AlignWithComputer(currentComputerZone);
                                StartCoroutine(compPopup.ResolvePopup(popupResolutionTime));
                                isResolvingPopup = true;
                                StartCoroutine(ResetResolvingFlag(popupResolutionTime));
                            }
                            else
                            {
                                Debug.Log("Zona válida, mas sem pop-up ou peão errado.");
                                AlignWithComputer(currentComputerZone);
                            }
                        }
                        // Restaura o render
                        //SetRenderersEnable(true);

                        // Reativa a colisão entre o peão e o ghost antes de destruí-lo
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
    /// Atualiza currentComputerZone se encontrar um Collider com a tag correta.
    /// </summary>
    /// <param name="position">Posição a ser verificada</param>
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
    /// Alinha o objeto com o computador (zona) de destino de forma suave.
    /// </summary>
    /// <param name="computer">Transform da zona do computador</param>
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
    }

    /// <summary>
    /// Aplica material semi-transparente a todos os renderers do objeto para criar efeito de ghost.
    /// </summary>
    /// <param name="obj">GameObject a ser modificado</param>
    private void ApplyTransparency(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.material = transparentMaterial;
        }
    }
}
