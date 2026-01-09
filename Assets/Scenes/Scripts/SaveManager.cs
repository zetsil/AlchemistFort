using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;


[System.Serializable]
public class SlotSaveData
{
    public string itemName;      // Identificator pentru ScriptableObject
    public int amount;           // Câte obiecte sunt în slot
    public int slotIndex;        // Indexul slotului
    public float durability;     // Durabilitatea (dacă este Tool)

    public SlotSaveData(InventorySlot slot)
    {
        this.itemName = slot.itemData.itemName;
        this.amount = slot.count;
        this.slotIndex = slot.slotIndex;
        // Dacă are stare (durabilitate), o salvăm, altfel punem -1
        this.durability = (slot.state != null) ? slot.state.currentDurability : -1f;
    }
}

[System.Serializable]
public class DroppedItemSaveData
{
    public string itemName;       // Numele Item-ului (pentru VisualManager)
    public Vector3 position;
    public Vector3 rotation;
    public float durability;

    public DroppedItemSaveData(WorldEntityState worldState)
    {
        // Încercăm să luăm numele de pe ItemPickup (pentru obiecte dropate)
        var pickup = worldState.GetComponent<ItemPickup>();
        if (pickup != null && pickup.itemData != null)
        {
            this.itemName = pickup.itemData.itemName;
        }
        else
        {
            // Dacă nu e pickup, verificăm dacă e o Entity (pentru clădiri/inamici)
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
public class WorldSaveData
{
    // Lista 1: ID-urile obiectelor originale care au fost DISTRUSE (Blacklist)
    public List<string> destroyedOriginals = new List<string>();

    // Lista 2: Datele complete ale obiectelor NOI create (Whitelist)
    public List<DroppedItemSaveData> newDroppedItems = new List<DroppedItemSaveData>();
}


[System.Serializable]
public class PlayerPositionData
{
    public Vector3 pos;
    public float rotationYaw;   // Rotația stânga/dreapta a corpului
    public float cameraPitch;   // Rotația sus/jos a privirii
}

[System.Serializable]
public class InventorySaveData
{
    public List<SlotSaveData> slots = new List<SlotSaveData>();
    public SlotSaveData equippedSlot;
}


public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string baseSavePath; // Calea către folderul "Saves"
    public string currentSaveName = "Salvarea_1"; // Numele folderului de profil
    public List<string> destroyedOriginals = new List<string>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Definim folderul rădăcină pentru toate salvările
            baseSavePath = Path.Combine(Application.persistentDataPath, "Saves");

            // Ne asigurăm că folderul "Saves" există
            if (!Directory.Exists(baseSavePath))
            {
                Directory.CreateDirectory(baseSavePath);
            }
        }
        else Destroy(gameObject);
    }

    // Funcție helper pentru a obține calea către folderul specific al unei salvări
    private string GetCurrentSaveFolderPath()
    {
        string folderPath = Path.Combine(baseSavePath, currentSaveName);

        // Dacă folderul pentru "Salvarea_1" nu există, îl creăm
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return folderPath;
    }

    public void PerformFullSave()
    {
        string folderPath = GetCurrentSaveFolderPath();

        // 1. Salvează Screenshot-ul
        CaptureAndSaveScreenshot(folderPath);
        SaveInventory();
        SavePlayerPosition(folderPath);
        SaveWorldItemState(folderPath);

        Debug.Log($"<color=cyan>Full Save Complete:</color> {currentSaveName}");
    }

