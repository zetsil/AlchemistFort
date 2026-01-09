using UnityEngine;
using System.Collections.Generic;

public class ParticleMap : MonoBehaviour
{
    [System.Serializable]
    public struct ParticleEntry
    {
        public string key;
        public GameObject prefab;
    }

    [SerializeField] private List<ParticleEntry> particleEntries;
    private Dictionary<string, GameObject> particleDictionary;

    private void Awake()
    {
        particleDictionary = new Dictionary<string, GameObject>();
        foreach (var entry in particleEntries)
        {
            if (!particleDictionary.ContainsKey(entry.key))
                particleDictionary.Add(entry.key, entry.prefab);
        }
    }

    private void OnEnable()
    {
        // Ne abonăm la noul eveniment cu coordonate
        GlobalEvents.OnParticleEffectRequested += HandleParticleRequest;
    }

    private void OnDisable()
    {
        GlobalEvents.OnParticleEffectRequested -= HandleParticleRequest;
    }

    private void HandleParticleRequest(string effectName, Vector3 spawnPosition)
    {
        if (particleDictionary.TryGetValue(effectName, out GameObject prefab))
        {
            SpawnParticle(prefab, spawnPosition);
        }
    }

    private void SpawnParticle(GameObject prefab, Vector3 position)
    {
        // Instanțiem la poziția precisă primită (ex: punctul de impact)
        GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        
        // Opțional: Dacă vrei ca particulele să fie "copil" al acestui manager
        // effect.transform.SetParent(this.transform);

        Destroy(effect, 2f); // Curățenie automată
    }
}