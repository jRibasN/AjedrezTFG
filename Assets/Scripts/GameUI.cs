using System;
using TMPro;
using UnityEngine;

public enum CameraAngle{
    menu = 0,
    whiteTeam = 1,
    blackTeam = 2
}

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { set; get; }

    public Server server;
    public Client client;

    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField adressInput;
    [SerializeField] private GameObject[] cameraAngles;

    public Action<bool> SetLocalGame;

    public Action<bool> SetComputerGame;

    private void Awake()
    {
        Instance = this;
        RegisterEvents();
    }

    // Cameras
    public void ChangeCamera(CameraAngle index){
        for (int i = 0; i < cameraAngles.Length; i++)
            cameraAngles[i].SetActive(false);

        cameraAngles[(int)index].SetActive(true);
    }

    // Buttons
    public void OnLocalGameButton(){
        menuAnimator.SetTrigger("InGameMenu");
        SetLocalGame?.Invoke(true);
        SetComputerGame?.Invoke(false);
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
    }

    public void OnComputerGameButton(){
        menuAnimator.SetTrigger("InGameMenu");
        SetComputerGame?.Invoke(true);
        SetLocalGame?.Invoke(false);
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
    }

    public void OnOnlineGameButton(){
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnOnlineHostButton(){
        SetLocalGame?.Invoke(false);
        SetComputerGame?.Invoke(false);
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
        menuAnimator.SetTrigger("HostMenu");
    }

    public void OnOnlineConnectButton(){
        SetLocalGame?.Invoke(false);
        SetComputerGame?.Invoke(false);
        client.Init(adressInput.text, 8007);
    }

    public void OnOnlineBackButton(){
        menuAnimator.SetTrigger("StartMenu");
    }

    public void OnHostBackButton(){
        server.ShutDown();
        client.ShutDown();
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnLeaveFromGameMenu(){
        ChangeCamera(CameraAngle.menu);
        menuAnimator.SetTrigger("StartMenu");
    }

    #region
    private void RegisterEvents(){
        NetUtility.C_START_GAME += OnStartGameClient;
    }

    private void UnregisterEvents(){
        NetUtility.C_START_GAME -= OnStartGameClient;
    }

    private void OnStartGameClient(NetMessage message)
    {
        menuAnimator.SetTrigger("InGameMenu");
    }
    #endregion
}
