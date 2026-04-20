using UnityEngine;

public class WeaponGroundScanner : MonoBehaviour
{
    [Header("Setări Vizuale")]
    [SerializeField] private GameObject dustPrefab;   // Prefab-ul cu Particle System-ul de praf
    [SerializeField] private LayerMask groundLayer;    // Setează aici layer-ul "Ground" sau "Default"
    
    [Header("Setări Scanare")]
    [SerializeField] private float rayDistance = 0.8f; // Cât de lungă e raza (ajustează în funcție de mărimea armei)
    [SerializeField] private float verticalOffset = 0.05f; // Mică distanță deasupra solului pentru a evita clipping-ul

    private bool _isScanning = false;
    private bool _hasSpawnedThisAttack = false;

    /// <summary>
    /// Pornit din ToolController prin Animation Event
    /// </summary>
    public void StartScanning()
    {
        _isScanning = true;
        _hasSpawnedThisAttack = false;
    }

    /// <summary>
    /// Oprit din ToolController sau automat la finalul animației
    /// </summary>
    public void StopScanning()
    {
        _isScanning = false;
    }

    void Update()
    {
        // Scanăm doar dacă animația ne-a dat voie și dacă nu am dat deja un praf la acest swing
        if (!_isScanning || _hasSpawnedThisAttack) return;

        // Tragem o rază perpendiculară pe pământ (Vector3.down în World Space)
        RaycastHit hit;
        
        // Debug vizual în editor: desenează o linie roșie să vezi unde scanează
        Debug.DrawRay(transform.position, Vector3.down * rayDistance, Color.red);

        if (Physics.Raycast(transform.position, Vector3.down, out hit, rayDistance, groundLayer))
        {
            SpawnDustEffect(hit);
            _hasSpawnedThisAttack = true; 
            
            // OPȚIONAL: Putem opri scanarea imediat ce am găsit pământul
            _isScanning = false;
        }
    }

    private void SpawnDustEffect(RaycastHit hit)
    {
        if (dustPrefab == null) return;

        // Calculăm rotația: aliniem particula cu planul solului (hit.normal)
        Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
        
        // Poziția este punctul de impact plus un mic offset vertical ca să nu se îngroape particulele
        Vector3 spawnPosition = hit.point + (hit.normal * verticalOffset);

        GameObject dustInstance = Instantiate(dustPrefab, spawnPosition, spawnRotation);

        // Curățenie în ierarhie: distrugem obiectul după ce termină particulele (ex: 2 secunde)
        Destroy(dustInstance, 2f);
    }
}