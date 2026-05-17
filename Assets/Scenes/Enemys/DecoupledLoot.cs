using UnityEngine;

public class DecoupledLoot : MonoBehaviour
{
    [Header("Physics Settings")]
    [SerializeField] private float mass = 1f;
    [Tooltip("Forța inițială de aruncare a armei când e scăpată")]
    [SerializeField] private Vector3 dropEjectionForce = new Vector3(0f, 2f, -1f); 

    /// <summary>
    /// Metoda apelată din scriptul inamicului când acesta moare.
    /// </summary>
    public void DecoupleAndDrop()
    {
        // 1. Rupem legătura cu mâna inamicului
        transform.SetParent(null);

        // 2. Ne asigurăm că are un Collider pentru a nu trece prin podea
        // Dacă are deja un collider dezactivat, îl activăm, altfel adăugăm un BoxCollider generic
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
        }
        col.enabled = true;

        // 3. Adăugăm fizică pentru a cădea
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.mass = mass;
        rb.isKinematic = false;
        rb.useGravity = true;

        // Opțional: Îi dăm un mic impuls armei ca să sară din mână realist
        // Direcția este relativă la rotația curentă a armei în momentul morții
        Vector3 worldForce = transform.TransformDirection(dropEjectionForce);
        rb.AddForce(worldForce, ForceMode.Impulse);
        
        // Îi dăm și puțină rotație aleatorie (spin) pentru realism
        rb.AddTorque(Random.insideUnitSphere * 5f, ForceMode.Impulse);

        // Schimbăm layer-ul dacă e necesar (ex: pentru a putea fi lovit ulterior de jucător)
        // gameObject.layer = LayerMask.NameToLayer("Lootable");
    }
}