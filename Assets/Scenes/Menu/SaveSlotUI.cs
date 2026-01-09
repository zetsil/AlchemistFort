using UnityEngine;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;


public enum SaveMenuMode { Load, Save }
public class SaveSlotUI : MonoBehaviour
{
    public static SaveSlotUI Instance { get; private set; }

    [Header("UI Toolkit Assets")]
    public UIDocument uiDocument;
    public VisualTreeAsset slotTemplate;

    private VisualElement root;
    private VisualElement slotContainer;
    private Label selectedSaveLabel;
    private Button backButton;

    // Referință către ecranul anterior pentru a ști unde să ne întoarcem
    private VisualElement previousScreen;

    private SaveMenuMode currentMode;
    private Label headerLabel;

    void Awake()
    {
        if (Instance == null) Instance = this;

        root = uiDocument.rootVisualElement;

        // --- AICI ERA LIPSA ---
        // Asigură-te că în UI Builder, Label-ul tău de titlu are numele "HeaderLabel"
        headerLabel = root.Q<Label>("HeaderLabel");

        slotContainer = root.Q<VisualElement>("SlotContainer");

        slotContainer.style.flexDirection = FlexDirection.Row;
        slotContainer.style.flexWrap = Wrap.Wrap;
        slotContainer.style.justifyContent = Justify.Center;
        
        selectedSaveLabel = root.Q<Label>("SelectedSaveName");
        backButton = root.Q<Button>("BackButton");

        // Verificare de siguranță pentru Debug
        if (headerLabel == null) Debug.LogError("Nu am găsit HeaderLabel în UXML!");

        backButton.clicked += GoBack;
        root.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Deschide ecranul de salvări.
    /// </summary>
    /// <param name="fromScreen">Elementul vizual (meniul) care trebuie ascuns/reafișat la Back.</param>
    public void ShowScreen(SaveMenuMode mode, VisualElement fromScreen = null)
    {
        currentMode = mode;
        previousScreen = fromScreen;

        if (previousScreen != null) previousScreen.style.display = DisplayStyle.None;
        root.style.display = DisplayStyle.Flex;

        // --- VERIFICARE SCENĂ ---
        // Executăm logica de pauză DOAR dacă nu suntem în Main Menu
        if (SceneManager.GetActiveScene().name != "StartMenu") 
        {
            Time.timeScale = 0f;
            if (FirstPersonController.Instance != null)
                FirstPersonController.Instance.cameraCanMove = false;
        }

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        if (headerLabel != null)
            headerLabel.text = (currentMode == SaveMenuMode.Load) ? "LOAD GAME" : "SAVE GAME";

        RefreshSaveSlots();
    }

    private void GoBack()
    {
        root.style.display = DisplayStyle.None;
        if (previousScreen != null) previousScreen.style.display = DisplayStyle.Flex;

        // Dacă ieșim din meniul de Load în Main Menu, resetăm timpul la normal
        if (SceneManager.GetActiveScene().name == "StartMenu")
        {
            Time.timeScale = 1f;
        }
    }

    private void RefreshSaveSlots()
    {
        if (slotContainer == null) return;
        slotContainer.Clear();

        string savesPath = Path.Combine(Application.persistentDataPath, "Saves");
        if (!Directory.Exists(savesPath)) Directory.CreateDirectory(savesPath);

        string[] saveFolders = Directory.GetDirectories(savesPath);

        // 1. Sloturile existente
        foreach (string fullPath in saveFolders)
        {
            string folderName = Path.GetFileName(fullPath);
            CreateSlotElement(folderName, () => SelectSave(folderName), false);
        }

        // 2. Slotul "+ NEW SAVE"
        if (currentMode == SaveMenuMode.Save)
        {
            CreateSlotElement("+ NEW SAVE", () =>
            {
                // Generăm un nume unic (ex: Save_Slot_1, Save_Slot_2)
                int nextNumber = saveFolders.Length + 1;
                string newName = "Save_Slot_" + nextNumber;

                // FOARTE IMPORTANT: Nu chemăm Managerul direct. 
                // Chemăm SelectSave care va porni Corutina de screenshot și închidere.
                SelectSave(newName); 
                
            }, true);
        }
        else if (saveFolders.Length == 0)
        {
            CreateSlotElement("NO SAVES FOUND", () => { }, true);
        }
    }

    private void SelectSave(string saveName)
    {

        SaveManager.Instance.currentSaveName = saveName;

        if (currentMode == SaveMenuMode.Load)
        {
            SaveManager.Instance.PerformFullLoad();
            GoBack();
            
            // Opțional: Forțăm un ResumeGame dacă avem PauseManager
            PauseMenuManager pauseMenu = FindObjectOfType<PauseMenuManager>();
            if (pauseMenu != null) pauseMenu.ResumeGame();
        }
        else
        {
            // Pornim Corutina pentru o salvare curată
            StartCoroutine(SaveWithScreenshotRoutine());
        }
    }


    private IEnumerator SaveWithScreenshotRoutine()
    {
        // 1. Ascundem UI-ul imediat
        root.style.display = DisplayStyle.None;
        
        // 2. Așteptăm finalul cadrului (foarte important!)
        // Această linie permite Unity-ului să curețe ecranul de UI
        yield return new WaitForEndOfFrame();

        // 3. Acum chemăm funcția de salvare din manager
        SaveManager.Instance.PerformFullSave();

        // 4. Așteptăm un timp infim pentru a lăsa sistemul de fișiere să proceseze
        yield return new WaitForSecondsRealtime(0.1f);

        // 5. Reafișăm meniul și facem refresh la poze
        root.style.display = DisplayStyle.Flex;
        RefreshSaveSlots();
    }


    private void CreateSlotElement(string labelText, System.Action onClickAction, bool isSpecial)
    {
        if (slotTemplate == null) return;

        VisualElement slot = slotTemplate.Instantiate();
        Button btn = slot.Q<Button>("SlotButton");
        Label nameLabel = slot.Q<Label>("FolderName");
        VisualElement imgPlaceholder = slot.Q<VisualElement>("ScreenshotPlaceholder");

        if (nameLabel != null) nameLabel.text = labelText;

        if (!isSpecial && imgPlaceholder != null)
        {
            string path = Path.Combine(Application.persistentDataPath, "Saves", labelText, "screenshot.png");
            if (File.Exists(path))
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(fileData))
                {
                    imgPlaceholder.style.backgroundImage = new StyleBackground(tex);
                }
            }
        }

        if (btn != null)
        {
            btn.clicked += onClickAction;

            if (isSpecial)
            {
                btn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Gri transparent
                btn.style.borderLeftWidth = btn.style.borderRightWidth =
                btn.style.borderTopWidth = btn.style.borderBottomWidth = 1f;
                // btn.style.borderStyle = BorderStyle.Dotted; // Margine punctată pentru "New"
            }
            else
            {
                btn.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f); // Negru solid
                btn.style.borderLeftColor = btn.style.borderRightColor =
                btn.style.borderTopColor = btn.style.borderBottomColor = new Color(0, 1, 0, 0.5f); // Contur verde
            }
        }

        slotContainer.Add(slot);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && root.style.display == DisplayStyle.Flex)
        {
            GoBack();
        }
    }
}