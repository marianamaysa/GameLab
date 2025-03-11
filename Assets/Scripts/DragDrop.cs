using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragDrop : MonoBehaviour
{
    private Vector3 offset;
    [SerializeField] private string destinationTag;
    private bool dragging = false;

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
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit) && hit.transform == transform)
                    {
                        offset = transform.position - GetTouchWorldPosition(touch);
                        //desabilita o collider para não interferir no raycast durante o arraste
                        GetComponent<Collider>().enabled = false;
                        dragging = true;
                    }
                    break;

                case TouchPhase.Moved:
                    if (dragging)
                    {
                        transform.position = GetTouchWorldPosition(touch) + offset;
                    }
                    break;

                case TouchPhase.Ended:
                    if (dragging)
                    {
                        //reabilita o collider
                        GetComponent<Collider>().enabled = true;

                        //realiza um raycast para detectar se o objeto foi solto sobre a área de destino
                        Vector3 rayOrigin = Camera.main.transform.position;
                        Vector3 rayDirection = GetTouchWorldPosition(touch) - Camera.main.transform.position;
                        RaycastHit hitInfo;
                        if (Physics.Raycast(rayOrigin, rayDirection, out hitInfo))
                        {
                            if (hitInfo.transform.CompareTag(destinationTag))
                            {
                                transform.position = hitInfo.transform.position;
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
        //converte a posição do toque para coordenadas do mundo, mantendo a mesma distância em z que o objeto.
        Vector3 touchScreenPos = new Vector3(touch.position.x, touch.position.y, Camera.main.WorldToScreenPoint(transform.position).z);
        return Camera.main.ScreenToWorldPoint(touchScreenPos);
    }
}
