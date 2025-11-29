using UnityEngine;
using System.Collections.Generic;

// Această clasă nu este abstractă pentru a putea fi adăugată ca și componentă
// pe un GameObject (așa cum o face NewActionUIGenerator-ul nostru).
public class LightBonfireExecutor : AbstractActionExecutor
{
    // Câmp opțional, util pentru a verifica starea curentă a focului în jocul tău
    [Header("Starea Focului")]
    public bool isBonfireLit = false;
    
    [Header("Bonfire Visuals")]
    [Tooltip("Obiectul care conține efectul de particule al focului (Fire_Purple).")]
    public GameObject fireParticles; // Schimbat din fireParticles în fireParticles pentru a menține numele

    [Tooltip("Obiectul care conține lumina punctuală (Point Light).")]
    public GameObject pointLight; // NOU: Referința pentru Point Light

    // NOU: Referință la generatorul UI
    private NewActionUIGenerator uiGenerator;


    void Start()
    {
        // Încercăm să obținem referința la NewActionUIGenerator de pe același GameObject
        uiGenerator = GetComponent<NewActionUIGenerator>();
        if (uiGenerator == null)
        {
            Debug.LogError("[LightBonfireExecutor] Nu a fost găsit NewActionUIGenerator pe acest GameObject!");
        }


        // Încercăm să găsim referințele dacă nu au fost setate manual
        // Căutăm printre copiii acestui GameObject
        if (fireParticles == null)
        {
            Transform fireTransform = transform.Find("Fire_Purple");
            if (fireTransform != null) fireParticles = fireTransform.gameObject;
        }

        if (pointLight == null)
        {
            Transform lightTransform = transform.Find("Point Light");
            if (lightTransform != null) pointLight = lightTransform.gameObject;
        }

        // Ne asigurăm că ambele sunt dezactivate la începutul jocului
        if (fireParticles != null) fireParticles.SetActive(false);
        if (pointLight != null) pointLight.SetActive(false);
    }


    /// <summary>
    /// Logica de validare TOTALĂ.
    /// Verifică resursele (din AbstractActionExecutor) și condițiile specifice.
    /// </summary>
    public override bool CanExecuteAction()
    {
        // 1. Verifică resursele necesare folosind logica părintelui.
        if (!CanExecuteResourceCheck())
        {
            Debug.Log($"Nu pot executa '{actionRecipe?.actionName}'. Lipsesc resurse.");
            return false;
        }

        // 2. Verifică starea specifică (Nu poți aprinde un foc care e deja aprins)
        if (isBonfireLit)
        {
            Debug.Log("Focul este deja aprins!");
            return false;
        }
        
        // Dacă ambele condiții sunt îndeplinite:
        return true;
    }

    /// <summary>
    /// Metoda de execuție a acțiunii (logica de aprindere).
    /// </summary>
    public override void ExecuteAction()
    {
        // Ne asigurăm că acțiunea este executabilă.
        if (!CanExecuteAction())
        {
            return;
        }
        
        // 1. CONSUMUL RESURSELOR: Apelăm metoda părintelui!
        ConsumeRequiredResources();
        
        // 2. Logica jocului: Aprinde focul
        Debug.Log("--- Acțiunea Executată: APRINDE FOCUL! ---");
        isBonfireLit = true; 

        // 3. ACTUALIZARE VIZUALĂ: Activăm ambele elemente vizuale
        if (fireParticles != null)
        {
            // Activăm Fire_Purple (sistemul de particule)
            fireParticles.SetActive(true);
            Debug.Log("✅ Fire_Purple activat.");
        }

        if (pointLight != null)
        {
            // Activăm Point Light (lumina din jurul focului)
            pointLight.SetActive(true);
            Debug.Log("✅ Point Light activat.");
        }
        
        // NOU: Schimbă nivelul de acțiune la 1 (Foc Aprins)
        if (uiGenerator != null)
        {
            uiGenerator.SetActionLevel(1);
            Debug.Log("✅ UI level changed to 1 (Lit Bonfire actions).");
        }
        
        // TODO: Adaugă aici schimbarea modelului, sunet de foc, etc.
    }
}