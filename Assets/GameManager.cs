using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
// using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PlayerController;

public class GameManager : MonoBehaviourPun
{
    public Image skillIconL_J;
    public Image skillIconL_K;
    public Image skillIconR_J;
    public Image skillIconR_K;
    public Color lockedColor = Color.gray;
    public Color availableColor = Color.white;
    public Color activeColor = Color.red;
    [SerializeField] private Sprite jAvailableSprite;
    [SerializeField] private Sprite jActiveSprite;
    [SerializeField] private Sprite jLockedSprite;

    [SerializeField] private Sprite kAvailableSprite;
    [SerializeField] private Sprite kActiveSprite;
    [SerializeField] private Sprite kLockedSprite;

    private PlayerController playerL;
    private PlayerController playerR;
    private Transform playerLTrans;
    private Transform playerRTrans;
    public Transform ballTrans;
    [SerializeField] private TextMeshProUGUI ballVelocity;
    [SerializeField] private Rigidbody2D ballRb;

    [SerializeField] private TextMeshProUGUI scoreTextL;
    [SerializeField] private TextMeshProUGUI scoreTextR;
    [SerializeField] private TextMeshProUGUI readyText;
    [SerializeField] private GameObject winInfo;
    [SerializeField] private TextMeshProUGUI winText;

    private int scoreL = 0;
    private int scoreR = 0;
    [SerializeField] private int maxScore = 15;
    private bool winLR = false; // false: left, true: right
    private bool isResetting = false;
    private float resetDelay = 1.5f;

    private bool isGameOver = false;
    private bool isReturning = false; // Sync end game

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[GameManager] Singleton initialized.");
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        scoreTextL.text = "0";
        scoreTextR.text = "0";

        readyText.gameObject.SetActive(false);
        
        winInfo.SetActive(false);
        winText.text = null;

