using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging = false;
    private Rigidbody rb;
    private GameObject ghost;

    [SerializeField] private string computerZoneTag = "ComputerZone";
    [SerializeField] private Vector3 alignedOffset = new Vector3(0, 0, -1);
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private LayerMask draggableLayer;
    [SerializeField] private LayerMask computerZoneLayer;
    [SerializeField] private Material transparentMaterial;
    [SerializeField] private float popupResolutionTime = 3f;

    private Transform currentComputerZone = null;
    private bool isResolvingPopup = false;

    [SerializeField] private Animator animator;

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

                        ghost = Instantiate(gameObject, transform.position, transform.rotation);
                        ApplyTransparency(ghost);
                        ghost.layer = LayerMask.NameToLayer("draggableLayer");

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
                                SetAnimationStates(true, false); // Sentado = true
                            }
                            else
                            {
                                Debug.Log("Zona válida, mas sem pop-up ou peão errado.");
                                AlignWithComputer(currentComputerZone);
                                SetAnimationStates(false, false);
                            }
                        }

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
            SetAnimationStates(false, false); // Nenhuma animação
        }
    }

    private void SetAnimationStates(bool sentado, bool correndo)
    {
        if (animator != null)
        {
            animator.SetBool("Sentado", sentado);
            animator.SetBool("Correndo", correndo);
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

    private void ApplyTransparency(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.material = transparentMaterial;
        }
    }
}
