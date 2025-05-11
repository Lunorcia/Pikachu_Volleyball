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
    }

    public void StartPrivateMatch()
    {
        Debug.Log("私人對戰模式");
        mainPanel.SetActive(false);
        privatePanel.SetActive(true);
        passwordInput.text = string.Empty;
    }

    public void ConfirmPrivateCode()
    {
        Debug.Log("Linking password: " + passwordInput.text);
        privatePanel.SetActive(false);
        matchingPanel.SetActive(true);
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
