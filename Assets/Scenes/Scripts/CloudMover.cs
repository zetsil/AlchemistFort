using UnityEngine;

/// <summary>
/// Muta norii la o viteza constanta si ii reseteaza pozitia cand ies din raza de vizibilitate.
/// </summary>
public class CloudMover : MonoBehaviour
{
    [Header("Setări Mișcare")]
    [Tooltip("Viteza norului (valoare mică pentru mișcare naturală).")]
    public float speed = 0.5f; // Recomandat: 0.1 la 1.0

    [Header("Setări Reciclare")]
    [Tooltip("Distanța de la care norul este resetat (Ex: -100f pe axa X).")]
    public float resetDistance = -100f; 

    [Tooltip("Poziția nouă la care este mutat norul după resetare (Ex: 100f pe axa X).")]
    public float resetPosition = 100f;

    private void Update()
    {
        // 1. Mișcarea constantă
        // Folosim Space.World pentru a ne asigura că se mișcă pe axa X a lumii, nu a norului.
        // Folosim Time.deltaTime pentru viteză constantă indiferent de FPS.
        transform.Translate(Vector3.left * speed * Time.deltaTime, Space.World);

        // 2. Logica de Reciclare (Lopping)
        // Verificăm dacă norul a ajuns la poziția de resetare (ex: a ieșit din stânga ecranului)
        if (transform.position.x < resetDistance)
        {
            ResetCloudPosition();
        }
    }

    private void ResetCloudPosition()
    {
        // Mutăm norul înapoi la poziția de pornire, de unde va reîncepe să se miște
        Vector3 newPos = transform.position;
        newPos.x = resetPosition; 
        
        // Bonus: Adăugăm o mică variație pe Y și Z pentru a părea mai puțin rigid
        newPos.y += Random.Range(-5f, 5f);
        newPos.z += Random.Range(-5f, 5f);
        
        transform.position = newPos;
        
        // Optional: Putem schimba și viteza puțin, dacă vrei variație
        // speed = Random.Range(0.4f, 0.8f);
    }
}