        // StartCoroutine(InitGame());
        StartCoroutine(WaitForPlayers());
    }
    // private IEnumerator InitGame()
    //{
    //    yield return null;  // wait for 1 frame to wait for ohter object start
    //    ResetAllPosition();
    //}

    private IEnumerator WaitForPlayers()
    {
        //while (playerL == null || playerR == null) // 等待角色生成
        //{
        //    GameObject pl = GameObject.Find("PlayerL");
        //    GameObject pr = GameObject.Find("PlayerR");

        //    if (pl != null && playerL == null)
        //    {
        //        playerL = pl.GetComponent<PlayerController>();
        //        playerLTrans = pl.transform;
        //    }

        //    if (pr != null && playerR == null)
        //    {
        //        playerR = pr.GetComponent<PlayerController>();
        //        playerRTrans = pr.transform;
        //    }

        //    if (playerL != null && playerR != null)
        //        break;

        //    yield return null;
        //}

        while (playerL == null || playerR == null)
        {
            PlayerController[] players = FindObjectsOfType<PlayerController>();

            foreach (var pc in players)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    if (pc.photonView.IsMine && playerL == null)
                    {
                        playerL = pc;
                        playerLTrans = pc.transform;
                    }
                    else if (!pc.photonView.IsMine && playerR == null)
                    {
                        playerR = pc;
                        playerRTrans = pc.transform;
                    }
                }
                else
                {
                    if (pc.photonView.IsMine && playerR == null)
                    {
                        playerR = pc;
                        playerRTrans = pc.transform;
                    }
                    else if (!pc.photonView.IsMine && playerL == null)
                    {
                        playerL = pc;
                        playerLTrans = pc.transform;
                    }
                }
            }

            if (playerL != null && playerR != null)
                break;

            yield return null;
        }

        ResetAllPosition();

        playerL.SetDoubleScoreSkill(false); // init skillK
        playerR.SetDoubleScoreSkill(false);
        UpdateSkillIcons();
    }

    // Update is called once per frame
    void Update()
    {
        //if (ballRb != null && ballVelocity != null)
        //{
        //    float speed = ballRb.velocity.magnitude;
        //    Vector2 v = ballRb.velocity;
        //    ballVelocity.text = "Ball Speed: " + speed.ToString("F2") + "\n" + $"Vx: {v.x:F2}  Vy: {v.y:F2}";
        //}
    }

    public void UpdateSkillIcons()
    {
        //skillIconL_J.color = GetColorByState(playerL.skillJ);
        //skillIconL_K.color = GetColorByState(playerL.skillK);
        //skillIconR_J.color = GetColorByState(playerR.skillJ);
        //skillIconR_K.color = GetColorByState(playerR.skillK);
        skillIconL_J.sprite = GetSpriteByState(playerL.skillJ, jAvailableSprite, jActiveSprite, jLockedSprite);
        skillIconL_K.sprite = GetSpriteByState(playerL.skillK, kAvailableSprite, kActiveSprite, kLockedSprite);
        skillIconR_J.sprite = GetSpriteByState(playerR.skillJ, jAvailableSprite, jActiveSprite, jLockedSprite);
        skillIconR_K.sprite = GetSpriteByState(playerR.skillK, kAvailableSprite, kActiveSprite, kLockedSprite);
    }

    private Sprite GetSpriteByState(SkillState state, Sprite available, Sprite active, Sprite locked)
    {
        switch (state)
        {
            case SkillState.Available: return available;
            case SkillState.Active: return active;
            default: return locked;
        }
    }

    Color GetColorByState(SkillState state)
    {
        switch (state)
        {
            case SkillState.Available: return availableColor;
            case SkillState.Active: return activeColor;
            default: return lockedColor;
        }
    }

    public void AddScore(bool isLeftTouched)
    {        
        if (isGameOver)
            return;

        if (!PhotonNetwork.IsMasterClient)
            return;

        // if player use immune skill
        bool isImmune = false;
        if(isLeftTouched && playerL != null && playerL.IsImmune())
            isImmune = true;
        if(!isLeftTouched && playerR != null &&  playerR.IsImmune())
            isImmune = true;
        if (isImmune)
            return; // immune scoring one time
        // end immune skill
        playerL?.CancelImmunity();
        playerR?.CancelImmunity();

        // if player use double scoring skill
        int addScore = 1;
        if (!isLeftTouched && playerL != null && playerL.IsDoubleScore())   // Left player add
            addScore = 2;
        else if (isLeftTouched && playerR != null && playerR.IsDoubleScore())   // Right player add
            addScore = 2;

        if (isLeftTouched)
        {
            scoreR += addScore;
            scoreTextR.text = scoreR.ToString();
            winLR = true;
        }
        else
        {
            scoreL += addScore;
            scoreTextL.text = scoreL.ToString();
            winLR = false;
        }

        // end double scoring skill after scoring
        if (playerL != null && playerL.IsDoubleScore())
        {
            playerL.CancelDoubleScore();
        }
        if (playerR != null && playerR.IsDoubleScore())
        {
            playerR.CancelDoubleScore();
        }

        photonView.RPC("SyncScoreAndReset", RpcTarget.All, scoreL, scoreR, winLR); // Sync
    }

    // Sync
    [PunRPC]
    public void SyncScoreAndReset(int newScoreL, int newScoreR, bool winRight)
    {
        if (Instance == null)
        {
            Debug.LogError("[Client] GameManager.Instance is NULL when SyncScoreAndReset called!");
            return;
        }

        scoreL = newScoreL;
        scoreR = newScoreR;
        winLR = winRight;

        scoreTextL.text = scoreL.ToString();
        scoreTextR.text = scoreR.ToString();

        CheckGameOver();
        if (!isGameOver)
            StartCoroutine(AfterScoring());
    }

    private IEnumerator AfterScoring()
    {
        isResetting = true; // delay scoring
        Time.timeScale = 0.5f;  // slow down game(frame) speed
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // Sync

        yield return new WaitForSeconds(resetDelay);

        ResetAllPosition();
    }

    private void ResetAllPosition()
    {

        // reset players position
        // playerLTrans.position = new Vector3(-6.5f, -4.5f, 0);
        // playerRTrans.position = new Vector3(6.5f, -4.5f, 0);
        if (playerL != null && playerL.photonView != null)
            playerL.photonView.RPC("RemoteReset", playerL.photonView.Owner, new Vector3(-6.5f, -4.5f, 0));
        if (playerR != null && playerR.photonView != null)
            playerR.photonView.RPC("RemoteReset", playerR.photonView.Owner, new Vector3(6.5f, -4.5f, 0));

        playerL.SetDoubleScoreSkill(winLR);     // if playerL lose (winLR=true), playerL can use skill
        playerR.SetDoubleScoreSkill(!winLR);    // if playerR lose (winLR=false), playerR can use skill
        UpdateSkillIcons();

        // reset ball position
        if (PhotonNetwork.IsMasterClient)
        {
            float ballX = winLR ? 6.5f : -6.5f; // true: right / false: left
            ballTrans.position = new Vector3(ballX, 5f, 0);
            ballRb.velocity = Vector2.zero;
            ballRb.angularVelocity = 0f;
            ballRb.bodyType = RigidbodyType2D.Static;
        }

        // stop ball moving
        ballRb.velocity = Vector2.zero;
        ballRb.angularVelocity = 0f;
        ballRb.bodyType = RigidbodyType2D.Static;   // prevent ball falling

        StartCoroutine(ShowReadyText());
        
        // Invoke(nameof(ResumeScoring), resetDelay);
    }

    private IEnumerator ShowReadyText()
    {
        // player cannot moving
        if (playerL != null)
            playerL.ReadyReset(true);
        if (playerR != null)
            playerR.ReadyReset(true);

        float timer = 0f;
        float flashInertval = 0.5f;

        while (timer < resetDelay)
        {
            readyText.gameObject.SetActive(!readyText.gameObject.activeSelf);
            yield return new WaitForSeconds(flashInertval);
            timer += flashInertval;
        }

        readyText.gameObject.SetActive(false);

        Time.timeScale = 1f;    // reset
        Time.fixedDeltaTime = 0.02f;

        isResetting = false;    // restart scoring

        ballRb.bodyType = RigidbodyType2D.Dynamic;  // ball can start falling
        
        playerL.ReadyReset(false);  // player can start moving
        playerR.ReadyReset(false);
    }

    // for ball controller to check
    public bool IsResetting()
    {
        return isResetting;
    }

    private void CheckGameOver()
    {
        if (scoreL >= maxScore)
        {
            Debug.Log("Left Player Wins!");
            winLR = false;
            photonView.RPC("GameOverRPC", RpcTarget.All, winLR);
            // EndGame();
        }
        else if (scoreR >= maxScore)
        {
            Debug.Log("Right Player Wins!");
            winLR = true;
            photonView.RPC("GameOverRPC", RpcTarget.All, winLR);
            //EndGame();
        }
    }

    public bool IsGameOver() { return isGameOver; }

    public void OpponentLeftGame()
    {
        if (isGameOver) return;

        if (PhotonNetwork.CurrentRoom.PlayerCount == 1) // opponent leave
        {
            Debug.Log("[GameManager] 對手斷線");
            if (playerL != null && playerL.photonView.IsMine)
                winLR = false;
            else
                winLR = true;

            isGameOver = true;      // end the game without sync
            Time.timeScale = 0f;    // stop game
            winInfo.SetActive(true);
            winText.text = winLR ? "PLAYER R WIN!" : "PLAYER L WIN!";

            Debug.Log($"[Photon] GameOver. 當前狀態：{PhotonNetwork.NetworkClientState}");
            StartCoroutine(ReturnToMenuAfterDelay());
        }
    }

    // Sync
    [PunRPC]
    public void GameOverRPC(bool winRight)
    {
        if (isGameOver) return; // avoid repeat

        winLR = winRight;

        isGameOver = true;
        Time.timeScale = 0f;    // stop game
        winInfo.SetActive(true);
        winText.text = winLR ? "PLAYER R WIN!" : "PLAYER L WIN!";

        if (!isReturning)
            StartCoroutine(ReturnToMenuAfterDelay());
    }

    //private void EndGame()
    //{
    //    isGameOver = true;
    //    Time.timeScale = 0f;    // stop game
    //    winInfo.SetActive(true);
    //    winText.text = winLR ? "PLAYER R WIN!" : "PLAYER L WIN!";
    //}

    private IEnumerator ReturnToMenuAfterDelay()
    {
        if (isReturning)
        {
            Debug.LogWarning("已在返回主選單流程 跳過重複呼叫");
            yield break;
        }

        isReturning = true;
        yield return new WaitForSecondsRealtime(3f); // 3秒後回Menu

        // disconnect 有問題
        //if (PhotonNetwork.IsConnected)
        //{
        //    foreach (var go in FindObjectsOfType<GameObject>())
        //    {
        //        if (go.GetComponent<PhotonView>() != null)
        //        {
        //            Destroy(go);
        //        }
        //    }

        //    PhotonNetwork.Disconnect();
        //    yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.Disconnected);
        //}

        Debug.Log("強制回主選單");
        if (PhotonNetwork.IsMasterClient)
            photonView.RPC("RPC_LeaveRoom", RpcTarget.All);
    }

    [PunRPC]
    public void RPC_LeaveRoom()
    {
        //if (PhotonNetwork.InRoom)
        //    PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainPage");
    }

    //private IEnumerator ReturnToMenuAfterDelay()
    //{
    //    if (isReturning)
    //    {
    //        Debug.LogWarning("已在返回主選單流程中. 跳過重複呼叫");
    //        yield break;
    //    }

    //    isReturning = true;
    //    yield return new WaitForSecondsRealtime(3f); // 3秒後回Menu

    //    //if (PhotonNetwork.IsConnected)
    //    //{
    //    //    foreach (var go in FindObjectsOfType<GameObject>())
    //    //    {
    //    //        if (go.GetComponent<PhotonView>() != null)
    //    //        {
    //    //            Destroy(go);
    //    //        }
    //    //    }

    //    //    PhotonNetwork.Disconnect();
    //    //    yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.Disconnected);
    //    //}


    //    //foreach (var pv in FindObjectsOfType<PhotonView>())
    //    //{
    //    //    Destroy(pv.gameObject);
    //    //}

    //    // NetworkManager.Instance.Disconnect();
    //    // yield return new WaitUntil(() => PhotonNetwork.NetworkClientState == ClientState.Disconnected);
    //    // DisconnectPlayers();
    //    //PhotonNetwork.LeaveRoom();
    //    //Debug.Log($"[Photon] After Disconnect(), current state: {PhotonNetwork.NetworkClientState}");

    //    SceneManager.LoadScene("MainPage");
    //}

    //public void DisconnectPlayers()
    //{
    //    StartCoroutine(DisconnectAndLoad());
    //    Destroy(Instance.gameObject);
    //}

    //private IEnumerator DisconnectAndLoad()
    //{
    //    PhotonNetwork.AutomaticallySyncScene = false;

    //    if (PhotonNetwork.InRoom)
    //    {
    //        PhotonNetwork.LeaveRoom();
    //        while (PhotonNetwork.InRoom)
    //            yield return null;
    //    }

    //    NetworkManager.Instance.Disconnect();
    //    PhotonNetwork.NetworkingClient = null;
    //    while (PhotonNetwork.IsConnected)
    //        yield return null;

    //    SceneManager.LoadScene("MainPage");
    //}
}