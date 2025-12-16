using UnityEngine;
using IngameDebugConsole;
// Asigură-te că ai namespace-ul corect pentru GameStateManager

// Clasa trebuie să fie publică (dacă e statică, e ok)
public static class GameCommands 
{
    [ConsoleMethod( "time.skip", "Trece imediat la următoarea stare a ciclului Zi/Noapte." )]
    public static void SkipTimeState() // Comanda nu primește parametri, deci nu are argumente
    {
        if (GameStateManager.Instance == null)
        {
            // Debug.LogError va afișa mesajul în consola in-game.
            Debug.LogError("time.skip: GameStateManager.Instance nu a fost găsit.");
            return;
        }

        GameStateManager.Instance.SkipTime();
        
        // Output-ul acestei comenzi este vizibil în consolă.
        Debug.Log("Ciclul de timp a fost forțat să treacă la următoarea stare.");
    }
}