using UnityEngine;
using TMPro;

public class MineTimerUI : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    void Start()
    {
        MineGameManager.Instance.StartMine();
    }

    void Update()
    {
        timerText.text =
            Mathf.Ceil(MineGameManager.Instance.GetTimeLeft()).ToString();
    }
}
