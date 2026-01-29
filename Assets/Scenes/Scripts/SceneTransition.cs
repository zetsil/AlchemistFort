using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Header("Configurație Tranziție")]
    [SerializeField] private string targetSceneName; // Numele scenei în care mergem
    [Header("Spawn Point (Manual)")]
    [Tooltip("Coordonatele unde va apărea jucătorul în noua scenă.")]
    public Vector3 spawnPosition;
    public float spawnRotationYaw;

    private bool istransitioning = false;

    /// <summary>
    /// Metoda principală care pornește procesul de schimbare a scenei.
    /// Poate fi apelată dintr-un script de tip Trigger sau prin apasarea unei taste.
    /// </summary>
    public void ExecuteTransition()
    {
        if (istransitioning) return;
        istransitioning = true;

        if (SaveManager.Instance != null)
        {
            Debug.Log($"<color=magenta>[Transition]</color> Salvare în Cache pentru scena: {SceneManager.GetActiveScene().name}");
            
            // 1. Salvăm tot ce s-a întâmplat în scena curentă în Dictionary-ul din SaveManager
            SaveManager.Instance.HandleSceneTransition();

            // 2. (Opțional) Salvăm și poziția jucătorului pentru scena viitoare dacă e nevoie
            // SaveManager.Instance.SavePlayerPosition(SaveManager.Instance.GetCurrentSaveFolderPath());
        }
        else
        {
            Debug.LogWarning("[Transition] SaveManager.Instance nu a fost găsit! Datele scenei se vor pierde.");
        }

        // 3. Încărcăm noua scenă
        StartCoroutine(LoadSceneRoutine());
    }

    private IEnumerator LoadSceneRoutine()
    {
        yield return new WaitForSeconds(0.1f);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetPendingPlayerSpawn(spawnPosition, spawnRotationYaw);
        }

        SceneManager.LoadSceneAsync(targetSceneName);
    }



}