using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }
    private UIDocument uiDocument;
    private VisualElement root;

    private Button btnContinue;
    private Button btnRestart;
    private Button btnSave;
    private Button btnLoad;
    private Button btnMainMenu;

    private bool isPaused = false;

    [Header("Configurare Scene")]
    public string mainMenuSceneName = "StartMenu";
    public string startingSceneName = "Forest";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Ascundem meniul la început
        root.style.display = DisplayStyle.None;

        // Referințe butoane (Asigură-te că numele din UXML coincid)
        btnContinue = root.Q<Button>("BtnContinue");
        btnRestart = root.Q<Button>("BtnRestart");
        btnSave = root.Q<Button>("BtnSave");
        btnLoad = root.Q<Button>("BtnLoad");
        btnMainMenu = root.Q<Button>("BtnMainMenu");

        // Evenimente
        btnContinue.clicked += ResumeGame;
        btnRestart.clicked += RestartLevel;
        btnSave.clicked += SaveGame;
        btnLoad.clicked += LoadGame;
        btnMainMenu.clicked += BackToMainMenu;
    }

    public bool IsGamePaused()
    { return isPaused; }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 1. Verificăm ierarhia: Primul care răspunde este Inventarul
            if (InventoryPanelController.Instance != null && InventoryPanelController.Instance.getIsPanelOpen())
            {
                // Închidem inventarul și oprim execuția aici (nu intrăm în pauză)
                InventoryPanelController.Instance.TogglePanel();
                Debug.Log("[PauseMenu] Inventar închis via Escape.");
                return; 
            }

            // 2. Dacă nu avem inventar deschis, gestionăm meniul de pauză
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        root.style.display = DisplayStyle.Flex;

        // ELIBERĂM CURSORUL
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        // OPRIM CAMERA (Accesăm instanța Singleton din FirstPersonController)
        if (FirstPersonController.Instance != null)
        {
            FirstPersonController.Instance.cameraCanMove = false;
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        root.style.display = DisplayStyle.None;

        // BLOCĂM CURSORUL ÎNAPOI ÎN JOC
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        // ACTIVĂM CAMERA ÎNAPOI
        if (FirstPersonController.Instance != null)
        {
            FirstPersonController.Instance.cameraCanMove = true;
        }
    }

    void RestartLevel()
    {
        Time.timeScale = 1f;

        // 1. Curățăm cache-ul de obiecte (SaveManager)
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearRuntimeCache();
        }

        // 2. RESETĂM TIMPUL ȘI ZIUA (GameStateManager)
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RestartGameProgress();
        }

        // 3. Reîncărcăm scena
        SceneManager.LoadScene(startingSceneName);
    }

    void SaveGame()
    {
        // Verificăm dacă instanța SaveSlotUI există
        if (SaveSlotUI.Instance != null)
        {
            // Deschidem ecranul în modul SAVE și îi dăm root-ul actual (Pause Menu)
            // pentru a ști unde să se întoarcă la apăsarea butonului Back
            SaveSlotUI.Instance.ShowScreen(SaveMenuMode.Save, root);
        }
        else
        {
            Debug.LogError("SaveSlotUI nu a fost găsit în scenă!");
        }
    }

    void LoadGame()
    {
        if (SaveSlotUI.Instance != null)
        {
            // Deschidem ecranul în modul LOAD
            SaveSlotUI.Instance.ShowScreen(SaveMenuMode.Load, root);
        }
        else
        {
            Debug.LogError("SaveSlotUI nu a fost găsit în scenă!");
        }
    }

    void BackToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}