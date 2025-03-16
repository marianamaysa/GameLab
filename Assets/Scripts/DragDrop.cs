using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private LayerMask draggableLayer; // Adicionamos um LayerMask para evitar a ComputerZone no Raycast

    private Transform currentComputerZone = null;
    private bool isInsideComputerZone = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = true;
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    // Agora o Raycast só atinge objetos na camada "draggableLayer"
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
                        AlignWithComputer(currentComputerZone);
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
        float elapsedTime = 0;
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
}
