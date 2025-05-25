using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerController playerL;
    [SerializeField] private PlayerController playerR;
    public Transform playerLTrans;
    public Transform playerRTrans;
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



    // Start is called before the first frame update
    void Start()
    {
        scoreTextL.text = "0";
        scoreTextR.text = "0";

        readyText.gameObject.SetActive(false);
        
        winInfo.SetActive(false);
        winText.text = null;

        StartCoroutine(InitGame());

    }
     private IEnumerator InitGame()
    {
        yield return null;  // wait for 1 frame to wait for ohter object start
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
        
        if (isLeftTouched)
        {
            scoreR++;
            scoreTextR.text = scoreR.ToString();
            winLR = true;
        }
        else
        {
            scoreL++;
            scoreTextL.text = scoreL.ToString();
            winLR = false;
        }
        CheckGameOver();
        if (!isGameOver)
            StartCoroutine(AfterScoring());

    }

    private IEnumerator AfterScoring()
    {
        isResetting = true; // delay scoring
        Time.timeScale = 0.5f;  // slow down game(frame) speed
        yield return new WaitForSeconds(resetDelay);

        ResetAllPosition();

    }

    private void ResetAllPosition()
    {
        // reset players position
        playerLTrans.position = new Vector3(-6.5f, -4.5f, 0);
        playerRTrans.position = new Vector3(6.5f, -4.5f, 0);
        // reset ball position
        float ballX = winLR ? 6.5f : -6.5f; // true: right / false: left
        ballTrans.position = new Vector3(ballX, 5f, 0);
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
        playerL.ReadyReset(true);
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

        isResetting = false;    // restart scoring
        Time.timeScale = 1f;
        ballRb.bodyType = RigidbodyType2D.Dynamic;  // ball can start falling
        // player can start moving
        playerL.ReadyReset(false);
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
            EndGame();
        }
        else if (scoreR >= maxScore)
        {
            Debug.Log("Right Player Wins!");
            winLR = true;
            EndGame();
        }
    }

    private void EndGame()
    {
        isGameOver = true;
        Time.timeScale = 0f;    // stop game
        winInfo.SetActive(true);
        winText.text = winLR ? "PLAYER R WIN!" : "PLAYER L WIN!";
    }
}
