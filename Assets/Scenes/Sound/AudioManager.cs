using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))] 
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Configurație Clipuri Audio (Scriptable Objects)")]
    [Tooltip("Trageți aici fișierele ScriptableObject de tip SoundClipData.")]
    public SoundClipData[] soundEvents;

    // Dicționar pentru căutare rapidă bazată pe numele evenimentului
    private Dictionary<string, SoundClipData> soundMap = new Dictionary<string, SoundClipData>();

    private AudioSource audioSource; 
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            InitializeManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    

    private void OnEnable()
    {
        GlobalEvents.OnPlaySound += PlaySound;
    }

    // Când componenta este dezactivată/distrusă, ne dezabonăm
    private void OnDisable()
    {
        GlobalEvents.OnPlaySound -= PlaySound;
    }

    private void InitializeManager()
    {
        audioSource = GetComponent<AudioSource>();

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


    public void PlaySound(string soundName)
    {
        if (soundMap.TryGetValue(soundName, out SoundClipData soundData))
        {
            AudioClip clip = soundData.GetNextClip();

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}