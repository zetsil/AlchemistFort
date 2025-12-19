using UnityEngine;
using System.Collections.Generic;
using System.Collections; // Necesar pentru Coroutines

[RequireComponent(typeof(AudioSource))] 
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Configurație Clipuri Audio (Scriptable Objects)")]
    [Tooltip("Trageți aici fișierele ScriptableObject de tip SoundClipData.")]
    public SoundClipData[] soundEvents;

    [Header("Sunete de Fundal (Ziuă/Noapte)")]
    [Tooltip("Clipuri audio pentru fundalul de ZIUĂ (ex: păsări, vânt).")]
    public AudioClip[] dayBackgroundSounds; 
    
    [Tooltip("Clipuri audio pentru fundalul de NOAPTE (ex: greieri, bufnițe).")]
    public AudioClip[] nightBackgroundSounds; 

    [Tooltip("Intervalul minim/maxim de așteptare între sunetele de fundal (secunde).")]
    public Vector2 backgroundSoundInterval = new Vector2(10f, 30f);

    [Header("Setări Timp Joc")]
    [Tooltip("Durata unei zile complete în secunde reale (300 de secunde = 5 minute).")]
    public float dayDurationSeconds = 300f; 

    // Dicționar pentru căutare rapidă bazată pe numele evenimentului
    private Dictionary<string, SoundClipData> soundMap = new Dictionary<string, SoundClipData>();

    private AudioSource effectsAudioSource; 
    private AudioSource backgroundAudioSource; // AudioSource dedicat sunetelor de fundal

    // Variabilă care simulează timpul jocului (0.0 la 1.0)
    // 0.0-0.5 ar putea fi Ziua, 0.5-1.0 ar putea fi Noaptea
    private float currentTime = 0f;
    private const float DAY_THRESHOLD = 0.5f; // Până la 50% este Ziua

    private bool IsDayTime => currentTime < DAY_THRESHOLD;
    
    // ====================================================================================
    // METODE UNITY LIFECYCLE
    // ====================================================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Poate fi util să nu se distrugă la schimbarea scenei
            InitializeManager();
            StartCoroutine(BackgroundSoundRoutine()); // Pornim rutina de sunete de fundal
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        // Simularea trecerii timpului
        // Această logică ar trebui să fie de obicei în clasa de GameController/TimeManager
        currentTime += Time.deltaTime / dayDurationSeconds;
        if (currentTime >= 1f)
        {
            currentTime = 0f; // Resetăm la începutul unei noi zile
        }
    }
    
    private void OnEnable()
    {
        GlobalEvents.OnPlaySound += PlaySound;
    }

    private void OnDisable()
    {
        GlobalEvents.OnPlaySound -= PlaySound;
    }

    // ====================================================================================
    // INIȚIALIZARE
    // ====================================================================================

    private void InitializeManager()
    {
        // AudioSource-ul inițial este folosit pentru efecte
        effectsAudioSource = GetComponent<AudioSource>();
        effectsAudioSource.loop = false; // Ne asigurăm că nu face loop

        // Adăugăm un AudioSource nou pentru sunetele de fundal intermitente
        backgroundAudioSource = gameObject.AddComponent<AudioSource>();
        backgroundAudioSource.loop = false;
        backgroundAudioSource.spatialBlend = 0f; // 2D Sound

        foreach (var soundData in soundEvents)
        {
            if (soundMap.ContainsKey(soundData.soundName))
            {
                Debug.LogWarning($"AudioManager: Evenimentul audio '{soundData.soundName}' este duplicat!");
                continue;
            }
            soundMap.Add(soundData.soundName, soundData);
        }

        Debug.Log($"AudioManager: Manager inițializat cu {soundMap.Count} evenimente audio ScriptableObject.");
    }

    // ====================================================================================
    // LOGICĂ SUNETE DE FUNDAL (BACKGROUND)
    // ====================================================================================

    private IEnumerator BackgroundSoundRoutine()
    {
        while (true)
        {
            // 1. Așteptăm un interval random
            float waitTime = Random.Range(backgroundSoundInterval.x, backgroundSoundInterval.y);
            yield return new WaitForSeconds(waitTime);

            // 2. Verificăm dacă un sunet de fundal rulează deja
            if (backgroundAudioSource.isPlaying)
            {
                // Dacă rulează, sărim peste această iterație și așteptăm din nou
                continue; 
            }

            // 3. Alegem clipul audio bazat pe timpul zilei/nopții
            AudioClip clipToPlay = GetAmbientClipBasedOnTime();

            if (clipToPlay != null)
            {
                backgroundAudioSource.PlayOneShot(clipToPlay);
            }
        }
    }

    private AudioClip GetAmbientClipBasedOnTime()
    {
        AudioClip[] targetClips = IsDayTime ? dayBackgroundSounds : nightBackgroundSounds;

        if (targetClips == null || targetClips.Length == 0)
        {
            Debug.LogWarning($"AudioManager: Nu există clipuri audio definite pentru {(IsDayTime ? "ziua" : "noaptea")}.");
            return null;
        }

        // Returnează un clip aleatoriu din lista relevantă
        return targetClips[Random.Range(0, targetClips.Length)];
    }

    // ====================================================================================
    // LOGICĂ SUNETE EFECTE (EFFECTS)
    // ====================================================================================

    public void PlaySound(string soundName)
    {
        if (soundMap.TryGetValue(soundName, out SoundClipData soundData))
        {
            AudioClip clip = soundData.GetNextClip();

            if (clip != null)
            {
                // Folosim AudioSource-ul dedicat efectelor
                effectsAudioSource.PlayOneShot(clip);
            }
        }
    }
}