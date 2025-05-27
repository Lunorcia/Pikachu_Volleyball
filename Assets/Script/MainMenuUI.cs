using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject matchingPanel;
    [SerializeField] private GameObject privatePanel;
    [SerializeField] private TMP_InputField passwordInput;
    private string password;

    // Start is called before the first frame update
    void Start()
    {
        mainPanel.SetActive(true);
        matchingPanel.SetActive(false);
        privatePanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartOnlineMatch()
    {
        Debug.Log("線上配對模式");
        mainPanel.SetActive(false);
        matchingPanel.SetActive(true);
        NetworkManager.Instance.isPublicMatch = true;

        // start matching
        NetworkManager.Instance.Connect();
        //if (PhotonNetwork.NetworkClientState != ClientState.JoinedLobby)
        //{
        //    NetworkManager.Instance.Connect();
        //}
        //else {
        //    PhotonNetwork.JoinRandomRoom();
        //}
    }

    public void StartPrivateMatch()
    {
        Debug.Log("私人對戰模式");
        mainPanel.SetActive(false);
        privatePanel.SetActive(true);
        NetworkManager.Instance.isPublicMatch = false;
        passwordInput.text = string.Empty;
    }

    public void ConfirmPrivateCode()
    {
        password = FindObjectOfType<MainMenuUI>().passwordInput.text.Trim();

        if (string.IsNullOrEmpty(password))
        {
            Debug.LogError("密碼不能為空");
            return;
        }
        Debug.Log("Linking password: " + passwordInput.text);

        privatePanel.SetActive(false);
        matchingPanel.SetActive(true);

        // start matching 
        NetworkManager.Instance.Connect();
    }

    public string GetPwd()
    {
        return password;
    }

    public void StartAIMatch()
    {
        Debug.Log("電腦對戰模式");
    }

    public void ExitGame()
    {
        Debug.Log("離開遊戲");
        Application.Quit();
    }

}
