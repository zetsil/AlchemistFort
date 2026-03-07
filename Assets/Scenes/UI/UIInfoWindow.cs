using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class UIInfoWindow : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _rootElement;
    
    // Referința către elementul care există deja în UI
    private VisualElement _infoWindowElement;

    // Referințe către sub-elemente
    private VisualElement _iconElement;
    private VisualElement _separator;
    private Label _nameLabel;
    private Label _costLabel;

    private const float DISPLAY_TIME = 4f;
    private const string INFO_CLASS = "info-window";
    private const string ALERT_CLASS = "alert-window";

    private Coroutine _removalCoroutine;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null) return;

        _rootElement = _uiDocument.rootVisualElement;

        // Căutăm elementul în document (trebuie să aibă acest nume în UI Builder)
        // Dacă ai pus template-ul direct, caută numele părintelui din template
        _infoWindowElement = _rootElement.Q<VisualElement>("InfoWindowRoot"); 

        if (_infoWindowElement != null)
        {
            // Găsim sub-elementele
            _iconElement = _infoWindowElement.Q<VisualElement>("Icon");
            _separator = _infoWindowElement.Q<VisualElement>("Separator");
            _nameLabel = _infoWindowElement.Q<Label>("BuildingNameLabel");
            _costLabel = _infoWindowElement.Q<Label>("CostLabel");

            // Îl ascundem la început
            _infoWindowElement.style.display = DisplayStyle.None;
        }
        else
        {
            Debug.LogError("Nu am găsit elementul 'InfoWindowRoot' în UIDocument! Verifică numele în UI Builder.");
        }
    }

    private void OnEnable()
    {
        GlobalEvents.OnNotificationRequested += DisplayInfo;
    }

    private void OnDisable()
    {
        GlobalEvents.OnNotificationRequested -= DisplayInfo;
    }

    private void DisplayInfo(string message, MessageType type)
    {
        if (_infoWindowElement == null) return;

        // Resetăm corutina dacă există una activă
        if (_removalCoroutine != null) StopCoroutine(_removalCoroutine);

        // 1. Resetăm vizibilitatea și clasele
        _infoWindowElement.RemoveFromClassList(INFO_CLASS);
        _infoWindowElement.RemoveFromClassList(ALERT_CLASS);
        
        _costLabel.style.display = DisplayStyle.Flex;
        _separator.style.display = DisplayStyle.Flex;
        _iconElement.style.display = DisplayStyle.Flex;

        // 2. Aplicăm logica de conținut
        switch (type)
        {
            case MessageType.Alert:
                _infoWindowElement.AddToClassList(ALERT_CLASS);
                _nameLabel.text = $"! {message}";
                _costLabel.style.display = DisplayStyle.None;
                _separator.style.display = DisplayStyle.None;
                _iconElement.style.backgroundColor = Color.red;
                break;

            case MessageType.ResourceNeeded:
                _infoWindowElement.AddToClassList(INFO_CLASS);
                string[] parts = message.Split('\n');
                if (parts.Length >= 2)
                {
                    _nameLabel.text = parts[0];
                    _costLabel.text = parts[1];
                }
                else
                {
                    _nameLabel.text = "Cost Info:";
                    _costLabel.text = message;
                }
                _iconElement.style.backgroundColor = Color.green;
                break;

            case MessageType.Info:
                _infoWindowElement.AddToClassList(INFO_CLASS);
                _nameLabel.text = message;
                _costLabel.style.display = DisplayStyle.None;
                _separator.style.display = DisplayStyle.None;
                _iconElement.style.display = DisplayStyle.None;
                break;
        }

        // 3. Afișăm elementul
        _infoWindowElement.style.display = DisplayStyle.Flex;

        // 4. Pornim cronometrul de ascundere
        _removalCoroutine = StartCoroutine(HideAfterDelay(DISPLAY_TIME));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        _infoWindowElement.style.display = DisplayStyle.None;
        _removalCoroutine = null;
    }
}