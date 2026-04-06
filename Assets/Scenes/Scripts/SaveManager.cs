using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;

// =========================================================
// 1. INVENTAR & PLAYER
// =========================================================

[System.Serializable]
public class SlotSaveData
{
    public string itemName;
    public int amount;
    public int slotIndex;
    public float durability;

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
public class QuickSlotSaveData
{
    public int index;
    public string targetItemName;

    public QuickSlotSaveData() { }

    public QuickSlotSaveData(QuickSlot qs)
    {
        this.index = qs.hotbarIndex;
        this.targetItemName = qs.targetItemName;
    }
}

[System.Serializable]
public class HotbarSaveData
{
    public List<QuickSlotSaveData> quickSlots = new List<QuickSlotSaveData>();
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
// 2. SISTEM UNIFICAT DE ENTITĂȚI (Inamici, Copaci, Drop-uri)
// =========================================================

[System.Serializable]
public class EntitySaveData
{
    public string entityName;      // Numele din EntityData sau ItemData
    public string uniqueID;        // ID-ul (pentru obiectele statice din scenă)
    public Vector3 position;
    public Vector3 rotation;
    public float currentHealth;
    public bool isSpawnedAtRuntime;

    public EntitySaveData() { }

    public EntitySaveData(Entity entity)
    {
        // 1. Numele entității
        var pickup = entity.GetComponent<ItemPickup>();
        if (pickup != null && pickup.itemData != null)
            this.entityName = pickup.itemData.itemName;
        else if (entity.entityData != null)
            this.entityName = entity.entityData.name;

        // 2. ID-ul și Starea
        var worldState = entity.GetComponent<WorldEntityState>();
        if (worldState != null)
        {
            this.uniqueID = worldState.uniqueID;
            this.isSpawnedAtRuntime = worldState.isSpawnedAtRuntime;
        }
        else
        {
            // Dacă entitatea nu are WorldEntityState (ex: Zombie simplu), o considerăm de runtime
            this.isSpawnedAtRuntime = true;
        }

        // 3. Poziție și Viață
        this.position = entity.transform.position;
        this.rotation = entity.transform.eulerAngles;
        this.currentHealth = entity.currentHealth;
    }
}

// =========================================================
// 3. PROGRES GLOBAL (Timp, Zi, Scena curentă)
// =========================================================

[System.Serializable]
public class GameProgressSaveData
{
    public GameStateManager.GameState currentState;
    public float timeRemaining;
    public int currentDayIndex;
    public string currentSceneName;
}

// =========================================================
// 4. SISTEMUL DE SCENE (IERARHIC)
// =========================================================

[System.Serializable]
public class SceneSaveData
{
    // Obiectele originale distruse în ACEASTĂ scenă (copaci morți, pietre sparte)
    public List<string> destroyedOriginals = new List<string>();

    // TOATE entitățile care sunt în viață (Statice lovite, Drop-uri, Zombii)
    public List<EntitySaveData> activeEntities = new List<EntitySaveData>();
}

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

[System.Serializable]
public class WorldSaveData
{
    public GameProgressSaveData gameProgress = new GameProgressSaveData();
    public List<SceneSaveEntry> sceneDataList = new List<SceneSaveEntry>();
}

public class SaveManager : MonoBehaviour
{
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
    
    private Dictionary<string, SceneSaveData> runtimeSceneCache = new Dictionary<string, SceneSaveData>();
    public List<string> currentSceneDestroyedIds = new List<string>();

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

    public void CacheCurrentSceneState()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneSaveData dataToCache = new SceneSaveData();

        dataToCache.destroyedOriginals = new List<string>(currentSceneDestroyedIds);

        // Găsim TOATE obiectele care au WorldEntityState (Inamici, Copaci, DAR ȘI DROP-URI)
        WorldEntityState[] allWorldStates = Object.FindObjectsByType<WorldEntityState>(FindObjectsSortMode.None);
        
