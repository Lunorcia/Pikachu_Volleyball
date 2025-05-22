using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;

    [SerializeField] private float maxVelocity = 20f;
    [SerializeField] private float maxAttackedVelocity = 40f;
    private bool isAttacked = false;

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
        // if ball is being attacked, increase velocity limit
        float vLimit = isAttacked ? maxAttackedVelocity : maxVelocity;
        if (rb.velocity.magnitude > vLimit)
            rb.velocity = rb.velocity.normalized * vLimit;

        // prevent ball stuck in the corner
        if (rb.velocity.magnitude < 0.05f)
        {
            Vector2 nudge = new Vector2(Random.Range(-0.05f, 0.05f), 0.5f);
            rb.AddForce(nudge, ForceMode2D.Impulse);
        }
    }

    // increase ball's velocity limit when attacking
    public void OnAttacked()
    {
        isAttacked = true;
    }

    // reset ball's velocity limit to normal
    public void OnTouched()
    {
        isAttacked = false;
    }
}
