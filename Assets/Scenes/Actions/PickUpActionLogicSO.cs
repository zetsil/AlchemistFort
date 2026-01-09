using UnityEngine;

[CreateAssetMenu(fileName = "PickUpAction", menuName = "Actions/PickUp Logic")]
public class PickUpActionLogicSO : AbstractActionLogicSO
{
    public override bool ExecuteAction(ActionRecipeSO recipe, GameObject initiator)
    {
        // Resetăm starea acțiunii
        isFinished = false;

        if (initiator == null)
        {
            Debug.LogError("PickUpAction: Initiatorul este null!");
            return false;
        }

        // 1. Încercăm să obținem componenta ItemPickup de pe obiectul interacționat
        ItemPickup pickup = initiator.GetComponent<ItemPickup>();

        // 2. Verificăm validitatea
        if (pickup != null && pickup.itemData != null)
        {
            // 3. Executăm logica de colectare existentă pe obiect
            // Aceasta va gestiona adăugarea în inventar și Destroy()
            pickup.Collect();

            // Marcăm acțiunea ca finalizată cu succes
            isFinished = true;
            return true;
        }
        else
        {
            Debug.LogError($"PickUpAction: Obiectul {initiator.name} nu are un ItemPickup valid!");
            return false;
        }
    }
}