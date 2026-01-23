using UnityEngine;

public class MineEntrance : MonoBehaviour
{
    void OnMouseOver()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E pressed on MineEntrance");
            MineGameManager.Instance.EnterMine();
        }
    }
}
