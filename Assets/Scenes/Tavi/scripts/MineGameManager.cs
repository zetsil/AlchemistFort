using UnityEngine;
using UnityEngine.SceneManagement;

public class MineGameManager : MonoBehaviour
{
    public static MineGameManager Instance;

    public float mineDuration = 10f;

    float currentTime;
    bool mineActive = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void EnterMine()
    {
        Debug.Log("ENTER MINE");
        SceneManager.LoadScene("Mine");
    }

    public void StartMine()
    {
        Debug.Log("START MINE TIMER");
        currentTime = mineDuration;
        mineActive = true;
    }

    void Update()
    {
        if (!mineActive) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            Debug.Log("TIME OVER");
            ExitMine();
        }
    }

    public float GetTimeLeft()
    {
        return currentTime;
    }

    public void ExitMine()
    {
        Debug.Log("EXIT MINE");
        mineActive = false;
        SceneManager.LoadScene("Main");
    }
}