        foreach (WorldEntityState state in allWorldStates)
        {
            // 1. Excludem Player-ul sau obiectele din mâna lui
            if (state.CompareTag("Player") || state.transform.root.CompareTag("Player")) continue;

            // 2. Încercăm să obținem componenta Entity (dacă există, ex: la Zombii)
            Entity entityComp = state.GetComponent<Entity>();
            
            // 3. Verificăm dacă este "viu" (pentru entități cu viață)
            if (entityComp != null && entityComp.currentHealth <= 0) continue;
            
            // 4. Dacă e un drop simplu, verificăm durabilitatea/viața din WorldEntityState
            if (state.currentHealthOrDurability <= 0 && state.isSpawnedAtRuntime) continue;

            // --- CREARE DATE SALVARE ---
            EntitySaveData data = new EntitySaveData();
            
            // Identificare nume (prioritate ItemPickup pentru Drop-uri)
            var pickup = state.GetComponent<ItemPickup>();
            if (pickup != null && pickup.itemData != null)
                data.entityName = pickup.itemData.itemName;
            else if (entityComp != null && entityComp.entityData != null)
                data.entityName = entityComp.entityData.name;
            else
                data.entityName = state.gameObject.name;

            data.uniqueID = state.uniqueID;
            data.position = state.transform.position;
            data.rotation = state.transform.eulerAngles;
            data.currentHealth = state.currentHealthOrDurability; // Salvăm viața sau durabilitatea
            data.isSpawnedAtRuntime = state.isSpawnedAtRuntime;

            dataToCache.activeEntities.Add(data);
            
            Debug.Log($"<color=cyan>[Cache] Salvat: {data.entityName} | Runtime: {data.isSpawnedAtRuntime} | Pos: {data.position}</color>");
        }

        if (runtimeSceneCache.ContainsKey(currentSceneName))
            runtimeSceneCache[currentSceneName] = dataToCache;
        else
            runtimeSceneCache.Add(currentSceneName, dataToCache);

        Debug.Log($"<color=yellow>[Cache] Scena '{currentSceneName}' salvată. Total obiecte: {dataToCache.activeEntities.Count}</color>");
    }
    
    private void CachePlayerRuntimeStats()
    {
        if (FirstPersonController.Instance == null) return;
        PlayerStats stats = FirstPersonController.Instance.GetComponent<PlayerStats>();
        if (stats == null) return;

        cachedPlayerStats = new PlayerRuntimeStats
        {
            health = stats.currentHealth,
            stamina = stats.currentStamina
        };
        hasCachedPlayerStats = true;
    }
    
    private void ApplyCachedPlayerRuntimeStats(PlayerStats stats)
    {
        if (!hasCachedPlayerStats) return;
        stats.currentHealth = (int)cachedPlayerStats.health;
        stats.currentStamina = Mathf.Clamp(cachedPlayerStats.stamina, 0f, stats.maxStamina);
        hasCachedPlayerStats = false;
    }

    public void ClearRuntimeCache()
    {
        runtimeSceneCache.Clear();
        currentSceneDestroyedIds.Clear();
        hasCachedPlayerStats = false;
        cachedPlayerStats = null;

        if (GameStateManager.Instance != null) GameStateManager.Instance.isRestoringFromSave = false;
        if (InventoryManager.Instance != null) InventoryManager.Instance.ClearInventory();
        if (EquippedManager.Instance != null) EquippedManager.Instance.ClearEquippedSlot();

        hasPendingSpawn = false;
        Debug.Log("<color=orange>[SaveManager] Cache-ul complet a fost golit!</color>");
    }

    public void HandleSceneTransition()
    {
        CacheCurrentSceneState();
        CachePlayerRuntimeStats();
    }

    public void SaveHotbar(string folderPath)
    {
        if (QuickSlotManager.Instance == null) return;

        HotbarSaveData saveData = new HotbarSaveData();

        // Parcurgem toate sloturile din Hotbar și le salvăm datele
        foreach (var qs in QuickSlotManager.Instance.quickSlots)
        {
            saveData.quickSlots.Add(new QuickSlotSaveData(qs));
        }

        string filePath = Path.Combine(folderPath, "hotbar.json");
        File.WriteAllText(filePath, JsonUtility.ToJson(saveData, true));
        
        Debug.Log($"<color=cyan>[SaveManager] Hotbar salvat.</color>");
    }

