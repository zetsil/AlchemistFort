using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement mainContainer; // Containerul care conține butoanele principale

    private Button btnPlay;
    private Button btnLoad;
    private Button btnExit;

    [Header("Configurare Scene")]
    public string gameplaySceneName = "Forest"; 

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;
        mainContainer = root.Q<VisualElement>("MainContainer"); // Asigură-te că ai acest nume în UXML

        btnPlay = root.Q<Button>("BtnPlay");
        btnLoad = root.Q<Button>("BtnLoad");
        btnExit = root.Q<Button>("BtnExit");

        btnPlay.clicked += StartNewGame;
        btnLoad.clicked += OpenLoadMenu;
        btnExit.clicked += ExitGame;

        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    void StartNewGame()
    {
        // Pornim jocul direct pe scena de gameplay
        SceneManager.LoadScene(gameplaySceneName);
    }

    void OpenLoadMenu()
    {
        // Apelăm SaveSlotUI în modul LOAD și îi dăm containerul principal să îl ascundă
        if (SaveSlotUI.Instance != null)
        {
            SaveSlotUI.Instance.ShowScreen(SaveMenuMode.Load, mainContainer);
        }
    }

    void ExitGame()
    {
        Application.Quit();
    }
}