    public void PerformFullLoad()
    {
        string folderPath = GetCurrentSaveFolderPath();

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"Folderul de salvare nu există: {folderPath}");
            return;
        }

        // Pornim procesul secvențial
        StartCoroutine(LoadSequence(folderPath));
    }


    private IEnumerator LoadSequence(string folderPath)
    {
        Debug.Log("<color=orange>Loading Scene...</color>");

        // A. RESETĂRI PRE-LOAD (Esențial pentru a nu bloca mișcarea)
        Time.timeScale = 1f; // Dezghețăm jocul
        destroyedOriginals.Clear(); // Curățăm lista de obiecte distruse din memorie

        // B. ÎNCĂRCARE ASINCRONĂ
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Forest");

        // Opțional: Aici poți actualiza o bară de progres UI
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // C. AȘTEPTARE DE SIGURANȚĂ (Eventual evenimentul SceneLoaded)
        // Așteptăm un frame pentru ca ierarhia să fie stabilă
        yield return new WaitForEndOfFrame();

        // D. VERIFICARE MANAGER-I (Nu aplicăm datele pe "orb")
        int attempts = 0;
        while (InventoryManager.Instance == null && attempts < 10)
        {
            attempts++;
            yield return new WaitForSeconds(0.05f); // Așteptăm scurt până apar Singletons
        }

        Debug.Log("<color=orange>Applying Saved Data to NEW Scene...</color>");

        // E. APLICARE DATE
        LoadInventory();
        LoadPlayerPosition(folderPath);
        LoadWorldItemState(folderPath);

        // F. STARE CURSOR (Reactivăm controlul jucătorului)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Dacă controllerul are un flag intern de mișcare, îl resetăm prin Reflection sau direct:
        if (FirstPersonController.Instance != null)
        {
            FirstPersonController.Instance.cameraCanMove = true;
            // Dacă ai variabila yaw/pitch privată, Reflection-ul din LoadPlayerPosition se ocupă deja.
        }

        Debug.Log($"<color=green>Load Complete and Physics Resumed!</color>");
    }


    public void RegisterDestroyedWorldItem(string id)
    {
        if (!string.IsNullOrEmpty(id) && !destroyedOriginals.Contains(id))
        {
            destroyedOriginals.Add(id);
        }
    }


    public void SaveInventory()
    {
        InventorySaveData saveData = new InventorySaveData();

        // A. Salvează sloturile din rucsac
        foreach (InventorySlot slot in InventoryManager.Instance.allSlots)
        {
            if (slot.itemData != null && slot.count > 0)
            {
                saveData.slots.Add(new SlotSaveData(slot));
            }
        }

        // B. Salvează unealta echipată (dacă există)
        InventorySlot equipped = EquippedManager.Instance.GetEquippedSlot();
        if (equipped != null && equipped.itemData != null)
        {
            saveData.equippedSlot = new SlotSaveData(equipped);
        }

        string filePath = Path.Combine(GetCurrentSaveFolderPath(), "inventory.json");
        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(filePath, json);

        Debug.Log($"<color=green>Inventar și Unealtă echipată salvate!</color>");
    }

    public void LoadInventory()
    {

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("LoadInventory ignorat: Nu există InventoryManager în scenă.");
            return;
        }

        string filePath = Path.Combine(GetCurrentSaveFolderPath(), "inventory.json");

        if (!File.Exists(filePath)) return;

        string json = File.ReadAllText(filePath);
        InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

        // 1. Curăță inventarul și mâna
        InventoryManager.Instance.ClearInventory();
        // Forțăm o curățare a mâinii (opțional, depinde de logica ta din Unequip)
        // EquippedManager.Instance.UnequipTool(); 

        // 2. Încarcă sloturile din rucsac
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

        // 3. Încarcă unealta în mână (prin GlobalEvents)
        if (saveData.equippedSlot != null && !string.IsNullOrEmpty(saveData.equippedSlot.itemName))
        {
            Item equippedSO = FindItemInVisualManager(saveData.equippedSlot.itemName);
            if (equippedSO != null)
            {
                InventorySlot equippedSlot = new InventorySlot(equippedSO, -1);
                equippedSlot.count = saveData.equippedSlot.amount;
                if (equippedSlot.state != null && saveData.equippedSlot.durability != -1f)
                    equippedSlot.state.currentDurability = saveData.equippedSlot.durability;

                // Trimitem cererea de echipare pentru a activa vizual unealta
                GlobalEvents.RequestSlotEquip(equippedSlot);
            }
        }

        Debug.Log("<color=blue>Inventar și Unealtă restabilite.</color>");
    }

    public void SavePlayerPosition(string folderPath)
    {
        if (FirstPersonController.Instance == null) return;

        PlayerPositionData data = new PlayerPositionData();

        // 1. Luăm poziția din transform-ul controllerului
        data.pos = FirstPersonController.Instance.transform.position;

        // 2. Extragem variabilele interne de rotație prin Reflection sau făcându-le publice
        // Deoarece în scriptul tău 'yaw' și 'pitch' sunt private, folosim valorile din transform:
        data.rotationYaw = FirstPersonController.Instance.transform.localEulerAngles.y;
        data.cameraPitch = FirstPersonController.Instance.playerCamera.transform.localEulerAngles.x;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Path.Combine(folderPath, "player_pos.json"), json);

        Debug.Log("<color=yellow>Poziția și Camera au fost salvate.</color>");
    }

    public void LoadPlayerPosition(string folderPath)
    {
        string filePath = Path.Combine(folderPath, "player_pos.json");
        if (!File.Exists(filePath)) return;

        string json = File.ReadAllText(filePath);
        PlayerPositionData data = JsonUtility.FromJson<PlayerPositionData>(json);

        if (FirstPersonController.Instance != null)
        {
            // 1. Dezactivăm temporar fizica pentru teleportare
            Rigidbody rb = FirstPersonController.Instance.GetComponent<Rigidbody>();
            if (rb != null) rb.linearVelocity = Vector3.zero;

            // 2. Setăm poziția
            FirstPersonController.Instance.transform.position = data.pos;

            // 3. Setăm rotațiile în Transform
            FirstPersonController.Instance.transform.localEulerAngles = new Vector3(0, data.rotationYaw, 0);
            FirstPersonController.Instance.playerCamera.transform.localEulerAngles = new Vector3(data.cameraPitch, 0, 0);

            // 4. IMPORTANT: Actualizăm variabilele interne private din FirstPersonController
            // Trebuie să folosim Reflection deoarece variabilele 'yaw' și 'pitch' sunt private în scriptul tău
            var fpcType = FirstPersonController.Instance.GetType();
            var yawField = fpcType.GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pitchField = fpcType.GetField("pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (yawField != null) yawField.SetValue(FirstPersonController.Instance, data.rotationYaw);
            if (pitchField != null) pitchField.SetValue(FirstPersonController.Instance, data.cameraPitch);

            Debug.Log("<color=yellow>Poziția și Camera au fost restabilite.</color>");
        }
    }

    public void CaptureAndSaveScreenshot(string folderPath)
    {
        string screenshotPath = Path.Combine(folderPath, "screenshot.png");
        // Capturăm ecranul la dimensiunea ferestrei
        ScreenCapture.CaptureScreenshot(screenshotPath);
        Debug.Log("Screenshot salvat la: " + screenshotPath);
    }


    public void SaveWorldItemState(string folderPath)
    {
        WorldSaveData worldData = new WorldSaveData();

        // Salvăm doar lista de obiecte distruse (Blacklist)
        worldData.destroyedOriginals = this.destroyedOriginals;

        // Ignorăm momentan newDroppedItems (va fi o listă goală)
        worldData.newDroppedItems = new List<DroppedItemSaveData>();

        // 2. Salvăm Whitelist-ul (Obiectele spawnate la runtime)
        WorldEntityState[] allStates = GameObject.FindObjectsOfType<WorldEntityState>();

        foreach (WorldEntityState state in allStates)
        {
            // Salvăm doar dacă obiectul a fost creat în timpul jocului
            if (state.isSpawnedAtRuntime)
            {
                worldData.newDroppedItems.Add(new DroppedItemSaveData(state));
            }
        }

        string filePath = Path.Combine(folderPath, "world_items.json");
        string json = JsonUtility.ToJson(worldData, true);
        File.WriteAllText(filePath, json);

        Debug.Log("<color=white>Starea obiectelor din lume (Blacklist) a fost salvată.</color>");
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

        // Actualizăm lista locală
        this.destroyedOriginals = worldData.destroyedOriginals;

        // Căutăm toate obiectele WorldItem din scenă și le distrugem pe cele din listă
        WorldEntityState[] sceneItems = GameObject.FindObjectsOfType<WorldEntityState>();
        int count = 0;

        foreach (WorldEntityState item in sceneItems)
        {
            // Dacă ID-ul obiectului se află în lista de distruse, îl ștergem
            if (destroyedOriginals.Contains(item.uniqueID))
            {
                Destroy(item.gameObject);
                count++;
            }
        }

        // B. Re-spawnăm obiectele dropate (Whitelist)
        foreach (DroppedItemSaveData dropData in worldData.newDroppedItems)
        {
            SpawnDroppedItem(dropData);
        }

        Debug.Log($"<color=white>Lumea a fost curățată: {count} obiecte originale eliminate.</color>");
    }

    private Item FindItemInVisualManager(string itemName)
    {
        if (ItemVisualManager.Instance == null) return null;
        return ItemVisualManager.Instance.GetItemDataByName(itemName);
    }


    private void SpawnDroppedItem(DroppedItemSaveData data)
    {
        GameObject prefab = null;

        // 1. Încercăm să găsim obiectul ca fiind un ITEM
        Item itemSO = ItemVisualManager.Instance.GetItemDataByName(data.itemName);
        if (itemSO != null)
        {
            prefab = ItemVisualManager.Instance.GetItemVisualPrefab(itemSO);
        }
        else
        {
            // 2. Dacă nu e item, încercăm să îl găsim ca ENTITY (Clădire/Inamic)
            EntityData entitySO = ItemVisualManager.Instance.GetEntityDataByName(data.itemName);
            if (entitySO != null)
            {
                prefab = ItemVisualManager.Instance.GetEntityVisualPrefab(entitySO);
            }
        }

        // Dacă am găsit un prefab valid în oricare din mape
        if (prefab != null)
        {
            // 3. Instanțiem obiectul
            GameObject spawned = Instantiate(prefab, data.position, Quaternion.Euler(data.rotation));

            // 4. Configurăm componenta WorldEntityState
            WorldEntityState state = spawned.GetComponent<WorldEntityState>();
            if (state != null)
            {
                state.isSpawnedAtRuntime = true;
                state.currentHealthOrDurability = data.durability;
            }

            // 5. Aplicăm viața înapoi în componenta Entity (dacă există)
            Entity entityComp = spawned.GetComponent<Entity>();
            if (entityComp != null)
            {
                // entityComp.currentHealth = data.durability;
            }
        }
        else
        {
            Debug.LogWarning($"[SaveManager] Nu s-a putut găsi prefab pentru: {data.itemName}");
        }
    }
    
    public void UnregisterDestroyedWorldItem(string id)
    {
        if (destroyedOriginals.Contains(id))
        {
            destroyedOriginals.Remove(id);
            Debug.Log($"[SaveManager] ID-ul {id} a fost scos din lista de distruse.");
        }
    }
}