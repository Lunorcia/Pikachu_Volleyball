using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        // prevent ball stuck in the corner
        if (rb.velocity.magnitude < 0.05f)
        {
            Vector2 nudge = new Vector2(Random.Range(-0.05f, 0.05f), 0.5f);
            rb.AddForce(nudge, ForceMode2D.Impulse);
        }
    }
}
