using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviourPun
{
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
    }

    // Update is called once per frame
    void Update()
    {
        if (ballRb != null && ballVelocity != null)
        {
            float speed = ballRb.velocity.magnitude;
            Vector2 v = ballRb.velocity;
            ballVelocity.text = "Ball Speed: " + speed.ToString("F2") + "\n" + $"Vx: {v.x:F2}  Vy: {v.y:F2}";
        }
    }

    public void AddScore(bool isLeftTouched)
    {        
        if (isGameOver)
            return;

        // if player use immune skill
        bool isImmune = false;
        if(isLeftTouched && playerL != null && playerL.photonView.IsMine && playerL.IsImmune())
            isImmune = true;
        if(!isLeftTouched && playerR != null &&  playerR.photonView.IsMine && playerR.IsImmune())
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

    // Sync
    [PunRPC]
    public void GameOverRPC(bool winRight)
    {
        winLR = winRight;

        isGameOver = true;
        Time.timeScale = 0f;    // stop game
        winInfo.SetActive(true);
        winText.text = winLR ? "PLAYER R WIN!" : "PLAYER L WIN!";
    }

    //private void EndGame()
    //{
    //    isGameOver = true;
    //    Time.timeScale = 0f;    // stop game
    //    winInfo.SetActive(true);
    //    winText.text = winLR ? "PLAYER R WIN!" : "PLAYER L WIN!";
    //}
}
