using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    [SerializeField] private string destinationTag;
    private bool dragging = false;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }
    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    //raycast para verificar se o toque começou sobre esse objeto
                    Ray ray = Camera.main.ScreenPointToRay(touch.position);
                    if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform)
                    {
                        offset = transform.position - GetTouchWorldPosition(touch);
                        dragging = true;
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
                    if (dragging)
                    {
                        //utiliza RaycastAll para encontrar o objeto de drop
                        Vector3 rayOrigin = Camera.main.transform.position;
                        Vector3 rayDirection = GetTouchWorldPosition(touch) - Camera.main.transform.position;
                        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection);

                        foreach (RaycastHit dropHit in hits)
                        {
                            if (dropHit.transform == transform)
                                continue;

                            if (dropHit.transform.CompareTag(destinationTag))
                            {
                                rb.MovePosition(dropHit.transform.position);
                                break;
                            }
                        }
                        dragging = false;
                    }
                    break;
            }
        }
    }

    Vector3 GetTouchWorldPosition(Touch touch)
    {
        //converte a posição do toque para coordenadas do mundo, mantendo a mesma distância em z que o objeto
        Vector3 touchScreenPos = new Vector3(touch.position.x, touch.position.y, Camera.main.WorldToScreenPoint(transform.position).z);
        return Camera.main.ScreenToWorldPoint(touchScreenPos);
    }
}
