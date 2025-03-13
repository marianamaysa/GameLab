using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // Garante que o objeto sempre tenha um Rigidbody anexado
public class DragDrop : MonoBehaviour
{
    private Vector3 offset; // Armazena a diferen�a entre a posi��o do objeto e o toque inicial
    [SerializeField] private string destinationTag; // Tag do objeto de destino onde o item pode ser solto
    private bool dragging = false; // Indica se o objeto est� sendo arrastado
    private Rigidbody rb; // Refer�ncia ao Rigidbody do objeto

    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); // Obt�m o Rigidbody do objeto
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Melhora a precis�o da detec��o de colis�o em movimento r�pido
        rb.useGravity = true; // Mant�m a gravidade ativada por padr�o
    }

    void Update()
    {
        if (Input.touchCount > 0) // Verifica se h� toque na tela
        {
            Touch touch = Input.GetTouch(0); // Obt�m o primeiro toque detectado
            Ray ray = Camera.main.ScreenPointToRay(touch.position); // Cria um raio a partir da c�mera na dire��o do toque

            switch (touch.phase)
            {
                case TouchPhase.Began: // Quando o toque come�a
                    if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform) // Verifica se o toque foi no objeto
                    {
                        offset = transform.position - GetTouchWorldPosition(touch); // Calcula a diferen�a entre o toque e a posi��o do objeto
                        dragging = true; // Define que o objeto est� sendo arrastado
                        rb.useGravity = false; // Desativa a gravidade enquanto arrasta
                        rb.velocity = Vector3.zero; // Para qualquer movimento anterior
                        rb.angularVelocity = Vector3.zero; // Para qualquer rota��o inesperada
                    }
                    break;

                case TouchPhase.Moved: // Quando o toque se move na tela
                    if (dragging)
                    {
                        Vector3 targetPos = GetTouchWorldPosition(touch) + offset; // Atualiza a posi��o do objeto de acordo com o toque
                        rb.MovePosition(targetPos); // Move o objeto suavemente para a nova posi��o
                    }
                    break;

                case TouchPhase.Ended: // Quando o toque termina
                    if (dragging)
                    {
                        dragging = false; // Define que o objeto n�o est� mais sendo arrastado
                        RaycastHit dropHit;

                        // Verifica se h� um destino v�lido pr�ximo ao soltar
                        if (Physics.Raycast(ray, out dropHit) && dropHit.transform.CompareTag(destinationTag))
                        {
                            rb.MovePosition(dropHit.transform.position); // Move o objeto para a posi��o do destino
                        }

                        rb.useGravity = true; // Reativa a gravidade ao soltar
                    }
                    break;
            }
        }
    }

    // Converte a posi��o do toque na tela para a posi��o no mundo, garantindo que ele fique no mesmo plano do objeto
    Vector3 GetTouchWorldPosition(Touch touch)
    {
        Plane plane = new Plane(Vector3.up, transform.position); // Cria um plano horizontal no n�vel do objeto
        Ray ray = Camera.main.ScreenPointToRay(touch.position); // Cria um raio na dire��o do toque
        if (plane.Raycast(ray, out float distance)) // Verifica onde o raio intersecta o plano
        {
            return ray.GetPoint(distance); // Retorna a posi��o exata do toque no mundo 3D
        }
        return transform.position; // Se n�o houver interse��o, mant�m a posi��o atual
    }
}