    public void LoadHotbar(string folderPath)
    {
        if (QuickSlotManager.Instance == null) return;

        string filePath = Path.Combine(folderPath, "hotbar.json");
        if (!File.Exists(filePath))
        {
            // Dacă nu există fișierul, curățăm Hotbar-ul existent pentru a preveni "fantome" de la sesiuni anterioare
            foreach(var qs in QuickSlotManager.Instance.quickSlots)
            {
                qs.Unassign();
            }
            return;
        }

        HotbarSaveData saveData = JsonUtility.FromJson<HotbarSaveData>(File.ReadAllText(filePath));

        // Asignăm noile valori
        foreach (var savedSlot in saveData.quickSlots)
        {
            if (string.IsNullOrEmpty(savedSlot.targetItemName))
            {
                QuickSlotManager.Instance.quickSlots[savedSlot.index].Unassign();
            }
            else
            {
                QuickSlotManager.Instance.AssignToHotbar(savedSlot.targetItemName, savedSlot.index);
            }
        }
        
        Debug.Log($"<color=green>[SaveManager] Hotbar încărcat cu succes!</color>");
    }
    
    public void SetPendingPlayerSpawn(Vector3 position, float yaw)
    {
        hasPendingSpawn = true;
        pendingSpawnPosition = position;
        pendingSpawnYaw = yaw;
    }

    private void ApplyPendingPlayerSpawn()
    {
        if (!hasPendingSpawn || FirstPersonController.Instance == null) return;

        var player = FirstPersonController.Instance;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        player.transform.position = pendingSpawnPosition;
        player.transform.localEulerAngles = new Vector3(0, pendingSpawnYaw, 0);
        player.playerCamera.transform.localEulerAngles = Vector3.zero;

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
        CacheCurrentSceneState();
        CaptureAndSaveScreenshot(folderPath);
        SaveInventory();
        SaveHotbar(folderPath);
        SavePlayerPosition(folderPath);
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
        string worldDataPath = Path.Combine(folderPath, "world_items.json");
        string sceneToLoad = "Forest"; 
        WorldSaveData loadedData = null;

        runtimeSceneCache.Clear();

        if (File.Exists(worldDataPath))
        {
            loadedData = JsonUtility.FromJson<WorldSaveData>(File.ReadAllText(worldDataPath));
            if (!string.IsNullOrEmpty(loadedData.gameProgress.currentSceneName))
                sceneToLoad = loadedData.gameProgress.currentSceneName;

            foreach (var entry in loadedData.sceneDataList)
            {
                if (!runtimeSceneCache.ContainsKey(entry.sceneName))
                    runtimeSceneCache.Add(entry.sceneName, entry.data);
            }
            LoadPlayerStats(folderPath);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneToLoad);
        while (!asyncLoad.isDone) yield return null;
        yield return new WaitForEndOfFrame();

        if (loadedData != null)
        {
            if (GameStateManager.Instance != null) GameStateManager.Instance.isRestoringFromSave = true;
            ApplyGlobalState(loadedData.gameProgress);
            if (GameStateManager.Instance != null) GameStateManager.Instance.isRestoringFromSave = false;
        }

        int attempts = 0;
        while (InventoryManager.Instance == null && attempts < 20)
        {
            attempts++;
            yield return new WaitForSeconds(0.05f);
        }

        LoadPlayerPosition(folderPath);
        LoadInventory();
        LoadHotbar(folderPath);
        ApplyPendingPlayerSpawn();
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

    private void OnPlayerStatsReady(PlayerStats stats) => ApplyCachedPlayerRuntimeStats(stats);
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCurrentSceneState();
        ApplyPendingPlayerSpawn();
    }

    // ----------------------------------------------------------------
    // 3. DETALII IMPLEMENTARE (SAVE / LOAD SPECIFIC)
    // ----------------------------------------------------------------

    public void SaveWorldItemStateToDisk(string folderPath)
    {
        WorldSaveData fullSave = new WorldSaveData();
        fullSave.gameProgress.currentSceneName = SceneManager.GetActiveScene().name;
        
        if (GameStateManager.Instance != null && WaveManager.Instance != null)
        {
            fullSave.gameProgress.currentState = GameStateManager.Instance.IsNight ? GameStateManager.GameState.Night : GameStateManager.GameState.Day;
            fullSave.gameProgress.timeRemaining = GameStateManager.Instance.timeRemaining;
            fullSave.gameProgress.currentDayIndex = WaveManager.Instance.GetCurrentDayIndex();
        }

        foreach (var kvp in runtimeSceneCache)
            fullSave.sceneDataList.Add(new SceneSaveEntry(kvp.Key, kvp.Value));

        File.WriteAllText(Path.Combine(folderPath, "world_items.json"), JsonUtility.ToJson(fullSave, true));
    }

