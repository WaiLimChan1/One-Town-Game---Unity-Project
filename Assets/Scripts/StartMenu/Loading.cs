using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    [SerializeField] GameObject StartMenu;

    [SerializeField] TMP_Text DescriptionText;
    [SerializeField] TMP_Text LoadingText;
    [SerializeField] Button CancelButton;
    private NetworkRunnerController networkRunnerController;
    private float MaxWaitTime = 100;
    private float timeCounter;

    void DetermineDescriptionText()
    {
        DescriptionText.text = networkRunnerController.LocalPlayerName + " is ";

        GameMode LocalGameMode = networkRunnerController.LocalGameMode;
        if (LocalGameMode == GameMode.Client) DescriptionText.text += "Joining Room " + networkRunnerController.RoomCode;
        else if (LocalGameMode == GameMode.Host) DescriptionText.text += "Hosting Room " + networkRunnerController.RoomCode;
        else if (LocalGameMode == GameMode.AutoHostOrClient) DescriptionText.text += "Joining Random Room";
    }

    private void OnStartedRunnerConnection()
    {
        this.gameObject.SetActive(true);
        DetermineDescriptionText();

        StartMenu.SetActive(false);

        timeCounter = MaxWaitTime;
    }

    private void OnPlayerJoinedSuccessfully()
    {
        this.gameObject.SetActive(false);
    }

    private void Start()
    {
        networkRunnerController = GlobalManagers.Instance.NetworkRunnerController;
        networkRunnerController.OnStartedRunnerConnection += OnStartedRunnerConnection;
        networkRunnerController.OnPlayerJoinedSuccessfully += OnPlayerJoinedSuccessfully;
        CancelButton.onClick.AddListener(networkRunnerController.ShutDownRunner);
        this.gameObject.SetActive(false);
    }

    private void Update()
    {
        LoadingText.text = "Loading... (" + (int)timeCounter + "s)";
        timeCounter -= Time.deltaTime;
        if (timeCounter < 0) timeCounter = 0;
        if (timeCounter <= 0) { CancelButton.onClick.Invoke(); }
    }

    private void OnDestroy()
    {
        networkRunnerController.OnStartedRunnerConnection -= OnStartedRunnerConnection;
        networkRunnerController.OnPlayerJoinedSuccessfully -= OnPlayerJoinedSuccessfully;
    }
}
