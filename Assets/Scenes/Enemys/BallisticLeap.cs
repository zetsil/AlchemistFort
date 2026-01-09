using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BallisticLeap : MonoBehaviour
{
    [Header("Setări Traiectorie")]
    public float leapHeight = 5f;
    public float gravity = -9.81f;

    [Header("Slingshot Feel")]
    public float windupTime = 0.2f;
    public float arcExaggeration = 1.3f;
    public float shakeIntensity = 0.05f;

    [Header("Rotire în aer")]
    public bool spinEnabled = true;
    public float spinTorque = 10f;

    [Header("Impact")]
    public float impactDamage = 3f;
    public float destroyDelay = 3f;

    [Header("Configurare")]
    public bool launchOnStart = true;
    public string targetTag = "Player";

    private Rigidbody rb;
    private Transform target;
    private bool hasHit = false;

    // 🔒 previne multiple Destroy()
    private bool destroyScheduled = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = true;
    }

    void Start()
    {
        GameObject targetObj = GameObject.FindGameObjectWithTag(targetTag);
        if (targetObj != null) target = targetObj.transform;

        if (launchOnStart)
            StartCoroutine(SlingshotLaunch());
    }

    // ✅ distrugere sigură (o singură dată)
    void ScheduleDestroy(float delay)
    {
        if (destroyScheduled) return;

        destroyScheduled = true;
        Destroy(gameObject, delay);
    }

    IEnumerator SlingshotLaunch()
    {
        if (target == null)
        {
            ScheduleDestroy(destroyDelay);
            yield break;
        }

        // 🔥 mic tremurat înainte de lansare (simte ca prastia intinsa)
        Vector3 originalPos = transform.position;
        float t = 0f;

        while (t < windupTime)
        {
            t += Time.deltaTime;

            transform.position = originalPos +
                (Vector3)Random.insideUnitCircle * shakeIntensity;

            yield return null;
        }

        transform.position = originalPos;

        // 🚀 lansează
        Launch();

        // 🌀 rotire în aer
        if (spinEnabled)
            rb.AddTorque(Random.onUnitSphere * spinTorque, ForceMode.Impulse);
    }

    public void Launch()
    {
        rb.linearVelocity = CalculateBallisticVelocity();

        // safety timer
        ScheduleDestroy(10f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Ignoră Triggerele (zone de detecție, nu corpuri solide)
        if (collision.collider.isTrigger) return;

        // 2. Ignoră alte proiectile (să nu se lovească între ele în aer)
        if (collision.gameObject.GetComponent<BallisticLeap>() != null) return;

        if (hasHit) return;

        // 3. Ignoră "Echipa" (creatorul sau alte obiecte cu același tag, ex: "Enemy")
        // Verificăm dacă obiectul lovit este părintele sau are același tag ca sursa
        if (collision.transform == transform.parent || collision.gameObject.CompareTag(gameObject.tag)) 
        {
            return;
        }

        // Dacă a trecut de toate filtrele, înregistrăm impactul
        hasHit = true;

        if (collision.gameObject.TryGetComponent<Entity>(out var victim))
        {
            // Aplicăm damage-ul folosind tipul de atac Claw
            victim.TakeDamage(impactDamage, ToolType.Claw);

            // Distrugere instantanee la impactul cu o entitate
            ScheduleDestroy(0f);
        }
        else
        {
            // A lovit un obiect static (pământ, zid) - distrugere după un delay (ex: 3s)
            ScheduleDestroy(destroyDelay);
        }
    }

    Vector3 CalculateBallisticVelocity()
    {
        Vector3 startPos = rb.position;
        Vector3 endPos = target.position;

        float displacementY = endPos.y - startPos.y;
        Vector3 displacementXZ = new Vector3(endPos.x - startPos.x, 0, endPos.z - startPos.z);

        // ⛰️ arc exagerat pentru feeling de praștie
        float exaggeratedHeight = leapHeight * arcExaggeration;

        float actualH = Mathf.Max(exaggeratedHeight, displacementY + 0.5f);

        Vector3 velocityY = Vector3.up * Mathf.Sqrt(-2 * gravity * actualH);

        float timeUp = Mathf.Sqrt(-2 * actualH / gravity);
        float timeDown = Mathf.Sqrt(2 * (displacementY - actualH) / gravity);
        float totalTime = timeUp + timeDown;

        Vector3 velocityXZ = displacementXZ / totalTime;

        return velocityXZ + velocityY;
    }
}
