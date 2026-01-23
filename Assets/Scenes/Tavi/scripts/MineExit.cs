using UnityEngine;

public class MineExit : MonoBehaviour
{
    void OnMouseOver()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E pressed on Ladder");
            MineGameManager.Instance.ExitMine();
        }
    }
}
