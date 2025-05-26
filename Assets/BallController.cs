using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviourPun, IPunObservable
{
    private Rigidbody2D rb;

    [SerializeField] private float maxVelocity = 20f;
    [SerializeField] private float maxAttackedVelocity = 40f;
    private bool isAttacked = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (!PhotonNetwork.IsMasterClient)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;  // 只讓 MasterClient 跑物理
        }
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!PhotonNetwork.IsMasterClient) // MasterClient處理碰地判斷和得分
            return;

        //GameManager gm = FindObjectOfType<GameManager>();
        //if (gm == null || gm.IsResetting())
        //    return;
        if (GameManager.Instance == null || GameManager.Instance.IsResetting())
            return;

        if ( collision.gameObject.CompareTag("LeftGround"))
        {
            GameManager.Instance.AddScore(true); // touch left groud
        }
        else if(collision.gameObject.CompareTag("RightGround"))
        {
            GameManager.Instance.AddScore(false);    // touch right ground
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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) // Master send
        {
            stream.SendNext(transform.position);
            stream.SendNext(rb.velocity);
            stream.SendNext(rb.angularVelocity);
        }
        else // Client receive
        {
            transform.position = (Vector3)stream.ReceiveNext();

            if (rb.bodyType != RigidbodyType2D.Static)
            {
                rb.velocity = (Vector2)stream.ReceiveNext();
                rb.angularVelocity = (float)stream.ReceiveNext();
            }
            else
            {
                _ = stream.ReceiveNext();
                _ = stream.ReceiveNext();
            }
        }
    }
}
