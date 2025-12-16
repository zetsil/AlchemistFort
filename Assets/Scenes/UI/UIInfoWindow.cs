using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;



// Noua denumire a clasei!
public class UIInfoWindow : MonoBehaviour
{
    // Asigură-te că tragi InfoWindow.uxml aici din Inspector
    public VisualTreeAsset infoWindowTemplate;

    private UIDocument _uiDocument;

    // Numele elementului rădăcină (root) al UI-ului principal
    private VisualElement _rootElement;

    // Vom folosi un sistem simplu: afișăm o singură fereastră la un moment dat
    private VisualElement _currentInfoWindow;

    // Cât timp stă fereastra pe ecran (poți ajusta asta)
    private const float DISPLAY_TIME = 4f;
    private const string INFO_CLASS = "info-window"; // Clasa USS
    private const string ALERT_CLASS = "alert-window";


    // Referință la corutina activă, pentru a o putea opri dacă este nevoie
    private Coroutine _removalCoroutine;



    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null || _uiDocument.rootVisualElement == null)
        {
            Debug.LogError("UIDocument sau rootVisualElement lipsesc!");
            return;
        }
        _rootElement = _uiDocument.rootVisualElement;

    }

    private void OnEnable()
    {
        // Ne abonăm la evenimentul din Subject (rămâne același)
        GlobalEvents.OnNotificationRequested += DisplayInfo;
    }

    private void OnDisable()
    {
        // Ne dezabonăm
        GlobalEvents.OnNotificationRequested -= DisplayInfo;
    }

    // Funcția Observer: primește și afișează mesajul
    private void DisplayInfo(string message, MessageType type)
    {
        if (infoWindowTemplate == null) 
        {
            Debug.LogError("InfoWindow UXML Template lipsește din Inspector.");
            return;
        }

        // 1. Curăță fereastra veche (pentru a afișa doar o notificare la un moment dat)
        if (_currentInfoWindow != null)
        {
            if (_removalCoroutine != null)
            {
                StopCoroutine(_removalCoroutine);
            }
            _currentInfoWindow.RemoveFromHierarchy();
        }

        // 2. Creează instanța și ia referințele la elementele din UXML
        VisualElement newInfoWindow = infoWindowTemplate.Instantiate();
        
        // Elementele structurii UXML îmbunătățite
        VisualElement iconElement = newInfoWindow.Q<VisualElement>("Icon");
        VisualElement separator = newInfoWindow.Q<VisualElement>("Separator");
        Label nameLabel = newInfoWindow.Q<Label>("BuildingNameLabel"); 
        Label costLabel = newInfoWindow.Q<Label>("CostLabel");
        
        // Asigură-te că toate elementele de bază sunt găsite
        if (nameLabel == null || costLabel == null || iconElement == null)
        {
            Debug.LogError("Eroare UXML: Lipsește unul dintre elementele 'BuildingNameLabel', 'CostLabel', sau 'Icon'.");
            return;
        }
        
        // 3. Elimină clasele vechi și setează clasa de bază
        newInfoWindow.RemoveFromClassList(INFO_CLASS);
        newInfoWindow.RemoveFromClassList(ALERT_CLASS);
        
        // Inițializează vizibilitatea elementelor secundare
        costLabel.style.display = DisplayStyle.Flex;
        separator.style.display = DisplayStyle.Flex;
        iconElement.style.display = DisplayStyle.Flex;


        // 4. LOGICA DE STIL ȘI CONȚINUT PE BAZA TIPULUI DE MESAJ
        switch (type)
        {
            case MessageType.Alert:
                // Mesaje de eroare (ex: Pickaxe Required)
                
                // Aplică clasa de stil Alert (Roșu, Bold)
                newInfoWindow.AddToClassList(ALERT_CLASS); 
                
                // Conținut
                nameLabel.text = $"! {message}";
                
                // Ascunde elementele secundare
                costLabel.style.display = DisplayStyle.None;
                separator.style.display = DisplayStyle.None;
                
                // Setare iconiță Alertă (Notă: necesită o textură reală pentru background)
                // Exemplu simplu:
                iconElement.style.backgroundColor = Color.red; 
                
                break;

            case MessageType.ResourceNeeded:
                // Mesaje de cost (ex: la Hover peste clădire)
                
                // Aplică clasa de stil Info/Cost (Neutru, Gri)
                newInfoWindow.AddToClassList(INFO_CLASS); 

                // Împărțim mesajul în Nume și Cost (presupunând formatul "Nume\nCost...")
                string[] parts = message.Split('\n');
                
                if (parts.Length >= 2)
                {
                    nameLabel.text = parts[0]; // Numele Clădirii
                    costLabel.text = parts[1]; // Costul Resurselor
                } 
                else 
                {
                    nameLabel.text = "Cost Info:";
                    costLabel.text = message;
                }
                
                // Setare iconiță Info (Exemplu: Verde deschis)
                iconElement.style.backgroundColor = Color.green; 

                break;
                
            case MessageType.Info:
                // Mesaje simple de sistem (ex: Item colectat)
                newInfoWindow.AddToClassList(INFO_CLASS);
                nameLabel.text = message;
                
                // Ascunde elementele secundare pentru mesajul simplu de info
                costLabel.style.display = DisplayStyle.None;
                separator.style.display = DisplayStyle.None;
                iconElement.style.display = DisplayStyle.None;
                break;

            default:
                // Ignoră sau tratează alte tipuri
                Debug.Log($"Tip de notificare neprocesat de UIInfoWindow: {type}");
                return;
        }

        // 5. Adaugă la UI-ul principal
        _rootElement.Add(newInfoWindow);
        _currentInfoWindow = newInfoWindow;

        // 6. Pornește cronometrul de ștergere
        _removalCoroutine = StartCoroutine(RemoveInfoAfterDelay(newInfoWindow, DISPLAY_TIME));
    }

    private IEnumerator RemoveInfoAfterDelay(VisualElement element, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_currentInfoWindow == element)
        {
             _currentInfoWindow = null;
        }
        element.RemoveFromHierarchy();
    }
}