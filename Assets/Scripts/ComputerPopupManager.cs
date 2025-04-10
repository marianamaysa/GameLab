using System.Collections;
using UnityEngine;

public class ComputerPopupManager : MonoBehaviour
{
    [SerializeField]
    private ComputerPopup[] computers; // Lista dos computadores na cena

    [Header("Configurações de Spawn")]
    [SerializeField] private float initialInterval = 15f;  // Intervalo inicial entre pop-ups
    [SerializeField] private float minInterval = 3f;         // Intervalo mínimo permitido
    [SerializeField] private float intervalDecrease = 0.5f;    // Valor a diminuir a cada spawn
    [SerializeField] private float resolutionTime = 3f;        // Tempo para resolução do popup

    private float currentInterval;

    private void Start()
    {
        // Se os computadores não foram atribuídos pelo Inspector, tenta encontrar todos na cena
        if (computers == null || computers.Length == 0)
        {
            computers = FindObjectsOfType<ComputerPopup>();
        }
        currentInterval = initialInterval;
        StartCoroutine(SpawnRandomPopup());
    }

    private IEnumerator SpawnRandomPopup()
    {
        while (true)
        {
            // Espera o tempo atual entre spawns
            yield return new WaitForSeconds(currentInterval);

            // Escolhe aleatoriamente um computador da lista
            if (computers != null && computers.Length > 0)
            {
                int randomIndex = Random.Range(0, computers.Length);
                ComputerPopup chosenComputer = computers[randomIndex];

                // Se o computador não tiver um pop-up ativo, exibe um novo.
                if (chosenComputer != null)
                {
                    chosenComputer.ShowPopup();
                }
            }

            // Diminui o intervalo, mas não abaixo do valor mínimo
            currentInterval -= intervalDecrease;
            if (currentInterval < minInterval)
            {
                currentInterval = minInterval;
            }
        }
    }
}