    // Noua logică inteligentă de Aplicare a Scenei
    public void ApplyCurrentSceneState()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        currentSceneDestroyedIds.Clear();

        if (!runtimeSceneCache.ContainsKey(currentSceneName)) return;

        SceneSaveData localData = runtimeSceneCache[currentSceneName];
        currentSceneDestroyedIds = new List<string>(localData.destroyedOriginals);

        // 1. Curățăm Entitățile și pregătim Dicționarul cu Entități Statice
        Dictionary<string, Entity> staticEntitiesInScene = new Dictionary<string, Entity>();
        WorldEntityState[] sceneItems = Object.FindObjectsByType<WorldEntityState>(FindObjectsSortMode.None);

        foreach (WorldEntityState state in sceneItems)
        {
            if (!string.IsNullOrEmpty(state.uniqueID))
            {
                if (currentSceneDestroyedIds.Contains(state.uniqueID))
                {
                    // Obiectul fusese distrus
                    state.gameObject.SetActive(false);
                    Destroy(state.gameObject);
                }
                else if (!state.isSpawnedAtRuntime)
                {
                    // Obiect original (ex: Copac). Îl ținem minte ca să-i actualizăm viața.
                    Entity e = state.GetComponent<Entity>();
                    if (e != null) staticEntitiesInScene[state.uniqueID] = e;
                }
            }

            // Distrugem obiectele vechi generate de la runtime pentru a le re-spawna din save
            if (state.isSpawnedAtRuntime)
            {
                state.gameObject.SetActive(false);
                Destroy(state.gameObject);
            }
        }

        // Distrugem toți inamicii, îi vom respawna doar pe cei din cache
        ZombieNPC[] existingEnemies = Object.FindObjectsByType<ZombieNPC>(FindObjectsSortMode.None);
        foreach (var e in existingEnemies) 
        {
            e.gameObject.SetActive(false);
            Destroy(e.gameObject);
        }

        // 2. Aplicăm starea din fișier/cache pe entitățile din scenă
        foreach (var entityData in localData.activeEntities)
        {
            // Dacă este un obiect STATIC și avem date despre el
            if (!entityData.isSpawnedAtRuntime && !string.IsNullOrEmpty(entityData.uniqueID))
            {
                if (staticEntitiesInScene.ContainsKey(entityData.uniqueID))
                {
                    Entity targetEntity = staticEntitiesInScene[entityData.uniqueID];
                    targetEntity.currentHealth = (int)entityData.currentHealth;

                    WorldEntityState wState = targetEntity.GetComponent<WorldEntityState>();
                    if (wState != null) wState.currentHealthOrDurability = entityData.currentHealth;
                }
            }
            else
            {
                // Este o entitate de RUNTIME (Inamic generat sau Drop). O instanțiem!
                SpawnEntityFromSave(entityData);
            }
        }

        // 3. Update WaveManager
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.LockManager();
            WaveManager.Instance.SetCurrentDay(GameStateManager.Instance.currentDay);
            WaveManager.Instance.UnlockManager();
            
            // Re-sincronizare inamici forțat 
            WaveManager.Instance.RefreshActiveEnemies();
            var recalculateMethod = WaveManager.Instance.GetType().GetMethod("RecalculateDayStateAfterLoad", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (recalculateMethod != null) recalculateMethod.Invoke(WaveManager.Instance, null);
        }
    }

