using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // Garante que o objeto sempre tenha um Rigidbody anexado
public class DragDrop : MonoBehaviour
{
    private Vector3 offset; // Armazena a diferença entre a posição do objeto e o toque inicial
    [SerializeField] private string destinationTag; // Tag do objeto de destino onde o item pode ser solto
    private bool dragging = false; // Indica se o objeto está sendo arrastado
    private Rigidbody rb; // Referência ao Rigidbody do objeto

    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); // Obtém o Rigidbody do objeto
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Melhora a precisão da detecção de colisão em movimento rápido
        rb.useGravity = true; // Mantém a gravidade ativada por padrão
    }

    void Update()
    {
        if (Input.touchCount > 0) // Verifica se há toque na tela
        {
            Touch touch = Input.GetTouch(0); // Obtém o primeiro toque detectado
            Ray ray = Camera.main.ScreenPointToRay(touch.position); // Cria um raio a partir da câmera na direção do toque

            switch (touch.phase)
            {
                case TouchPhase.Began: // Quando o toque começa
                    if (Physics.Raycast(ray, out RaycastHit hit) && hit.transform == transform) // Verifica se o toque foi no objeto
                    {
                        offset = transform.position - GetTouchWorldPosition(touch); // Calcula a diferença entre o toque e a posição do objeto
                        dragging = true; // Define que o objeto está sendo arrastado
                        rb.useGravity = false; // Desativa a gravidade enquanto arrasta
                        rb.velocity = Vector3.zero; // Para qualquer movimento anterior
                        rb.angularVelocity = Vector3.zero; // Para qualquer rotação inesperada
                    }
                    break;

                case TouchPhase.Moved: // Quando o toque se move na tela
                    if (dragging)
                    {
                        Vector3 targetPos = GetTouchWorldPosition(touch) + offset; // Atualiza a posição do objeto de acordo com o toque
                        rb.MovePosition(targetPos); // Move o objeto suavemente para a nova posição
                    }
                    break;

                case TouchPhase.Ended: // Quando o toque termina
                    if (dragging)
                    {
                        dragging = false; // Define que o objeto não está mais sendo arrastado
                        RaycastHit dropHit;

                        // Verifica se há um destino válido próximo ao soltar
                        if (Physics.Raycast(ray, out dropHit) && dropHit.transform.CompareTag(destinationTag))
                        {
                            rb.MovePosition(dropHit.transform.position); // Move o objeto para a posição do destino
                        }

                        rb.useGravity = true; // Reativa a gravidade ao soltar
                    }
                    break;
            }
        }
    }

    // Converte a posição do toque na tela para a posição no mundo, garantindo que ele fique no mesmo plano do objeto
    Vector3 GetTouchWorldPosition(Touch touch)
    {
        Plane plane = new Plane(Vector3.up, transform.position); // Cria um plano horizontal no nível do objeto
        Ray ray = Camera.main.ScreenPointToRay(touch.position); // Cria um raio na direção do toque
        if (plane.Raycast(ray, out float distance)) // Verifica onde o raio intersecta o plano
        {
            return ray.GetPoint(distance); // Retorna a posição exata do toque no mundo 3D
        }
        return transform.position; // Se não houver interseção, mantém a posição atual
    }
}