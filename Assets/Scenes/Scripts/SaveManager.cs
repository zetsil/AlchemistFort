using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;



// =========================================================
// 1. INVENTAR & PLAYER (Rămân neschimbate)
// =========================================================

[System.Serializable]
public class SlotSaveData
{
    public string itemName;      // ID-ul (numele din ScriptableObject)
    public int amount;           // Cantitatea
    public int slotIndex;        // Indexul slotului (-1 pt echipament, 0-x pt rucsac)
    public float durability;     // Durabilitate (dacă e cazul)

    // Constructor gol pentru JSON
    public SlotSaveData() { }

    public SlotSaveData(InventorySlot slot)
    {
        if (slot.itemData != null)
        {
            this.itemName = slot.itemData.itemName;
            this.amount = slot.count;
            this.slotIndex = slot.slotIndex;
            this.durability = (slot.state != null) ? slot.state.currentDurability : -1f;
        }
    }
}

[System.Serializable]
public class InventorySaveData
{
    public List<SlotSaveData> slots = new List<SlotSaveData>();
    public SlotSaveData equippedSlot;
}

[System.Serializable]
public class PlayerStatsSaveData
{
    public int health;
    public float stamina;
}

[System.Serializable]
public class PlayerPositionData
{
    public Vector3 pos;
    public float rotationYaw;
    public float cameraPitch;
}


// =========================================================
// 2. OBIECTE DIN LUME (Dropate / Inamici)
// =========================================================

[System.Serializable]
public class DroppedItemSaveData
{
    public string itemName;
    public Vector3 position;
    public Vector3 rotation;
    public float durability;

    public DroppedItemSaveData() { }

