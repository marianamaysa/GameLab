using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging = false;
    private Rigidbody rb;
    private GameObject ghost; // Ghost (clone semi-transparente do peão)
    [SerializeField] private Animator animator; // Referência ao Animator presente no filho

    [Header("PopUp")]
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

    [Header("Musica e SFX")]
    [SerializeField] public AudioClip placementSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip resolvingSound;


    private void Awake()
    {

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

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
                    }
                    break;

                case TouchPhase.Ended:
                    if (dragging)
                    {
                        dragging = false;
                        rb.useGravity = true;
                        SetAnimationStates(false, false); // Zera a animação "Correndo"

                        // Usa a posição final do toque para a verificação
                        Vector3 finalTouchPos = GetTouchWorldPosition(touch);
                        CheckForComputerZone(finalTouchPos);

                        if (currentComputerZone != null)
                        {
                            ComputerPopup compPopup = currentComputerZone.GetComponent<ComputerPopup>();
                            if (compPopup != null && compPopup.CanResolvePopup(gameObject.tag))
                            {
                                if (placementSound != null)
                                    audioSource.PlayOneShot(placementSound);

                                StartCoroutine(AlignAndHoldAtComputer(currentComputerZone));
                                StartCoroutine(compPopup.ResolvePopup(popupResolutionTime));
                                isResolvingPopup = true;
                                StartCoroutine(ResetResolvingFlag(popupResolutionTime));
                                SetAnimationStates(true, false); // Sentado = true
                            }
                            else
                            {
                                StartCoroutine(AlignAndHoldAtComputer(currentComputerZone));
                            }
                        }

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

    /// Verifica se há uma zona de computador na posição informada.
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

    /// Alinha o peão com a zona (computador) de destino de forma suave e fixa sua posição.
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
        transform.position = targetPosition;
        transform.rotation = targetRotation;
        rb.isKinematic = true; // Fixa o peão na posição final
    }

    private IEnumerator ResetResolvingFlag(float time)
    {
        if (resolvingSound != null)
        {
            audioSource.clip = resolvingSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        yield return new WaitForSeconds(time);

        if (audioSource.isPlaying && audioSource.clip == resolvingSound)
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.loop = false;
        }

        isResolvingPopup = false;
        SetAnimationStates(false, false);
        rb.isKinematic = false; // Permite que o peão seja movido novamente
    }

    // Aplica o material semi-transparente a todos os renderers do objeto ghost
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
