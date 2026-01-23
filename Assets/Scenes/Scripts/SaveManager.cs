using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

// =========================================================
// STRUCTURI DE DATE PENTRU SALVARE (SERIALIZABLE)
// =========================================================

[System.Serializable]
public class SlotSaveData
{
    public string itemName;      // Identificator pentru ScriptableObject
    public int amount;           // Cantitate
    public int slotIndex;        // Poziția în inventar
    public float durability;     // Durabilitatea (dacă este unealtă)
    

    public SlotSaveData(InventorySlot slot)
    {
        this.itemName = slot.itemData.itemName;
        this.amount = slot.count;
        this.slotIndex = slot.slotIndex;
        this.durability = (slot.state != null) ? slot.state.currentDurability : -1f;
    }
}

[System.Serializable]
public class DroppedItemSaveData
{
    public string itemName;
    public Vector3 position;
    public Vector3 rotation;
    public float durability;

    public DroppedItemSaveData(WorldEntityState worldState)
    {
        // Încercăm să luăm numele de pe ItemPickup
        var pickup = worldState.GetComponent<ItemPickup>();
        if (pickup != null && pickup.itemData != null)
        {
            this.itemName = pickup.itemData.itemName;
        }
        else
        {
            // Dacă nu e pickup, verificăm dacă e o Entity (pentru structuri construite)
            var entity = worldState.GetComponent<Entity>();
            if (entity != null && entity.entityData != null)
            {
                this.itemName = entity.entityData.name;
            }
        }

        this.position = worldState.transform.position;
        this.rotation = worldState.transform.eulerAngles;
        this.durability = worldState.currentHealthOrDurability;
    }
}

[System.Serializable]
public class EnemySaveData
{
    public string enemyName; // Numele din EntityData (ex: "Zombie_Basic")
    public Vector3 position;
    public Vector3 rotation;
    public float currentHealth;

    public EnemySaveData(ZombieNPC enemy)
    {
        if (enemy.entityData != null)
            this.enemyName = enemy.entityData.name;
        
        this.position = enemy.transform.position;
        this.rotation = enemy.transform.eulerAngles;
        
        // Presupunem că ZombieNPC moștenește din Entity sau are acces la viață
        this.currentHealth = enemy.CurrentHealth; 
    }
}

[System.Serializable]
public class GameProgressSaveData
{
    // Starea Ciclului Zi/Noapte
    public GameStateManager.GameState currentState;
    public float timeRemaining;

    // Starea Valurilor
    public int currentDayIndex;

    // NOU: Numele scenei curente
    public string currentSceneName;
}

[System.Serializable]
public class PlayerPositionData
{
    public Vector3 pos;
    public float rotationYaw;
    public float cameraPitch;
}

[System.Serializable]
public class InventorySaveData
{
    public List<SlotSaveData> slots = new List<SlotSaveData>();
    public SlotSaveData equippedSlot;
}

[System.Serializable]
public class WorldSaveData
{
    // Lista 1: ID-urile obiectelor originale distruse (copaci tăiați, pietre sparte, etc.)
    public List<string> destroyedOriginals = new List<string>();

    // Lista 2: Obiectele dropate pe jos sau construite de jucător
    public List<DroppedItemSaveData> newDroppedItems = new List<DroppedItemSaveData>();

    // Lista 3: Inamicii activi în momentul salvării
    public List<EnemySaveData> activeEnemies = new List<EnemySaveData>();

    // Lista 4: Progresul global (Timp, Zi, Scenă)
    public GameProgressSaveData gameProgress = new GameProgressSaveData();
    
}

