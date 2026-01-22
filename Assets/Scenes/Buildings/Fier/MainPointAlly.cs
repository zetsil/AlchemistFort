using UnityEngine;

public class MainPointAlly : AllyEntity
{
    [Header("Main Point Settings")]
    [Tooltip("Mesaj personalizat când baza este distrusă")]
    public string defeatMessage = "The Main Core has been destroyed!";

    protected override void Start()
    {
        base.Start();
        // Aici poți adăuga logică extra la început, 
        // de exemplu să înregistrezi acest punct într-un radar
    }

    protected override void Die()
    {
        Debug.Log($"<color=red><b>GAME OVER:</b></color> {defeatMessage}");

        // 1. Apelăm logica de bază din AllyEntity (cea care spawnează ruina/brokenBuildingPrefab)
        // Folosim base.Die() pentru că AllyEntity (sau părintele Entity) se ocupă de DropLoot()
        base.Die();

        // 2. Trimitem semnalul global de moarte
        // Deoarece HUD-ul ascultă deja OnPlayerDeath pentru a afișa ecranul roșu,
        // distrugerea bazei va avea același efect vizual.
        GlobalEvents.NotifyPlayerDeath();

        // 3. Opțional: Putem trimite o notificare specifică înainte de ecranul final
        GlobalEvents.RequestNotification(defeatMessage, MessageType.Alert);
        
        // Dacă vrei să oprești și mișcarea jucătorului când baza moare:
        var player = GameObject.FindWithTag("Player")?.GetComponent<FirstPersonController>();
        if (player != null)
        {
            player.playerCanMove = false;
            player.cameraCanMove = false;
        }
    }
}