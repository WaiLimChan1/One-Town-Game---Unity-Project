using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    public static GameOver instance;
    [SerializeField] public NetworkedPlayer NetworkedPlayer;

    [SerializeField] private TMP_Text SurvivalTime;
    [SerializeField] private Button Exit;

    public void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void InitializeSurvivalTime()
    {
        SurvivalTime.text = "Survival Time: " + NetworkedPlayer.GetSurvivalTime() + " Seconds";
    }

    public void TurnOn()
    {
        instance.gameObject.SetActive(true);
        InitializeSurvivalTime();
        Exit.onClick.AddListener(GlobalManagers.Instance.NetworkRunnerController.ShutDownRunner);
        Time.timeScale = 1;
    }
}