// =========================================================
// CLASA PRINCIPALĂ SAVE MANAGER
// =========================================================

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string baseSavePath; 
    public string currentSaveName = "Salvarea_1"; 
    public List<string> destroyedOriginals = new List<string>();
    private const string AUTOSAVE_FOLDER_NAME = "AutoSave_Transition";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            baseSavePath = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(baseSavePath))
            {
                Directory.CreateDirectory(baseSavePath);
            }
        }
        else Destroy(gameObject);
    }

    private string GetCurrentSaveFolderPath()
    {
        string folderPath = Path.Combine(baseSavePath, currentSaveName);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }
        return folderPath;
    }

    // ----------------------------------------------------------------
    // OPERAȚII PRINCIPALE (FULL SAVE / FULL LOAD)
    // ----------------------------------------------------------------

    public void PerformFullSave()
    {
        string folderPath = GetCurrentSaveFolderPath();

        // --- LOGICA DE OVERWRITE/MERGE CU AUTOSAVE ---
        if (HasAutoSave())
        {
            string autoSavePath = Path.Combine(baseSavePath, AUTOSAVE_FOLDER_NAME, "world_items.json");
            try 
            {
                string autoJson = File.ReadAllText(autoSavePath);
                WorldSaveData autoData = JsonUtility.FromJson<WorldSaveData>(autoJson);

                // Combinăm listele de obiecte distruse din alte scene (fără duplicate)
                foreach (string id in autoData.destroyedOriginals)
                {
                    if (!destroyedOriginals.Contains(id))
                    {
                        destroyedOriginals.Add(id);
                    }
                }
                Debug.Log("<color=cyan>[SaveManager] Progresul din AutoSave a fost integrat în salvarea manuală.</color>");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[SaveManager] Nu s-a putut citi AutoSave-ul pentru merge: " + e.Message);
            }
        }
        // ----------------------------------------------

        CaptureAndSaveScreenshot(folderPath);
        SaveInventory();
        SavePlayerPosition(folderPath);
        SaveWorldItemState(folderPath); 

        Debug.Log($"<color=cyan>[SaveManager] Full Save Complete (with Overwrite): {currentSaveName}</color>");
    }

    public void PerformFullLoad()
    {
        string folderPath = GetCurrentSaveFolderPath();

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"[SaveManager] Folderul de salvare nu există: {folderPath}");
            return;
        }

        StartCoroutine(LoadSequence(folderPath));
    }
    
    public void PerformAutoSave(string sceneName)
    {
        string previousSaveName = currentSaveName;
        currentSaveName = GetAutoSaveFolder(sceneName);

        string folderPath = GetCurrentSaveFolderPath();

        SaveInventory();
        SavePlayerPosition(folderPath);
        SaveWorldItemState(folderPath);

        currentSaveName = previousSaveName;

        Debug.Log($"<color=cyan>[AutoSave] Saved scene: {sceneName}</color>");
    }


