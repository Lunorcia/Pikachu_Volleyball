using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;
    public bool isPublicMatch = false;

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

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
        Debug.Log("�s�W Master Server �ǳƥ[�J Lobby");
    }

    // join lobby
    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");

        if (isPublicMatch)
        {
            PhotonNetwork.JoinRandomRoom();
        }
        else 
        {
            string roomName = FindObjectOfType<MainMenuUI>().GetPwd();
            if (!string.IsNullOrEmpty(roomName))
            {
                Debug.Log($"[PrivateMatch] JoinOrCreateRoom: {roomName}");
                RoomOptions options = new RoomOptions { MaxPlayers = 2 };
                PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
            }
            else
            {
                Debug.Log("�䤣������ж�");
            }
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No available room. Creating new one");
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = 2 });
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player entered room: {newPlayer.NickName}, current count = {PhotonNetwork.CurrentRoom.PlayerCount}");

        // �ж��� 2 �H�}�l�C��
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            PhotonNetwork.LoadLevel("GameScene"); // load scene
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
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
        string prefabName = PhotonNetwork.IsMasterClient ? "PlayerL" : "PlayerR";
        PhotonNetwork.Instantiate(prefabName, Vector3.zero, Quaternion.identity); // self character
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room");
        Debug.Log("���\���}�ж��A�{�b�i�H���s�[�J");
        SceneManager.LoadScene("MainPage");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (SceneManager.GetActiveScene().name == "GameScene" && GameManager.Instance != null)
        {
            if (!GameManager.Instance.IsGameOver())
            {
                Debug.Log($"Player {otherPlayer.NickName} �_�u���}�ж�"); // opponent leave
                GameManager.Instance.OpponentLeftGame();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