    // Metoda unificată de Spawn
    private void SpawnEntityFromSave(EntitySaveData data)
    {
        GameObject prefab = null;
        Debug.Log($"<color=cyan>[SpawnSystem] Încercare spawn pentru: {data.entityName} la poziția {data.position}</color>");

        // 1. Căutăm în Iteme (Drop-uri)
        Item itemSO = ItemVisualManager.Instance.GetItemDataByName(data.entityName);
        if (itemSO != null) 
        {
            prefab = ItemVisualManager.Instance.GetItemVisualPrefab(itemSO);
            Debug.Log($"[SpawnSystem] Găsit ca ITEM. Prefab null? {prefab == null}");
        }
        else 
        {
            // 2. Căutăm în Entități (Zombii)
            EntityData entitySO = ItemVisualManager.Instance.GetEntityDataByName(data.entityName);
            if (entitySO != null) 
            {
                prefab = ItemVisualManager.Instance.GetEntityVisualPrefab(entitySO);
                Debug.Log($"[SpawnSystem] Găsit ca ENTITY. Prefab null? {prefab == null}");
            }
        }

        // 3. Verificăm dacă am găsit prefab-ul în final
        if (prefab != null)
        {
            GameObject spawned = Instantiate(prefab, data.position, Quaternion.Euler(data.rotation));
            
            // Sincronizăm viața (pentru Entity clasic)
            Entity entityComp = spawned.GetComponent<Entity>();
            if (entityComp != null) 
            {
                entityComp.currentHealth = (int)data.currentHealth;
                Debug.Log($"[SpawnSystem] Succes: {data.entityName} spawnat cu {entityComp.currentHealth} HP.");
            }

            // Sincronizăm starea mondială (pentru Drop-uri/Durabilitate)
            WorldEntityState state = spawned.GetComponent<WorldEntityState>();
            if (state != null)
            {
                state.isSpawnedAtRuntime = true;
                state.currentHealthOrDurability = data.currentHealth;
                if (!string.IsNullOrEmpty(data.uniqueID)) state.uniqueID = data.uniqueID;
                Debug.Log($"[SpawnSystem] Starea WorldEntityState a fost configurată pentru ID: {state.uniqueID}");
            }
        }
        else
        {
            Debug.LogError($"<color=red>[SpawnSystem] EROARE: Nu s-a găsit niciun prefab pentru numele '{data.entityName}'. Verifică ItemVisualManager!</color>");
        }
    }

    private void ApplyGlobalState(GameProgressSaveData progress)
    {
        if (GameStateManager.Instance != null && WaveManager.Instance != null)
        {
            GameStateManager.Instance.SetStateManually(progress.currentState, progress.timeRemaining);
            WaveManager.Instance.LockManager();
            WaveManager.Instance.SetCurrentDay(progress.currentDayIndex);
            WaveManager.Instance.UnlockManager();
            
            float totalDur = progress.currentState == GameStateManager.GameState.Day ? 300f : 180f;
            GlobalEvents.NotifyTimeUpdate(progress.timeRemaining / totalDur, progress.currentState == GameStateManager.GameState.Night);
        }
    }

    public void RegisterDestroyedWorldItem(string id)
    {
        if (!string.IsNullOrEmpty(id) && !currentSceneDestroyedIds.Contains(id))
            currentSceneDestroyedIds.Add(id);
    }
    
    public void UnregisterDestroyedWorldItem(string id)
    {
        if (!string.IsNullOrEmpty(id) && currentSceneDestroyedIds.Contains(id))
            currentSceneDestroyedIds.Remove(id);
    }

    // ... Metodele pentru Inventar / Player rămân identice ...

    public void SaveInventory()
    {
        if (InventoryManager.Instance == null) return;
        
        InventorySaveData saveData = new InventorySaveData();
        string folderPath = GetCurrentSaveFolderPath();

        // 1. Salvăm Sloturile din Rucsac (Doar cele pline)
        foreach (InventorySlot slot in InventoryManager.Instance.allSlots)
        {
            if (slot != null && slot.itemData != null && slot.count > 0)
            {
                saveData.slots.Add(new SlotSaveData(slot));
            }
        }

        // 2. Salvăm Slotul Echipat (Mâna jucătorului)
        if (EquippedManager.Instance != null)
        {
            InventorySlot equippedSlot = EquippedManager.Instance.GetEquippedSlot();
            if (equippedSlot != null && equippedSlot.itemData != null)
            {
                saveData.equippedSlot = new SlotSaveData(equippedSlot);
            }
        }

        // Scriem pe disc
        string filePath = Path.Combine(folderPath, "inventory.json");
        File.WriteAllText(filePath, JsonUtility.ToJson(saveData, true));
        
        Debug.Log($"<color=cyan>[SaveManager] Inventar salvat. {saveData.slots.Count} sloturi ocupate.</color>");
    }

