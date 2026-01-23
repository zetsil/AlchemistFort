using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;

public class SceneTransitionDoor : MonoBehaviour
{
    [Header("Scene Transition")]
    public string targetScene;
    public Vector3 spawnPositionInTargetScene;
    public float spawnYaw;

    private bool isTransitioning = false;

    public void TriggerTransition()
    {
        if (isTransitioning) return;
        isTransitioning = true;
        StartCoroutine(TransitionRoutine());
    }

    private IEnumerator TransitionRoutine()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"<color=yellow>[SceneTransition] Current scene: {currentScene}</color>");

        // 1️⃣ Autosave scena curentă
        Debug.Log($"<color=yellow>[SceneTransition] Performing AutoSave for scene: {currentScene}</color>");
        SaveManager.Instance.PerformAutoSave(currentScene);
        yield return new WaitForEndOfFrame();
        Debug.Log($"<color=yellow>[SceneTransition] AutoSave complete for scene: {currentScene}</color>");

        // 2️⃣ Setăm currentSaveName la autosave-ul scenei țintă
        string targetAutoSaveFolder = $"AutoSave_{targetScene}";
        Debug.Log($"<color=yellow>[SceneTransition] Switching SaveManager to target autosave: {targetAutoSaveFolder}</color>");
        SaveManager.Instance.currentSaveName = targetAutoSaveFolder;

        // 3️⃣ Full Load pentru autosave-ul scenei țintă
        if (Directory.Exists(Path.Combine(Application.persistentDataPath, "Saves", targetAutoSaveFolder)))
        {
            Debug.Log($"<color=yellow>[SceneTransition] Performing Full Load for scene: {targetScene}</color>");
            SaveManager.Instance.PerformFullLoad();
        }
        else
        {
            Debug.LogWarning($"<color=red>[SceneTransition] No autosave found for scene: {targetScene}. Full Load skipped.</color>");
        }

        // 4️⃣ Resetăm flag-ul de tranziție
        isTransitioning = false;
        Debug.Log($"<color=yellow>[SceneTransition] Transition finished</color>");
    }


}
