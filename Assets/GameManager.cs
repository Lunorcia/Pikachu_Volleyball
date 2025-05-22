using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ballVelocity;
    [SerializeField] private Rigidbody2D ballRb;
    // Start is called before the first frame update
    void Start()
    {
        
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
}
