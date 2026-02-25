using UnityEngine;

public class LevitateEffect : MonoBehaviour
{
    [Header("Setări Mișcare")]
    [Tooltip("Cât de sus și de jos se mișcă sfera")]
    public float amplitude = 0.5f; 
    [Tooltip("Cât de repede oscilează")]
    public float frequency = 1f;

    [Header("Setări Rotație")]
    [Tooltip("Viteza de rotație pe fiecare axă")]
    public Vector3 rotationSpeed = new Vector3(0, 50, 0);

    private Vector3 startPosition;

    void Start()
    {
        // Salvăm poziția inițială ca punct de referință
        startPosition = transform.position;
    }

    void Update()
    {
        // 1. Calculăm noua poziție pe axa Y folosind Sinus
        // Formula: Pos = StartPos + Sin(Timp * Viteză) * Amplitudine
        float newY = startPosition.y + Mathf.Sin(Time.time * frequency) * amplitude;
        
        // Aplicăm noua poziție
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // 2. Adăugăm și o rotație ușoară pentru a face focul să pară viu
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}