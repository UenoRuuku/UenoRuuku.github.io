using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Unity.Mathematics;

public class PlayerCtrl : MonoBehaviour
{
    public float windMultiA = 1;
    public float windMultiB = 1;
    public float windUpSpeed = 5;
    [Header("move")]
    public float WalkSpeed;
    public float AccelerateTime;
    public float DecelerateTime;
    public Vector2 InputOffset;
    public bool canMove = true;
    [Header("Jump")]
    public float FallMultiplier;
    public float LowJumpMultiplier;
    public float JumpingSpeed;
    public float WallJumpSpeed;
    bool canJump = true;
    public int jumpPoint = 2;
    [Header("GroundCheck")]
    public LayerMask GroundLayerMask;
    public LayerMask WallLayerMask;
    public Vector2 PointOffset;
    public Vector2 LeftOffset;
    public Vector2 RightOffset;
    public Vector2 Size;
    public float slideSpeed = 1;
    float velocityX;
    bool isJumping;
    bool isOnWall = false;
    bool isOnLeftWall;
    bool isOnGround;
    bool GravityModifier = true;
    [Header("dash")]
    bool isDashing = false;
    bool wasDashed = false;
    bool coolDownEnd = false;
    Vector2 dir;
    public GameObject DashParticleSystem;
    public float DashTime;
    public float DragMaxForce;
    public float DragForce;
    public float DragDuration;
    public float CoolDownTime;
    public bool canDash = true;
    bool isWindUp = false;

    [Header("onwall")]
    bool onWall = false;

    Rigidbody2D rig;
    Animator anim;
    public AudioSource jumpAudio;
    public AudioSource dashAudio;
    bool cpuControl = false;
    GameObject targetPosition = null;

    bool outStop = false;

