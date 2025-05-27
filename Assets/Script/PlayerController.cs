using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviourPun, IPunObservable
{
    private Rigidbody2D rigidBody2D;
    private Animator animator;
    private Collider2D colli2D;
    private SpriteRenderer spriteRenderer;

    private enum State { idle, running, jumping, falling, attack };
    private State state = State.idle;
    private bool isTouched = false;
    private bool isReadyWaiting = false;
    
    private bool isFacingRight = true;
    private int lastState = -1;
    
    private Vector3 networkPosition;  // smoothing (Sync)
    private float distanceThreshold = 1.5f;
    private Vector3 lastReceivedPosition;
    private Vector3 velocity;
    private float smoothTime = 0.05f;


    public enum SkillState { Locked, Available, Active }
    public SkillState skillJ = SkillState.Locked;
    public SkillState skillK = SkillState.Locked;

    // immune skill
    private bool usedImmunity = false;
    private bool isImmune = false;
    // double scoring skill
    private bool canDoubleScore = false;
    private bool usedDoubleScore = false;
    private bool isDoubleScore = false;
    [SerializeField] private float skillDuration = 10f;

    [SerializeField] private LayerMask ground;
    [SerializeField] private float speed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float compensationJumpForce;
    [SerializeField] private float pushForce = 5f;

    [SerializeField] private float normalPushForce = 5f;
    [SerializeField] private float attackPushForce = 10f;
    [SerializeField] private float attackTime = 0.5f;
    [SerializeField] private float score;
    //[SerializeField] private TextMeshProUGUI scoreText;


    // Start is called before the first frame update
    void Start()
    {
        rigidBody2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        colli2D = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (!photonView.IsMine)
        {
            rigidBody2D.isKinematic = true; // 不控制對手角色(減少抖動)
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            // master client 生成在左邊 (面右)
            transform.position = new Vector3(-6.5f, -4.5f, 0);
            isFacingRight = true;
        }
        else
        {
            // client 生成在右邊 (面左)
            transform.position = new Vector3(6.5f, -4.5f, 0);
            isFacingRight = false;
        }

        spriteRenderer.flipX = !isFacingRight; // init facing
    }

    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine)
        {
            if (Vector3.Distance(transform.position, lastReceivedPosition) > 1.5f)
            {
                // 差距過大直接修正
                transform.position = lastReceivedPosition;
                velocity = Vector3.zero;
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                lastReceivedPosition,
                ref velocity,
                smoothTime
            );
            return;
        }

        if (isReadyWaiting)
        {
            // prevent animation stuck in inappropriate state while state ready
            if ((int)state != lastState)  // avoid flash
            {
                animator.SetInteger("state", (int)state);   // change animator parameter     
                lastState = (int)state;
            }
            return;
        }

        

        Movement();

        // attack
        if (Input.GetKeyDown(KeyCode.Return) && state != State.attack)  // prevent double attack
        {
            StartCoroutine(Attack());
        }

        // immune skill
        if (Input.GetKeyDown(KeyCode.J) && !usedImmunity)
        {
            usedImmunity = true;
            photonView.RPC("ActivateImmunitySkill", RpcTarget.All);
        }

        // double scoring skill
        if (Input.GetKeyDown(KeyCode.K) && canDoubleScore && !usedDoubleScore)
        {
            usedDoubleScore = true;
            photonView.RPC("ActivateDoubleScoreSkill", RpcTarget.All);
        }

        AnimationState();

        // avoid flash
        if ((int)state != lastState)
        {
            animator.SetInteger("state", (int)state); // change animator parameter
            lastState = (int)state;
        }
    }
    //void FixedUpdate()
    //{
    //    if (!photonView.IsMine)
    //    {
    //        if (networkPosition == Vector3.zero)
    //            return;

    //        float distance = Vector3.Distance(transform.position, networkPosition);

    //        if (distance > distanceThreshold)
    //        {
    //            transform.position = networkPosition; // 太遠就拉回來
    //        }
    //        else
    //        {
    //            // 平滑移動到目標位置
    //            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.fixedDeltaTime * 15f);
    //        }
    //    }
    //}

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
            // this.transform.localScale = new Vector2(-1, 1); // face left
            isFacingRight = false;
        }
        // moving right
        else if (hDirection > 0)    // Input.GetKey(KeyCode.D)
        {
            rigidBody2D.velocity = new Vector2(speed, rigidBody2D.velocity.y);
            // this.transform.localScale = new Vector2(1, 1);  // face right
            isFacingRight = true;
        }
        // jumping
        if (Input.GetButton("Jump") && colli2D.IsTouchingLayers(ground))    // use layermask to detect whether touching ground or not
        {
            Jump();
        }
        // face
        spriteRenderer.flipX = !isFacingRight; 
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
        BallController ballCtrl = collision.gameObject.GetComponent<BallController>();
        // Vector2 pushDir = new Vector2(transform.localScale.x * pushForce, 1f).normalized;
        float xDirection = isFacingRight ? 1f : -1f;
        Vector2 pushDir = new Vector2(xDirection * pushForce, 1f).normalized;

        if (state == State.attack)          // attack to push the ball
        {
            Debug.Log("attack!");

            ballCtrl?.OnAttacked(); // increase ball's velocity limit when attacking
            //pushDir = new Vector2(transform.localScale.x * pushForce * 1.5f, 1f).normalized;
            pushDir = new Vector2(xDirection * pushForce * 1.5f, 1f).normalized;
            ballRb.AddForce(pushDir * attackPushForce, ForceMode2D.Impulse);

            // debug 看的 (畫法向量)
            //Debug.DrawRay(contact.point, contact.normal, Color.red, 1.5f);
        }
        else if (contact.normal.y < -0.5f)
        {
            Debug.Log("touching ball");

            ballCtrl?.OnTouched();  // reset ball's velocity limit to normal
            ballRb.AddForce(pushDir * normalPushForce, ForceMode2D.Impulse);

            // debug 看的 (畫法向量)
            //Debug.DrawRay(contact.point, contact.normal, Color.green, 1.5f);
        }
        else
        {
            ballCtrl?.OnTouched();  // reset ball's velocity limit to normal
        }
        // 補償向上速度，避免被球重量往下壓造成跳躍高度減低
        if (state == State.jumping && isTouched == false)
        {
            rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, Mathf.Max(rigidBody2D.velocity.y, compensationJumpForce));
            isTouched = true;   // prevent double compensation
        }

        /*
        //if (state == State.attack)          // attack to push the ball
        //{
        //    Debug.Log("attack!");
        //    Vector2 forceDir = collision.GetContact(0).normal * (-1f);
        //    collision.rigidbody.AddForce(forceDir * attackPushForce, ForceMode2D.Impulse);

        //    // debug 看的 (畫法向量)
        //    Debug.DrawRay(contact.point, contact.normal, Color.red, 1f);
        //}
        //else if (contact.normal.y < -0.5f)  // touch the ball from the bottom
        //{
        //    Debug.Log("touching ball");
        //    float xDir = Input.GetAxisRaw("Horizontal");
        //    Vector2 forceDir = new Vector2(xDir == 0 ? transform.localScale.x : xDir, 1f).normalized;   // ������ɫ������?�Q��?����

        //    ballRb.velocity = Vector2.zero;
        //    ballRb.AddForce(forceDir * pushForce, ForceMode2D.Impulse); // add pushing force to the ball (weaker than attacking force)

        //    if (state == State.jumping)
        //    {
        //        // 補償向上速度，避免被球重量往下壓造成跳躍高度減低
        //        rigidBody2D.velocity = new Vector2(rigidBody2D.velocity.x, Mathf.Max(rigidBody2D.velocity.y, jumpForce * 0.7f));
        //    }

        //    // debug 看的 (畫法向量)
        //    Debug.DrawRay(contact.point, contact.normal, Color.green, 1f);
        //}
        */
    }

    public void ReadyReset(bool isWaiting)
    {
        isReadyWaiting = isWaiting;
        if (isWaiting)
        {
            // prevent animation stuck in inappropriate state
            state = State.idle;
            rigidBody2D.bodyType = RigidbodyType2D.Static;
        }
        else
            rigidBody2D.bodyType = RigidbodyType2D.Dynamic;
    }

    private void AnimationState()
    {
        if (state == State.jumping)
        {
            if (rigidBody2D.velocity.y < 0.1f)  // from jump to fall
            {
                state = State.falling;
                isTouched = false;  // reset ball touching detect
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

    // sync
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!enabled) return;

        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext((int)state);
            stream.SendNext(isFacingRight);
        }
        else
        {
            lastReceivedPosition = (Vector3)stream.ReceiveNext();
            state = (State)(int)stream.ReceiveNext();
            isFacingRight = (bool)stream.ReceiveNext();

            if (!photonView.IsMine)
            {
                spriteRenderer.flipX = !isFacingRight;
            }

            animator.SetInteger("state", (int)state);
        }
    }

    public bool IsImmune()
    {
        return isImmune;
    }

    public void CancelImmunity()
    {
        isImmune = false;
        skillJ = SkillState.Locked;
        GameManager.Instance.UpdateSkillIcons();
    }

    public bool IsDoubleScore()
    {
        return isDoubleScore;
    }

    public void SetDoubleScoreSkill(bool isUsable)
    {
        canDoubleScore = isUsable;
    }

    public void CancelDoubleScore()
    {
        isDoubleScore = false;
        skillK = SkillState.Locked;
        GameManager.Instance.UpdateSkillIcons();
    }


    [PunRPC]
    public void RemoteReset(Vector3 pos)
    {
        StartCoroutine(ForceResetPosition(pos));  // each player reset itself
    }

    public IEnumerator ForceResetPosition(Vector3 pos)
    {
        yield return null; // 等一幀

        transform.position = pos;
        rigidBody2D.velocity = Vector2.zero;
        rigidBody2D.angularVelocity = 0f;

        state = State.idle;
        lastState = (int)state;
        animator.SetInteger("state", lastState);

        isFacingRight = pos.x < 0;
        spriteRenderer.flipX = !isFacingRight;

        if (photonView.IsMine)
            lastReceivedPosition = transform.position; // Sync
    }

    [PunRPC]
    public void ActivateImmunitySkill()
    {
        isImmune = true;
        skillJ = SkillState.Active;
        GameManager.Instance.UpdateSkillIcons();
        StartCoroutine(DisableSkillAfterTime(() => { 
            isImmune = false;
            skillJ = SkillState.Locked;
        }));
    }

    [PunRPC]
    public void ActivateDoubleScoreSkill()
    {
        isDoubleScore = true;
        skillK = SkillState.Active;
        GameManager.Instance.UpdateSkillIcons();
        StartCoroutine(DisableSkillAfterTime(() => { 
            isDoubleScore = false;
            skillK = SkillState.Locked;
        }));
    }

    public IEnumerator DisableSkillAfterTime(System.Action disableAction)
    {
        yield return new WaitForSecondsRealtime(skillDuration);
        disableAction?.Invoke();
        GameManager.Instance.UpdateSkillIcons();
    }

}
