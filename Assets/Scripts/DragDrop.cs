using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging = false;
    private Rigidbody rb;

    [SerializeField] private string computerZoneTag = "ComputerZone"; // Tag da área do computador
    [SerializeField] private Vector3 alignedOffset = new Vector3(0, 0, -1); // Ajuste de posição para alinhar o peão
    [SerializeField] private float alignmentSpeed = 5f;
    [SerializeField] private LayerMask draggableLayer; // Layer para garantir que o Raycast acerte apenas o peão

    private Transform currentComputerZone = null;
    private bool isInsideComputerZone = false;
    private bool isResolvingPopup = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = true;
    }

    void Update()
    {
        if (isResolvingPopup) return; // Impede que o peão seja movido enquanto resolve o pop-up

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
                    }
                    break;

                case TouchPhase.Moved:
                    if (dragging)
                    {
                        Vector3 targetPos = GetTouchWorldPosition(touch) + offset;
                        rb.MovePosition(targetPos);
                    }
                    break;

                case TouchPhase.Ended:
                    dragging = false;
                    rb.useGravity = true;

                    if (isInsideComputerZone && currentComputerZone != null)
                    {
                        Debug.Log("Chegou aq");
                        // Verifica se o computador tem um pop-up e se o peão tem a tag necessária para resolvê-lo
                        ComputerPopup compPopup = currentComputerZone.GetComponent<ComputerPopup>();
                        if (compPopup != null && compPopup.CanResolvePopup(gameObject.tag))
                        {
                            Debug.Log("Resolvendo");
                            // Alinha o peão com o computador e inicia a resolução do pop-up
                            AlignWithComputer(currentComputerZone);
                            StartCoroutine(compPopup.ResolvePopup());
                            isResolvingPopup = true;
                            StartCoroutine(ResetResolvingFlag(compPopup.resolutionTime));
                        }
                        else
                        {
                            Debug.Log("Pc sem popups");
                            // Caso não haja pop-up ou o peão não seja o correto, apenas alinha o peão com o computador
                            AlignWithComputer(currentComputerZone);
                        }
                    }

                    isInsideComputerZone = false;
                    currentComputerZone = null;
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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(computerZoneTag))
        {
            isInsideComputerZone = true;
            currentComputerZone = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(computerZoneTag))
        {
            isInsideComputerZone = false;
            currentComputerZone = null;
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