    public void LoadInventory()
    {
        if (InventoryManager.Instance == null) return;
        
        string folderPath = GetCurrentSaveFolderPath();
        string filePath = Path.Combine(folderPath, "inventory.json");
        
        if (!File.Exists(filePath)) return;

        InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(File.ReadAllText(filePath));
        
        // 1. Curățăm inventarul curent (Sărim peste toate sloturile și le facem Clear)
        InventoryManager.Instance.ClearInventory(); 
        
        if (EquippedManager.Instance != null) 
        {
            EquippedManager.Instance.ClearEquippedSlot(); 
        }
        
        // 2. Restaurăm sloturile din Rucsac
        foreach (SlotSaveData data in saveData.slots)
        {
            Item itemSO = FindItemInVisualManager(data.itemName);
            if (itemSO != null)
            {
                // IMPORTANT: Ne asigurăm că indexul este valid
                if (data.slotIndex >= 0 && data.slotIndex < InventoryManager.Instance.allSlots.Count)
                {
                    // Luăm referința la slotul persistent deja existent
                    InventorySlot persistentSlot = InventoryManager.Instance.allSlots[data.slotIndex];

                    // Repopulăm datele direct în el
                    persistentSlot.SetItem(itemSO);
                    persistentSlot.count = data.amount;

                    if (persistentSlot.state != null && data.durability != -1f)
                    {
                        persistentSlot.state.currentDurability = data.durability;
                    }
                    
                    // Reconstruim dicționarul ca să știm noile sume
                    InventoryManager.Instance.RebuildInventoryDictionary();
                }
                else
                {
                    Debug.LogWarning($"[SaveManager] Am încercat încărcarea unui slot la un index invalid: {data.slotIndex}");
                }
            }
        }
        
        // 3. Forțăm reconstrucția dicționarului intern al Inventarului (pentru căutări)
        // Va trebui să expui această metodă dacă e privată în InventoryManager
        InventoryManager.Instance.GetType().GetMethod("RebuildInventoryDictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(InventoryManager.Instance, null);
        
        // 4. Restaurăm Slotul Echipat (Folosim HandleEquipSlotRequest ca să trigăruim și vizualul)
        if (saveData.equippedSlot != null && !string.IsNullOrEmpty(saveData.equippedSlot.itemName)) 
        {
            Item equippedSO = FindItemInVisualManager(saveData.equippedSlot.itemName);
            if (equippedSO != null) 
            {
                // Cream un slot temporar doar pentru a-l pasa ca "cerere" către EquippedManager
                InventorySlot tempSlot = new InventorySlot(SlotType.Tool, equippedSO, -1);
                tempSlot.count = saveData.equippedSlot.amount;
                
                if (tempSlot.state != null && saveData.equippedSlot.durability != -1f)
                {
                    tempSlot.state.currentDurability = saveData.equippedSlot.durability;
                }
                
                // Trimitem cererea ca și cum am fi dat click din inventar
                // EquippedManager se va ocupa să-l copieze și să-l afișeze în mână
                GlobalEvents.RequestSlotEquip(tempSlot);
            }
        }
        
        Debug.Log($"<color=green>[SaveManager] Inventar încărcat cu succes!</color>");
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
        PlayerStatsSaveData data = new PlayerStatsSaveData { health = stats.currentHealth, stamina = stats.currentStamina };
        File.WriteAllText(Path.Combine(folderPath, "player_stats.json"), JsonUtility.ToJson(data, true));
    }

    private void LoadPlayerStats(string folderPath)
    {
        string path = Path.Combine(folderPath, "player_stats.json");
        if (!File.Exists(path)) return;
        cachedPlayerStats = JsonUtility.FromJson<PlayerRuntimeStats>(File.ReadAllText(path));
        hasCachedPlayerStats = true;
    }

    public void CaptureAndSaveScreenshot(string folderPath) { ScreenCapture.CaptureScreenshot(Path.Combine(folderPath, "screenshot.png")); }
    private Item FindItemInVisualManager(string itemName) { return ItemVisualManager.Instance != null ? ItemVisualManager.Instance.GetItemDataByName(itemName) : null; }
}