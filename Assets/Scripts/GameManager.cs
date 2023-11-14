using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Abertay.Analytics;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; // set up singleton
            DontDestroyOnLoad(Instance);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void SendGameData(string eventname, Dictionary<string, object> data)
    {
        AnalyticsManager.SendCustomEvent(eventname, data);
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitApplication()
    {
        print("Quit");
        Application.Quit();
    }
}