// Funcție pentru a încărca din AutoSave (la intrarea într-o scenă nouă)
    public bool HasAutoSaveForScene(string sceneName)
    {
        string path = Path.Combine(
            baseSavePath,
            GetAutoSaveFolder(sceneName),
            "world_items.json"
        );
        return File.Exists(path);
    }


    public bool HasAutoSave()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        return HasAutoSaveForScene(currentScene);
    }

    

    public void LoadAutoSaveForScene(string sceneName)
    {
        if (!HasAutoSaveForScene(sceneName)) return;

        string previousSaveName = currentSaveName;
        currentSaveName = GetAutoSaveFolder(sceneName);

        string folderPath = GetCurrentSaveFolderPath();
        StartCoroutine(LoadSequence(folderPath));

        currentSaveName = previousSaveName;

        Debug.Log($"<color=green>[AutoSave] Loaded scene: {sceneName}</color>");
    }

    private IEnumerator LoadSequence(string folderPath)
    {
        Debug.Log("<color=orange>Loading Scene...</color>");

        Time.timeScale = 1f;
        destroyedOriginals.Clear();

        // 1. Determinăm ce scenă trebuie încărcată din fișierul de salvare
        string sceneToLoad = "Forest"; // Fallback default
        string worldDataPath = Path.Combine(folderPath, "world_items.json");

        if (File.Exists(worldDataPath))
        {
            try
            {
                string json = File.ReadAllText(worldDataPath);
                WorldSaveData tempData = JsonUtility.FromJson<WorldSaveData>(json);
                if (!string.IsNullOrEmpty(tempData.gameProgress.currentSceneName))
                {
                    sceneToLoad = tempData.gameProgress.currentSceneName;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Eroare la citirea scenei din save: {e.Message}. Se folosește fallback 'Forest'.");
            }
        }

        Debug.Log($"<color=orange>Se încarcă scena: {sceneToLoad}</color>");

        // 2. Încărcăm scena specificată în salvare
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        // Așteptăm ca managerii să se inițializeze
        int attempts = 0;
        while (InventoryManager.Instance == null && attempts < 20)
        {
            attempts++;
            yield return new WaitForSeconds(0.05f);
        }

        Debug.Log("<color=orange>Applying Saved Data...</color>");

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.isRestoringFromSave = true;
        }

        LoadInventory();
        LoadPlayerPosition(folderPath);
        LoadWorldItemState(folderPath); // Aici se încarcă inamicii și timpul

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (FirstPersonController.Instance != null)
        {
            FirstPersonController.Instance.cameraCanMove = true;
        }

        Debug.Log($"<color=green>[SaveManager] Load Complete!</color>");
    }

    // ----------------------------------------------------------------
    // SALVARE/ÎNCĂRCARE LUME (Obiecte, Inamici, Timp)
    // ----------------------------------------------------------------

    public void SaveWorldItemState(string folderPath)
    {
        WorldSaveData worldData = new WorldSaveData();

        // 1. Salvăm lista de obiecte distruse
        worldData.destroyedOriginals = this.destroyedOriginals;

        // 2. Salvăm Obiectele Dropate / Construite (Whitelist)
        WorldEntityState[] allStates = GameObject.FindObjectsOfType<WorldEntityState>();
        foreach (WorldEntityState state in allStates)
        {
            if (state.isSpawnedAtRuntime)
            {
                worldData.newDroppedItems.Add(new DroppedItemSaveData(state));
            }
        }

        // 3. Salvăm Inamicii Activi
        ZombieNPC[] enemies = Object.FindObjectsByType<ZombieNPC>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.CurrentHealth > 0)
            {
                worldData.activeEnemies.Add(new EnemySaveData(enemy));
            }
        }

        // 4. Salvăm Progresul Jocului (Timp, Val și SCENA)
        // Salvăm întotdeauna numele scenei curente
        worldData.gameProgress.currentSceneName = SceneManager.GetActiveScene().name;

        if (GameStateManager.Instance != null && WaveManager.Instance != null)
        {
            worldData.gameProgress.currentState = GameStateManager.Instance.IsNight ? 
                GameStateManager.GameState.Night : GameStateManager.GameState.Day;
            worldData.gameProgress.timeRemaining = GameStateManager.Instance.timeRemaining;
            worldData.gameProgress.currentDayIndex = WaveManager.Instance.GetCurrentDayIndex();
        }

        // Scrierea pe disc
        string filePath = Path.Combine(folderPath, "world_items.json");
        string json = JsonUtility.ToJson(worldData, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"<color=white>Starea lumii (Scenă, Obiecte, Inamici, Timp) a fost salvată.</color>");
    }

    public void LoadWorldItemState(string folderPath)
    {
        string filePath = Path.Combine(folderPath, "world_items.json");
        if (!File.Exists(filePath))
        {
            destroyedOriginals.Clear();
            return;
        }

        string json = File.ReadAllText(filePath);
        WorldSaveData worldData = JsonUtility.FromJson<WorldSaveData>(json);

        if (GameStateManager.Instance != null && WaveManager.Instance != null)
        {
            // 1. Întâi setăm timpul (ca restul sistemelor să știe unde suntem în ciclu)
            GameStateManager.Instance.SetStateManually(worldData.gameProgress.currentState, worldData.gameProgress.timeRemaining);
            
            // 2. Apoi setăm ziua (SetCurrentDay va vedea timpul de mai sus și va bloca dublurile)
            WaveManager.Instance.SetCurrentDay(worldData.gameProgress.currentDayIndex); 

            // 3. Notificăm UI-ul
            float totalDur = worldData.gameProgress.currentState == GameStateManager.GameState.Day ? 300f : 180f;
            GlobalEvents.NotifyTimeUpdate(worldData.gameProgress.timeRemaining / totalDur); 
        }

        // B. Curățăm Inamicii existenți
        ZombieNPC[] existingEnemies = Object.FindObjectsByType<ZombieNPC>(FindObjectsSortMode.None);
        foreach (var e in existingEnemies) Destroy(e.gameObject);

        // C. Re-spawnăm Inamicii
        foreach (var enemyData in worldData.activeEnemies)
        {
            SpawnEnemyFromSave(enemyData);
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.RefreshActiveEnemies();
        }

        // D. Procesăm lista de obiecte distruse
        this.destroyedOriginals = worldData.destroyedOriginals;
        WorldEntityState[] sceneItems = GameObject.FindObjectsOfType<WorldEntityState>();
        foreach (WorldEntityState item in sceneItems)
        {
            if (destroyedOriginals.Contains(item.uniqueID))
            {
                Destroy(item.gameObject);
            }
        }

        // E. Re-spawnăm obiectele dropate/construite
        foreach (DroppedItemSaveData dropData in worldData.newDroppedItems)
        {
            SpawnDroppedItem(dropData);
        }
    }

    // ----------------------------------------------------------------
    // HELPERS PENTRU SPAWN
    // ----------------------------------------------------------------

    private void SpawnEnemyFromSave(EnemySaveData data)
    {
        EntityData enemySO = ItemVisualManager.Instance.GetEntityDataByName(data.enemyName);
        if (enemySO != null)
        {
            GameObject prefab = ItemVisualManager.Instance.GetEntityVisualPrefab(enemySO);
            if (prefab != null)
            {
                GameObject spawned = Instantiate(prefab, data.position, Quaternion.Euler(data.rotation));
                
                Entity entityComp = spawned.GetComponent<Entity>();
                if (entityComp != null)
                {
                    entityComp.currentHealth = (int)data.currentHealth;
                }
            }
        }
    }

    private void SpawnDroppedItem(DroppedItemSaveData data)
    {
        GameObject prefab = null;

        Item itemSO = ItemVisualManager.Instance.GetItemDataByName(data.itemName);
        if (itemSO != null)
        {
            prefab = ItemVisualManager.Instance.GetItemVisualPrefab(itemSO);
        }
        else
        {
            EntityData entitySO = ItemVisualManager.Instance.GetEntityDataByName(data.itemName);
            if (entitySO != null)
            {
                prefab = ItemVisualManager.Instance.GetEntityVisualPrefab(entitySO);
            }
        }

        if (prefab != null)
        {
            GameObject spawned = Instantiate(prefab, data.position, Quaternion.Euler(data.rotation));
            WorldEntityState state = spawned.GetComponent<WorldEntityState>();
            if (state != null)
            {
                state.isSpawnedAtRuntime = true;
                state.currentHealthOrDurability = data.durability;
            }
            
            Entity entityComp = spawned.GetComponent<Entity>();
            if (entityComp != null)
            {
                entityComp.currentHealth = (int)data.durability;
            }
        }
    }

    // ----------------------------------------------------------------
    // INVENTAR & PLAYER
    // ----------------------------------------------------------------

    public void SaveInventory()
    {
        InventorySaveData saveData = new InventorySaveData();

        foreach (InventorySlot slot in InventoryManager.Instance.allSlots)
        {
            if (slot.itemData != null && slot.count > 0)
            {
                saveData.slots.Add(new SlotSaveData(slot));
            }
        }

        InventorySlot equipped = EquippedManager.Instance.GetEquippedSlot();
        if (equipped != null && equipped.itemData != null)
        {
            saveData.equippedSlot = new SlotSaveData(equipped);
        }

        string filePath = Path.Combine(GetCurrentSaveFolderPath(), "inventory.json");
        File.WriteAllText(filePath, JsonUtility.ToJson(saveData, true));
    }

    private string GetAutoSaveFolder(string sceneName)
    {
        return $"AutoSave_{sceneName}";
    }


    public void LoadInventory()
    {
        if (InventoryManager.Instance == null) return;

        string filePath = Path.Combine(GetCurrentSaveFolderPath(), "inventory.json");
        if (!File.Exists(filePath)) return;

        InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(File.ReadAllText(filePath));

        InventoryManager.Instance.ClearInventory();

        foreach (SlotSaveData data in saveData.slots)
        {
            Item itemSO = FindItemInVisualManager(data.itemName);
            if (itemSO != null)
            {
                InventorySlot newSlot = new InventorySlot(itemSO, data.slotIndex);
                newSlot.count = data.amount;
                if (newSlot.state != null && data.durability != -1f)
                    newSlot.state.currentDurability = data.durability;

                InventoryManager.Instance.AddExistingSlot(newSlot);
            }
        }

        if (saveData.equippedSlot != null && !string.IsNullOrEmpty(saveData.equippedSlot.itemName))
        {
            Item equippedSO = FindItemInVisualManager(saveData.equippedSlot.itemName);
            if (equippedSO != null)
            {
                InventorySlot equippedSlot = new InventorySlot(equippedSO, -1);
                equippedSlot.count = saveData.equippedSlot.amount;
                if (equippedSlot.state != null && saveData.equippedSlot.durability != -1f)
                    equippedSlot.state.currentDurability = saveData.equippedSlot.durability;

                GlobalEvents.RequestSlotEquip(equippedSlot);
            }
        }
    }

    public void SavePlayerPosition(string folderPath)
    {
        if (FirstPersonController.Instance == null) return;

        PlayerPositionData data = new PlayerPositionData();
        data.pos = FirstPersonController.Instance.transform.position;
        data.rotationYaw = FirstPersonController.Instance.transform.localEulerAngles.y;
        data.cameraPitch = FirstPersonController.Instance.playerCamera.transform.localEulerAngles.x;

        File.WriteAllText(Path.Combine(folderPath, "player_pos.json"), JsonUtility.ToJson(data, true));
    }

    public void LoadPlayerPosition(string folderPath)
    {
        string filePath = Path.Combine(folderPath, "player_pos.json");
        if (!File.Exists(filePath)) return;

        PlayerPositionData data = JsonUtility.FromJson<PlayerPositionData>(File.ReadAllText(filePath));

        if (FirstPersonController.Instance != null)
        {
            Rigidbody rb = FirstPersonController.Instance.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            FirstPersonController.Instance.transform.position = data.pos;
            FirstPersonController.Instance.transform.localEulerAngles = new Vector3(0, data.rotationYaw, 0);
            FirstPersonController.Instance.playerCamera.transform.localEulerAngles = new Vector3(data.cameraPitch, 0, 0);

            var fpcType = FirstPersonController.Instance.GetType();
            var yawField = fpcType.GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pitchField = fpcType.GetField("pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (yawField != null) yawField.SetValue(FirstPersonController.Instance, data.rotationYaw);
            if (pitchField != null) pitchField.SetValue(FirstPersonController.Instance, data.cameraPitch);
        }
    }

    // ----------------------------------------------------------------
    // UTILITARE
    // ----------------------------------------------------------------

    public void CaptureAndSaveScreenshot(string folderPath)
    {
        ScreenCapture.CaptureScreenshot(Path.Combine(folderPath, "screenshot.png"));
    }

    public void RegisterDestroyedWorldItem(string id)
    {
        if (!string.IsNullOrEmpty(id) && !destroyedOriginals.Contains(id))
        {
            destroyedOriginals.Add(id);
        }
    }

    public void UnregisterDestroyedWorldItem(string id)
    {
        if (destroyedOriginals.Contains(id))
        {
            destroyedOriginals.Remove(id);
        }
    }

    private Item FindItemInVisualManager(string itemName)
    {
        if (ItemVisualManager.Instance == null) return null;
        return ItemVisualManager.Instance.GetItemDataByName(itemName);
    }
}