    // Start is called before the first frame update
    void Start()
    {
        rig = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        isOnGround = OnGround();
        OnWall();
        setJumpAnimation();
        if (GravityModifier)
        {
            adjustGravity();
        }
        if (canDash) {
            dash();
        }
        if (isOnWall) {
            WallSlide();
            anim.SetBool("isClimb", true);
        }
        else
        {
            anim.SetBool("isClimb", false);
        }
        if (canJump)
        {
            jump();
        }
        if (canMove)
        {
            run();
        }
        if (isWindUp) {
            doWindUp();
        }
        if (!isOnGround && !isOnWall)
        {
            rig.velocity = new Vector2(rig.velocity.x + (windMultiA - windMultiB) * WalkSpeed, rig.velocity.y);
        }

        if (rig.velocity.y > 20) {
            rig.velocity = new Vector2(rig.velocity.x, 20);
        }
        if (rig.velocity.y < -20) {
            rig.velocity = new Vector2(rig.velocity.x, -20);
        }
        if (rig.velocity.x > 60)
        {
            rig.velocity = new Vector2(60, rig.velocity.y);
        }
        if (rig.velocity.x < -60) {
            rig.velocity = new Vector2(-60, rig.velocity.y);
        }
    }
    void run()
    {
            if (Input.GetAxisRaw("Horizontal") > InputOffset.x)
            {
                rig.velocity = new Vector2(Mathf.SmoothDamp(rig.velocity.x, windMultiA * WalkSpeed * Time.fixedDeltaTime * 60, ref velocityX, AccelerateTime), rig.velocity.y);
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * -1, transform.localScale.y, transform.localScale.z);
                anim.SetFloat("run", 1f);
            }
            else if (Input.GetAxisRaw("Horizontal") < InputOffset.x * -1)
            {
                rig.velocity = new Vector2(Mathf.SmoothDamp(rig.velocity.x, windMultiB * WalkSpeed * Time.fixedDeltaTime * 60 * -1, ref velocityX, AccelerateTime), rig.velocity.y);
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                anim.SetFloat("run", 1f);
            }
            else
            {
                rig.velocity = new Vector2(Mathf.SmoothDamp(rig.velocity.x, 0, ref velocityX, DecelerateTime), rig.velocity.y);
                anim.SetFloat("run", 0);
            }

    }

    void jump()
    {
        if (Input.GetAxis("Jump") == 1 && !isJumping && jumpPoint > 0)
        {
            jumpAudio.Play();
            if (!isOnWall)
            {
                rig.velocity = new Vector2(rig.velocity.x, JumpingSpeed);
                jumpPoint -= 1;
            }
            else {
                Debug.Log("jump on wall");
                isOnWall = false;
                StartCoroutine(StopWallJumping());
                if (isOnLeftWall)
                {
                    rig.velocity = new Vector2(WallJumpSpeed, JumpingSpeed);
                }
                else {
                    rig.velocity = new Vector2(-WallJumpSpeed, JumpingSpeed);
                }
            }
            isJumping = true;
            anim.SetBool("isJump", true);
            if (!isOnGround)
            {
            }
        }
        if (Input.GetAxis("Jump") == 0)
        {
            isJumping = false;
            if (isOnGround) {
                anim.SetBool("isJump", false);
                jumpPoint = 1;
            }
        }
    }



    void adjustGravity()
    {
        if (rig.velocity.y < 0)
        {
            rig.velocity += Vector2.up * Physics2D.gravity.y * (FallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rig.velocity.y > 0 && Input.GetAxis("Jump") != 1)
        {
            rig.velocity += Vector2.up * Physics2D.gravity.y * (LowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void dash()
    {
        if (Input.GetAxisRaw("Dash") == 1 && !wasDashed)
        {
            dashAudio.Play();
            wasDashed = true;
            dir = getDir();
            rig.velocity += dir * DragForce;
            coolDownEnd = false;
            DashParticleSystem.SetActive(true);
            StartCoroutine(Dash());
        }
        if (coolDownEnd && Input.GetAxisRaw("Dash") == 0)
        {
            wasDashed = false;
        }
    }

    void WallSlide() {
        if (isJumping) {
            return;
        }
        if ((isOnLeftWall && Input.GetAxisRaw("Horizontal") < InputOffset.x * -1) || (!isOnLeftWall && Input.GetAxisRaw("Horizontal") > InputOffset.x)) {
            rig.velocity = new Vector2(rig.velocity.x, -slideSpeed);
        }      
    }

    Vector2 getDir()
    {
        Vector2 d = new Vector2(Input.GetAxisRaw("Horizontal"), 0);
        if (d.x == 0 && d.y == 0)
        {
            if (transform.localScale.x < 0)
            {
                d.x = 1;
            }
            else
            {
                d.x = -1;
            }
        }
        return d;
    }

    IEnumerator Dash()
    {
        gameObject.layer = 12;
        canMove = false;
        canJump = false;
        GravityModifier = false;
        rig.gravityScale = 0;
        rig.velocity = new Vector2(rig.velocity.x, 0);
        DOVirtual.Float(DragMaxForce, 0, DragDuration, RigidbodyDrag);
        yield return new WaitForSecondsRealtime(DashTime);

        canMove = true;
        canJump = true;
        GravityModifier = true;
        rig.gravityScale = 1;
        DashParticleSystem.SetActive(false);
        gameObject.layer = 9;
        yield return new WaitForSecondsRealtime(CoolDownTime);

        coolDownEnd = true;
    }

    IEnumerator StopWallJumping() {
        canMove = false;
        yield return new WaitForSecondsRealtime(0.1f);
        canMove = true;
    }
    public void RigidbodyDrag(float x)
    {
        rig.drag = x;
    }

    bool OnGround()
    {
        Collider2D Coll = Physics2D.OverlapBox((Vector2)transform.position + PointOffset, Size, 0, GroundLayerMask);

        if (Coll != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    void OnWall()
    {
        Collider2D CollA = Physics2D.OverlapBox((Vector2)transform.position + LeftOffset, Size, 0, WallLayerMask);
        Collider2D CollB = Physics2D.OverlapBox((Vector2)transform.position + RightOffset, Size, 0, WallLayerMask);
        if (CollA != null)
        {
            jumpPoint = 1;
            isOnWall = true;
            isOnLeftWall = true;
        }
        else if (CollB != null)
        {
            jumpPoint = 1;
            isOnWall = true;
            isOnLeftWall = false;
        }
        else {
            isOnWall = false;
        }

    }


    void setJumpAnimation()
    {
        // Debug.Log(rig.velocity.y);
        if (rig.velocity.y > 0 && isJumping)
        {
            anim.SetFloat("jump", 0);
        }
        else if (rig.velocity.y < 0 && !isOnGround)
        {
            anim.SetFloat("jump", 1);
            anim.SetBool("isJump", true);
        }
        else if (isOnGround) {
            anim.SetBool("isJump", false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube((Vector2)transform.position + PointOffset, Size);
        Gizmos.DrawWireCube((Vector2)transform.position + LeftOffset, Size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube((Vector2)transform.position + PointOffset, Size);
    }
    public void setWindMulti(float a, float b) {
        windMultiA = a;
        windMultiB = b;
    }


    public void WindUp(bool b) {
        isWindUp = b;
        canJump = !b;
    }

    void doWindUp() {
        rig.velocity = new Vector2(rig.velocity.x, windUpSpeed);
        jumpPoint = 1;
    }

}
