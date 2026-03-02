using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EntityHealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private CanvasGroup canvasGroup; // Adaugă un CanvasGroup pe Canvas-ul din Prefab
    [SerializeField] private Vector3 offset = new Vector3(0, 2.5f, 0);

    private Transform target;
    private Transform cam;
    private Coroutine hideCoroutine;

    void Awake()
    {
        cam = Camera.main.transform;
        if (canvasGroup != null) canvasGroup.alpha = 0; // Invizibil la început
    }

    public void Setup(Transform entityTransform, int maxHealth)
    {
        target = entityTransform;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;

        // 1. Calculăm poziția dorită în spațiul lumii (World Space) 
        // înainte de a desprinde obiectul. 
        // Asta "îngheață" locul unde l-ai pus tu în Editor.
        Vector3 worldSpawnPos = transform.position;

        // 2. Desprindem HealthBar-ul de părinte (WallComplete)
        // Astfel, nu mai este afectat de scala de 99.88 sau de rotație.
        transform.SetParent(null);

        // 3. Resetăm scala la una normală (0.01 sau cât era inițial în Prefab)
        // Deoarece nu mai e copil, scala nu mai este multiplicată cu 100.
        transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        // 4. Calculăm offset-ul real față de poziția de bază a entității
        // Folosim poziția world de la pasul 1 minus poziția inamicului.
        offset = worldSpawnPos - target.position;
    }

    public void UpdateHealthBar(int currentHealth)
    {
        healthSlider.value = currentHealth;


        if (currentHealth <= 0)
        {
            DestroyHealthBar();
            return; // Ieșim din metodă, nu mai are sens să facem Show()
        }

        // Când viața se schimbă, afișăm bara și resetăm timer-ul
        Show();
    }

    private void Show()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1;

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(3.5f)); // Cele 3-4 secunde cerute
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Fade out opțional
        float duration = 0.5f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(1, 0, elapsed / duration);
            yield return null;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Actualizăm poziția în fiecare cadru pentru a urmări ținta
        transform.position = target.position + offset;

        // Billboard (să se uite la cameră)
        transform.LookAt(transform.position + cam.forward);
    }
    

    private void DestroyHealthBar()
    {
        // Oprim corutina de hide dacă rulează, ca să nu încerce să modifice alpha pe un obiect distrus
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        
        // Distrugem obiectul barei de viață (cel de pe care rulează acest script)
        Destroy(gameObject);
    }
}