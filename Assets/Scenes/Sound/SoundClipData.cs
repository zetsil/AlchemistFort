using UnityEngine;

// Permite crearea asset-ului direct din meniul "Assets/Create"
[CreateAssetMenu(fileName = "NewSoundEvent", menuName = "Audio/Sound Event Data")]
public class SoundClipData : ScriptableObject 
{
    // Numele este implicit cel al fișierului asset, dar îl păstrăm pentru debug
    [Tooltip("Numele evenimentului audio (Ex: 'AxeHit')")]
    public string soundName = "New Event";

    [Header("Variatie Pitch")]
    [Tooltip("Daca este activat, sunetul va avea un pitch random la fiecare redare.")]
    public bool useRandomPitch = false;
    [Range(0.1f, 2f)] public float minPitch = 0.9f;
    [Range(0.1f, 2f)] public float maxPitch = 1.1f;
    
    [Tooltip("Lista de sunete alternative (variante) pentru acest eveniment.")]
    public AudioClip[] clips;
    
    // Contorul Round-Robin este stocat pe asset, dar nu este expus
    [System.NonSerialized] 
    private int counter = 0; 

    /// <summary>
    /// Returnează următorul AudioClip din secvența Round-Robin.
    /// </summary>
    public AudioClip GetNextClip()
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning($"SoundClipData '{soundName}' nu are clipuri asignate.");
            return null;
        }

        // 1. Obținem clipul curent
        AudioClip clipToPlay = clips[counter];

        // 2. Incrementăm contorul și îl resetăm ciclic (Round-Robin)
        counter = (counter + 1) % clips.Length;

        return clipToPlay;
    }

    /// <summary>
    /// Resetează contorul (util pentru situații specifice).
    /// </summary>
    public void ResetCounter()
    {
        counter = 0;
    }
}