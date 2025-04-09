using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging = false;
    private Rigidbody rb;

    [SerializeField] private string computerZoneTag = "ComputerZone";
    [SerializeField] private Vector3 alignedOffset = new Vector3(0, 0, -1);
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private LayerMask draggableLayer;
    [SerializeField] private LayerMask computerZoneLayer;

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

                        CheckForComputerZone(); // <- Checa manualmente se já está dentro
                    }
                    break;

                case TouchPhase.Moved:
                    if (dragging)
                    {
                        Vector3 targetPos = GetTouchWorldPosition(touch) + offset;
                        rb.MovePosition(targetPos);

                        // Verifica constantemente se está dentro da zona durante o arrasto
                        CheckForComputerZone();

                        if (currentComputerZone != null)
                        {
                            Debug.Log("Peão está dentro da zona do computador enquanto arrasta.");
                        }
                    }
                    break;

                case TouchPhase.Ended:
                    if (dragging)
                    {
                        dragging = false;
                        rb.useGravity = true;

                        CheckForComputerZone(); // <- Checa novamente ao soltar

                        if (currentComputerZone != null)
                        {
                            Debug.Log("Soltou dentro da zona.");

                            ComputerPopup compPopup = currentComputerZone.GetComponent<ComputerPopup>();
                            if (compPopup != null && compPopup.CanResolvePopup(gameObject.tag))
                            {
                                Debug.Log("Resolvendo pop-up!");
                                AlignWithComputer(currentComputerZone);
                                StartCoroutine(compPopup.ResolvePopup());
                                isResolvingPopup = true;
                                StartCoroutine(ResetResolvingFlag(compPopup.resolutionTime));
                            }
                            else
                            {
                                Debug.Log("Zona válida, mas sem pop-up ou peão errado.");
                                AlignWithComputer(currentComputerZone);
                            }
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

    // Substitui completamente OnTriggerEnter/Exit com uma detecção confiável
    private void CheckForComputerZone()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.5f, computerZoneLayer);
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
}
