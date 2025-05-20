using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    void Awake()
    {
        // Ensure only exist a NetworkManager in different scene
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

    public void Connect()
    {
        PhotonNetwork.ConnectUsingSettings();
        Debug.Log("Connecting to Photon");
    }

    public void Disconnect()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            Debug.Log("Disconnected from Photon");
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server");
        PhotonNetwork.JoinLobby();
    }

    // join lobby
    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No available room. Creating new one");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");

        // Switch to the GameScene
        if (SceneManager.GetActiveScene().name != "GameScene")
        {
            SceneManager.LoadScene("GameScene");
        }
        else
        {
            SpawnPlayer();
        }
    }

    protected new void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected new void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene" && PhotonNetwork.InRoom)
        {
            SpawnPlayer();
        }
    }

    void SpawnPlayer()
    {
        PhotonNetwork.Instantiate("PlayerPrefab", Vector3.zero, Quaternion.identity);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