    public DroppedItemSaveData(WorldEntityState worldState)
    {
        // 1. Încercăm să luăm numele de pe ItemPickup (item mic)
        var pickup = worldState.GetComponent<ItemPickup>();
        if (pickup != null && pickup.itemData != null)
        {
            this.itemName = pickup.itemData.itemName;
        }
        else
        {
            // 2. Dacă nu e pickup, verificăm dacă e Entity (structură construită)
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

    public EnemySaveData() { }

    public EnemySaveData(ZombieNPC enemy)
    {
        if (enemy.entityData != null)
            this.enemyName = enemy.entityData.name;
        
        this.position = enemy.transform.position;
        this.rotation = enemy.transform.eulerAngles;
        this.currentHealth = enemy.CurrentHealth; 
    }
}

// =========================================================
// 3. PROGRES GLOBAL (Timp, Zi, Scena curentă)
// =========================================================

[System.Serializable]
public class GameProgressSaveData
{
    // Starea Ciclului Zi/Noapte
    public GameStateManager.GameState currentState;
    public float timeRemaining;

    // Starea Valurilor
    public int currentDayIndex;

    // Scena în care s-a dat Save
    public string currentSceneName;
}

// =========================================================
// 4. SISTEMUL NOU DE SCENE (IERARHIC)
// =========================================================

// Datele specifice UNEI SINGURE scene (ce ținem în Cache)
[System.Serializable]
public class SceneSaveData
{
    // Obiectele originale distruse în ACEASTĂ scenă (copaci, pietre)
    public List<string> destroyedOriginals = new List<string>();

    // Obiectele noi apărute în ACEASTĂ scenă (drop-uri, construcții)
    public List<DroppedItemSaveData> droppedItems = new List<DroppedItemSaveData>();

    // Inamicii activi în ACEASTĂ scenă
    public List<EnemySaveData> activeEnemies = new List<EnemySaveData>();
}

// Wrapper pentru a salva Dictionary-ul în JSON (JsonUtility nu știe Dictionary simplu)
[System.Serializable]
public class SceneSaveEntry
{
    public string sceneName;
    public SceneSaveData data;

    public SceneSaveEntry(string name, SceneSaveData data)
    {
        this.sceneName = name;
        this.data = data;
    }
}

// Structura finală a fișierului "world_items.json"
[System.Serializable]
public class WorldSaveData
{
    // 1. Progresul General (valabil pentru tot jocul)
    public GameProgressSaveData gameProgress = new GameProgressSaveData();

    // 2. Lista cu datele fiecărei scene vizitate (Forest, Cave, etc.)
    // Aceasta înlocuiește listele vechi "plate"
    public List<SceneSaveEntry> sceneDataList = new List<SceneSaveEntry>();
}

public class SaveManager : MonoBehaviour
{

    // Spawn temporar folosit DOAR la tranziții
    private bool hasPendingSpawn = false;
    private Vector3 pendingSpawnPosition;
    private float pendingSpawnYaw;

    [System.Serializable]
    private class PlayerRuntimeStats
    {
        public float health;
        public float stamina;
    }

    private PlayerRuntimeStats cachedPlayerStats;
    private bool hasCachedPlayerStats = false;
    
    public static SaveManager Instance { get; private set; }

    private string baseSavePath;
    public string currentSaveName = "Salvarea_1";
    
    // CACHE-UL: Aici ținem minte datele tuturor scenelor vizitate în sesiunea curentă
    // Key = Numele Scenei, Value = Datele (obiecte distruse, inamici, etc.)
    private Dictionary<string, SceneSaveData> runtimeSceneCache = new Dictionary<string, SceneSaveData>();

    // Lista locală pentru scena CURENTĂ (pentru acces rapid la distrugeri)
    public List<string> currentSceneDestroyedIds = new List<string>();

    private const string AUTOSAVE_FOLDER_NAME = "AutoSave_Transition";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            baseSavePath = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(baseSavePath)) Directory.CreateDirectory(baseSavePath);
        }
        else Destroy(gameObject);
    }

    private string GetCurrentSaveFolderPath()
    {
        string folderPath = Path.Combine(baseSavePath, currentSaveName);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    // ----------------------------------------------------------------
    // 1. LOGICA DE CACHE (MEMORIE)
    // ----------------------------------------------------------------

    /// <summary>
    /// Această funcție "fotografiază" starea scenei curente și o pune în RAM (Cache).
    /// Se apelează înainte de Save pe disc sau înainte de schimbarea scenei.
    /// </summary>
    public void CacheCurrentSceneState()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneSaveData dataToCache = new SceneSaveData();

        // A. Salvăm ID-urile distruse (din lista locală)
        dataToCache.destroyedOriginals = new List<string>(currentSceneDestroyedIds);

        // B. Salvăm obiectele dropate/construite
        WorldEntityState[] allStates = GameObject.FindObjectsOfType<WorldEntityState>();
        foreach (WorldEntityState state in allStates)
        {
            if (state.isSpawnedAtRuntime)
            {
                dataToCache.droppedItems.Add(new DroppedItemSaveData(state));
            }
        }

        // C. Salvăm Inamicii
        ZombieNPC[] enemies = Object.FindObjectsByType<ZombieNPC>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy.CurrentHealth > 0)
            {
                dataToCache.activeEnemies.Add(new EnemySaveData(enemy));
            }
        }

        // D. Introducem în Cache
        if (runtimeSceneCache.ContainsKey(currentSceneName))
        {
            runtimeSceneCache[currentSceneName] = dataToCache;
        }
        else
        {
            runtimeSceneCache.Add(currentSceneName, dataToCache);
        }

        Debug.Log($"<color=yellow>[Cache] Scena '{currentSceneName}' a fost salvată în memorie.</color>");
    }
    
    private void CachePlayerRuntimeStats()
    {
        if (FirstPersonController.Instance == null) return;

        PlayerStats stats = FirstPersonController.Instance.GetComponent<PlayerStats>();
        if (stats == null) return;

        cachedPlayerStats = new PlayerRuntimeStats
        {
            health = stats.currentHealth,   // din Entity
            stamina = stats.currentStamina
        };

        hasCachedPlayerStats = true;
    }
    
    private void ApplyCachedPlayerRuntimeStats(PlayerStats stats)
    {
        if (!hasCachedPlayerStats) return;

        stats.currentHealth = (int)cachedPlayerStats.health;
        stats.currentStamina = Mathf.Clamp(
            cachedPlayerStats.stamina,
            0f,
            stats.maxStamina
        );

        hasCachedPlayerStats = false;
    }



    /// <summary>
    /// Aceasta trebuie apelată CÂND PLECI din scenă (Tranziție).
    /// Doar actualizează memoria, nu scrie pe disc.
    /// </summary>
    public void HandleSceneTransition()
    {
        CacheCurrentSceneState();
        CachePlayerRuntimeStats();
    }
    
    public void SetPendingPlayerSpawn(Vector3 position, float yaw)
    {
        hasPendingSpawn = true;
        pendingSpawnPosition = position;
        pendingSpawnYaw = yaw;
    }

    private void ApplyPendingPlayerSpawn()
    {
        if (!hasPendingSpawn) return;
        if (FirstPersonController.Instance == null) return;

        var player = FirstPersonController.Instance;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        player.transform.position = pendingSpawnPosition;
        player.transform.localEulerAngles = new Vector3(0, pendingSpawnYaw, 0);
        player.playerCamera.transform.localEulerAngles = Vector3.zero;

        // sincronizăm yaw / pitch interne
        var fpcType = player.GetType();
        var yawField = fpcType.GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var pitchField = fpcType.GetField("pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (yawField != null) yawField.SetValue(player, pendingSpawnYaw);
        if (pitchField != null) pitchField.SetValue(player, 0f);

        hasPendingSpawn = false;
    }

    // ----------------------------------------------------------------
    // 2. OPERAȚII DISC (FULL SAVE / LOAD)
    // ----------------------------------------------------------------

    public void PerformFullSave()
    {
        string folderPath = GetCurrentSaveFolderPath();

        // 1. Mai întâi actualizăm Cache-ul cu ce vedem acum pe ecran
        CacheCurrentSceneState();

        // 2. Scriem fișierele
        CaptureAndSaveScreenshot(folderPath);
        SaveInventory();
        SavePlayerPosition(folderPath);

        // 3. Scriem World Data (care acum ia totul din Cache)
        SaveWorldItemStateToDisk(folderPath);
        SavePlayerStats(folderPath);

        Debug.Log($"<color=cyan>[SaveManager] Full Save Complete: {currentSaveName}</color>");
    }

    public void PerformFullLoad()
    {
        string folderPath = GetCurrentSaveFolderPath();
        if (!Directory.Exists(folderPath)) return;
        StartCoroutine(LoadSequence(folderPath));
    }

    private IEnumerator LoadSequence(string folderPath)
    {
        // 1. Citim fișierul world_items.json pentru a afla SCENA și a popula CACHE-ul
        string worldDataPath = Path.Combine(folderPath, "world_items.json");
        string sceneToLoad = "Forest"; 

        // Curățăm cache-ul vechi înainte de încărcare
        runtimeSceneCache.Clear();

        if (File.Exists(worldDataPath))
        {
            string json = File.ReadAllText(worldDataPath);
            WorldSaveData loadedData = JsonUtility.FromJson<WorldSaveData>(json);

            if (!string.IsNullOrEmpty(loadedData.gameProgress.currentSceneName))
                sceneToLoad = loadedData.gameProgress.currentSceneName;

            // RECONSTRUIM CACHE-UL DIN FIȘIER
            foreach (var entry in loadedData.sceneDataList)
            {
                if (!runtimeSceneCache.ContainsKey(entry.sceneName))
                {
                    runtimeSceneCache.Add(entry.sceneName, entry.data);
                }
            }

            // Setăm și stările globale (timp, zi)
            ApplyGlobalState(loadedData.gameProgress);
            LoadPlayerStats(folderPath);
        }

        // 2. Încărcăm scena
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        while (!asyncLoad.isDone) yield return null;
        yield return new WaitForEndOfFrame();

        // Așteptăm InventoryManager
        int attempts = 0;
        while (InventoryManager.Instance == null && attempts < 20)
        {
            attempts++;
            yield return new WaitForSeconds(0.05f);
        }

        // 3. Aplicăm datele specifice scenei curente din Cache
        ApplyCurrentSceneState();

        // 4. Inventar și Player
        LoadInventory();
        LoadPlayerPosition(folderPath);
        ApplyPendingPlayerSpawn();


        Debug.Log($"<color=green>[SaveManager] Load Complete!</color>");
    }
    
    private void OnEnable()
    {
        PlayerStats.OnPlayerStatsReady += OnPlayerStatsReady;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        PlayerStats.OnPlayerStatsReady -= OnPlayerStatsReady;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnPlayerStatsReady(PlayerStats stats)
    {
        ApplyCachedPlayerRuntimeStats(stats);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCurrentSceneState();
        ApplyPendingPlayerSpawn();
    }

    // ----------------------------------------------------------------
    // 3. DETALII IMPLEMENTARE (SAVE / LOAD SPECIFIC)
    // ----------------------------------------------------------------

    // Scrie tot Cache-ul într-un singur JSON
    public void SaveWorldItemStateToDisk(string folderPath)
    {
        WorldSaveData fullSave = new WorldSaveData();

        // A. Salvăm progresul global
        fullSave.gameProgress.currentSceneName = SceneManager.GetActiveScene().name;
        if (GameStateManager.Instance != null && WaveManager.Instance != null)
        {
            fullSave.gameProgress.currentState = GameStateManager.Instance.IsNight ?
                GameStateManager.GameState.Night : GameStateManager.GameState.Day;
            fullSave.gameProgress.timeRemaining = GameStateManager.Instance.timeRemaining;
            fullSave.gameProgress.currentDayIndex = WaveManager.Instance.GetCurrentDayIndex();
        }

        // B. Convertim Cache-ul (Dictionary) în Listă pentru JSON
        foreach (var kvp in runtimeSceneCache)
        {
            fullSave.sceneDataList.Add(new SceneSaveEntry(kvp.Key, kvp.Value));
        }

        string json = JsonUtility.ToJson(fullSave, true);
        File.WriteAllText(Path.Combine(folderPath, "world_items.json"), json);
    }

    // Aplică obiectele în scena curentă folosind datele din Cache
    public void ApplyCurrentSceneState()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // Resetăm lista locală
        currentSceneDestroyedIds.Clear();

        // Dacă nu avem date despre scena asta în cache, nu facem nimic (e prima vizită)
        if (!runtimeSceneCache.ContainsKey(currentSceneName))
        {
            Debug.Log($"[SaveManager] Nu există date în cache pentru {currentSceneName}. Se inițializează default.");
            return;
        }

        SceneSaveData localData = runtimeSceneCache[currentSceneName];

        // 1. Distrugeri
        currentSceneDestroyedIds = new List<string>(localData.destroyedOriginals);

        WorldEntityState[] sceneItems = GameObject.FindObjectsOfType<WorldEntityState>();
        foreach (WorldEntityState item in sceneItems)
        {
            // Dacă itemul din scenă are un ID care apare în lista de distruse, îl ștergem
            if (!string.IsNullOrEmpty(item.uniqueID) && currentSceneDestroyedIds.Contains(item.uniqueID))
            {
                Destroy(item.gameObject);
            }
        }

        // 2. Curățăm și Re-spawnăm Inamicii
        ZombieNPC[] existingEnemies = Object.FindObjectsByType<ZombieNPC>(FindObjectsSortMode.None);
        foreach (var e in existingEnemies) Destroy(e.gameObject);

        foreach (var enemyData in localData.activeEnemies)
        {
            SpawnEnemyFromSave(enemyData);
        }

        // 3. Re-spawnăm drop-urile/construcțiile
        foreach (DroppedItemSaveData dropData in localData.droppedItems)
        {
            SpawnDroppedItem(dropData);
        }

        if (WaveManager.Instance != null) WaveManager.Instance.RefreshActiveEnemies();
    }

    private void ApplyGlobalState(GameProgressSaveData progress)
    {
        if (GameStateManager.Instance != null && WaveManager.Instance != null)
        {
            GameStateManager.Instance.SetStateManually(progress.currentState, progress.timeRemaining);
            WaveManager.Instance.SetCurrentDay(progress.currentDayIndex);
            
            float totalDur = progress.currentState == GameStateManager.GameState.Day ? 300f : 180f;
            GlobalEvents.NotifyTimeUpdate(progress.timeRemaining / totalDur);
        }
    }

    // ----------------------------------------------------------------
    // HELPERS (Rămân la fel ca înainte)
    // ----------------------------------------------------------------

    public void RegisterDestroyedWorldItem(string id)
    {
        // Adăugăm doar în lista locală a scenei curente
        if (!string.IsNullOrEmpty(id) && !currentSceneDestroyedIds.Contains(id))
        {
            currentSceneDestroyedIds.Add(id);
        }
    }

    // ... (SpawnEnemyFromSave, SpawnDroppedItem, SaveInventory, etc. rămân la fel ca în codul tău vechi)
    
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
                if (entityComp != null) entityComp.currentHealth = (int)data.currentHealth;
            }
        }
    }

    private void SpawnDroppedItem(DroppedItemSaveData data)
    {
        GameObject prefab = null;
        Item itemSO = ItemVisualManager.Instance.GetItemDataByName(data.itemName);
        if (itemSO != null) prefab = ItemVisualManager.Instance.GetItemVisualPrefab(itemSO);
        else 
        {
            EntityData entitySO = ItemVisualManager.Instance.GetEntityDataByName(data.itemName);
            if (entitySO != null) prefab = ItemVisualManager.Instance.GetEntityVisualPrefab(entitySO);
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
            if (entityComp != null) entityComp.currentHealth = (int)data.durability;
        }
    }

    // ... (Inventory Methods - copy paste din codul tău vechi, nu necesită schimbări)
    public void SaveInventory() 
    {
        InventorySaveData saveData = new InventorySaveData();
        foreach (InventorySlot slot in InventoryManager.Instance.allSlots) {
            if (slot.itemData != null && slot.count > 0) saveData.slots.Add(new SlotSaveData(slot));
        }
        InventorySlot equipped = EquippedManager.Instance.GetEquippedSlot();
        if (equipped != null && equipped.itemData != null) saveData.equippedSlot = new SlotSaveData(equipped);
        
        File.WriteAllText(Path.Combine(GetCurrentSaveFolderPath(), "inventory.json"), JsonUtility.ToJson(saveData, true));
    }

    public void LoadInventory()
    {
        if (InventoryManager.Instance == null) return;
        string filePath = Path.Combine(GetCurrentSaveFolderPath(), "inventory.json");
        if (!File.Exists(filePath)) return;
        InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(File.ReadAllText(filePath));
        InventoryManager.Instance.ClearInventory();
        foreach (SlotSaveData data in saveData.slots) {
            Item itemSO = FindItemInVisualManager(data.itemName);
            if (itemSO != null) {
                InventorySlot newSlot = new InventorySlot(itemSO, data.slotIndex);
                newSlot.count = data.amount;
                if (newSlot.state != null && data.durability != -1f) newSlot.state.currentDurability = data.durability;
                InventoryManager.Instance.AddExistingSlot(newSlot);
            }
        }
        if (saveData.equippedSlot != null && !string.IsNullOrEmpty(saveData.equippedSlot.itemName)) {
            Item equippedSO = FindItemInVisualManager(saveData.equippedSlot.itemName);
            if (equippedSO != null) {
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
        if (FirstPersonController.Instance != null) {
            Rigidbody rb = FirstPersonController.Instance.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;
            FirstPersonController.Instance.transform.position = data.pos;
            FirstPersonController.Instance.transform.localEulerAngles = new Vector3(0, data.rotationYaw, 0);
            FirstPersonController.Instance.playerCamera.transform.localEulerAngles = new Vector3(data.cameraPitch, 0, 0);
            
            // Reflection pentru pitch/yaw private fields
            var fpcType = FirstPersonController.Instance.GetType();
            var yawField = fpcType.GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pitchField = fpcType.GetField("pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (yawField != null) yawField.SetValue(FirstPersonController.Instance, data.rotationYaw);
            if (pitchField != null) pitchField.SetValue(FirstPersonController.Instance, data.cameraPitch);
        }
    }


    public void SavePlayerStats(string folderPath)
    {
        if (FirstPersonController.Instance == null) return;

        PlayerStats stats = FirstPersonController.Instance.GetComponent<PlayerStats>();
        if (stats == null) return;

        PlayerStatsSaveData data = new PlayerStatsSaveData
        {
            health = stats.currentHealth,
            stamina = stats.currentStamina
        };

        File.WriteAllText(
            Path.Combine(folderPath, "player_stats.json"),
            JsonUtility.ToJson(data, true)
        );
    }


    private void LoadPlayerStats(string folderPath)
    {
        string path = Path.Combine(folderPath, "player_stats.json");
        if (!File.Exists(path)) return;

        cachedPlayerStats = JsonUtility.FromJson<PlayerRuntimeStats>(
            File.ReadAllText(path)
        );

        hasCachedPlayerStats = true;
    }


    // Aceasta este funcția care lipsea:
    public void UnregisterDestroyedWorldItem(string id)
    {
        // Dacă un obiect a "reînviat" sau a fost respawnat, îl scoatem din lista de distruse
        // astfel încât să apară din nou la următorul Load.
        if (!string.IsNullOrEmpty(id) && currentSceneDestroyedIds.Contains(id))
        {
            currentSceneDestroyedIds.Remove(id);
        }
    }

    public void CaptureAndSaveScreenshot(string folderPath) { ScreenCapture.CaptureScreenshot(Path.Combine(folderPath, "screenshot.png")); }
    private Item FindItemInVisualManager(string itemName) { return ItemVisualManager.Instance != null ? ItemVisualManager.Instance.GetItemDataByName(itemName) : null; }
}