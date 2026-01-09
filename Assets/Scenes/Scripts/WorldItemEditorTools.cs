#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class WorldItemEditorTools : Editor
{
    [MenuItem("Tools/Save System/Assign Unique IDs to World Items")]
    public static void AssignUniqueIDs()
    {
        // Găsește toate instanțele de WorldItem din scena curentă
        WorldEntityState[] allWorldItems = GameObject.FindObjectsOfType<WorldEntityState>();
        int assignedCount = 0;
        int skippedCount = 0;

        // Folosim Undo.RecordObjects pentru a putea da "Ctrl+Z" dacă greșim ceva
        Undo.RecordObjects(allWorldItems, "Assign Unique IDs");

        foreach (WorldEntityState item in allWorldItems)
        {
            // Setăm ID-ul doar dacă:
            // 1. Nu are deja unul
            // 2. NU este marcat ca spawnat la runtime (obiectele din editor sunt "originale")
            if (string.IsNullOrEmpty(item.uniqueID) && !item.isSpawnedAtRuntime)
            {
                item.uniqueID = Guid.NewGuid().ToString();
                
                // Marcăm obiectul ca "murdar" pentru ca Unity să știe că trebuie salvat
                EditorUtility.SetDirty(item);
                assignedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        // Forțăm salvarea scenei pentru a păstra ID-urile
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"<b>[Save System Tool]</b> Procesat finalizat!");
        Debug.Log($"<color=green>ID-uri noi generate: {assignedCount}</color>");
        Debug.Log($"<color=yellow>Obiecte sărite (aveau deja ID): {skippedCount}</color>");
    }

    [MenuItem("Tools/Save System/Clear All Unique IDs")]
    public static void ClearIDs()
    {
        if (EditorUtility.DisplayDialog("Atenție!", "Sigur vrei să ștergi TOATE ID-urile unice din scenă? Această acțiune va strica salvările vechi.", "Da", "Anulează"))
        {
            WorldEntityState[] allWorldItems = GameObject.FindObjectsOfType<WorldEntityState>();
            Undo.RecordObjects(allWorldItems, "Clear Unique IDs");

            foreach (WorldEntityState item in allWorldItems)
            {
                item.uniqueID = "";
                EditorUtility.SetDirty(item);
            }
            Debug.Log("<color=red>Toate ID-urile au fost șterse.</color>");
        }
    }
}

#endif