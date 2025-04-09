using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging = false;
    private Rigidbody rb;
    private GameObject clone; // Clone semi-transparente do peão

    [SerializeField] private string computerZoneTag = "ComputerZone";
    [SerializeField] private Vector3 alignedOffset = new Vector3(0, 0, -1);
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private LayerMask draggableLayer;
    [SerializeField] private LayerMask computerZoneLayer;
    [SerializeField] private Material transparentMaterial; // Material semi-transparente para o clone

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

                        // Criar clone semi-transparente
                        clone = Instantiate(gameObject, transform.position, transform.rotation);
                        ApplyTransparency(clone);
                    }
                    break;

                case TouchPhase.Moved:
                    if (dragging && clone != null)
                    {
                        Vector3 targetPos = GetTouchWorldPosition(touch) + offset;
                        clone.transform.position = targetPos;

                        // Verifica constantemente se o clone está dentro da zona durante o arrasto
                        CheckForComputerZone(clone.transform.position);
                    }
                    break;

                case TouchPhase.Ended:
                    if (dragging)
                    {
                        dragging = false;
                        rb.useGravity = true;

                        if (currentComputerZone != null)
                        {
                            ComputerPopup compPopup = currentComputerZone.GetComponent<ComputerPopup>();
                            if (compPopup != null && compPopup.CanResolvePopup(gameObject.tag))
                            {
                                AlignWithComputer(currentComputerZone);
                                StartCoroutine(compPopup.ResolvePopup());
                                isResolvingPopup = true;
                                StartCoroutine(ResetResolvingFlag(compPopup.resolutionTime));
                            }
                            else
                            {
                                AlignWithComputer(currentComputerZone);
                            }
                        }

                        // Destruir o clone após o arrasto
                        if (clone != null)
                        {
                            Destroy(clone);
                            clone = null;
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
