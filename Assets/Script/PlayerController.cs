using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rigidBody2D;
    private Animator animator;
    private Collider2D colli2D;

    private enum State { idle, running, jumping, falling, attack};
    private State state = State.idle;

    [SerializeField] private LayerMask ground;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float pushForce = 5f;
    [SerializeField] private float attackForce = 10f;
    [SerializeField] private float attackTime = 0.5f;
    [SerializeField] private float score;
    [SerializeField] private TextMeshProUGUI scoreText;


    // Start is called before the first frame update
    void Start()
    {
        rigidBody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        colli2D = GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        Movement();
        // attack
        if (Input.GetKeyDown(KeyCode.Return) && state != State.attack)  // prevent double attack
        {
            StartCoroutine(Attack());
        }

        AnimationState();
        animator.SetInteger("state", (int)state);   // change animator parameter

    }

    // Collect items
    private void OnTriggerEnter2D(Collider2D collision)
    {
        
    }

    private void Movement()
    {
        float hDirection = Input.GetAxis("Horizontal"); //unity input manager

        // move left
        if (hDirection < 0) // Input.GetKey(KeyCode.A)
        {
            rigidBody2D.velocity = new Vector2(-speed, rigidBody2D.velocity.y);
            this.transform.localScale = new Vector2(-1, 1); // face left
        }
        // moving right
        else if (hDirection > 0)    // Input.GetKey(KeyCode.D)
        {
            rigidBody2D.velocity = new Vector2(speed, rigidBody2D.velocity.y);
            this.transform.localScale = new Vector2(1, 1);  // face right
        }
        // jumping
        if (Input.GetButton("Jump") && colli2D.IsTouchingLayers(ground))    // use layermask to detect whether touching ground or not
        {
            Jump();
        }
    }

    private void Jump()
    {
        rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, jumpForce);
        state = State.jumping;
    }

    private IEnumerator Attack()
    {
        state = State.attack;

        yield return new WaitForSeconds(attackTime); // wait for attack animation end

        if (!colli2D.IsTouchingLayers(ground))  // still in jumping / falling stage 
        {
            if (rigidBody2D.velocity.y >= 0.1f)
            {
                state = State.jumping;
            }
            else
            {
                state = State.falling;
            }
        }
        else    // touch the ground
        {
            if (Mathf.Abs(rigidBody2D.velocity.x) > 2f)
            {
                state = State.running;
            }
            else
            {
                state = State.idle;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) 
            return;

        
        ContactPoint2D contact = collision.GetContact(0);
        Rigidbody2D ballRb = collision.rigidbody;

        if (state == State.attack)          // attack to push the ball
        {
            Debug.Log("attack!");
            Vector2 forceDir = collision.GetContact(0).normal * (-1f);
            collision.rigidbody.AddForce(forceDir * attackForce, ForceMode2D.Impulse);

            // debug 看的 (畫法向量)
            Debug.DrawRay(contact.point, contact.normal, Color.red, 1f);
        }
        else if (contact.normal.y < -0.5f)  // touch the ball from the bottom
        {
            Debug.Log("touching ball");
            float xDir = Input.GetAxisRaw("Horizontal");
            Vector2 forceDir = new Vector2(xDir == 0 ? transform.localScale.x : xDir, 1f).normalized;   // 根據角色面向哪邊決定頂球方向

            ballRb.velocity = Vector2.zero;
            ballRb.AddForce(forceDir * pushForce, ForceMode2D.Impulse); // add pushing force to the ball (weaker than attacking force)

            if (state == State.jumping) 
            {
                // 補償向上速度，避免被球重量往下壓造成跳躍高度減低
                rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, Mathf.Max(rigidBody2D.velocity.y, jumpForce * 0.7f));
            }

            // debug 看的 (畫法向量)
            Debug.DrawRay(contact.point, contact.normal, Color.green, 1f);
        }
    }

    private void AnimationState()
    {
        if (state == State.jumping)
        {
            if (rigidBody2D.velocity.y < 0.1f)  // from jump to fall
            {
                state = State.falling;
            }
        }
        else if (state == State.falling)
        {
            if (colli2D.IsTouchingLayers(ground))   // falling to the ground
            {
                state = State.idle;
            }
        }
        else if (state == State.attack)
        {
            // play attack animation
        }
        else if (Mathf.Abs(rigidBody2D.velocity.x) > 2f)    //running to left/right
        {
            state = State.running;
        }
        else
        {
            state = State.idle;
        }
    }
}
