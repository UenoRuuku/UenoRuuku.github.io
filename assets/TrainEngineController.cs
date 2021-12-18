using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainEngineController : MonoBehaviour
{
    private float faceDirection;
    public float rotationSpeed;
    public float rollRotationSpeed;
    public float runSpeed;
    private Rigidbody2D rb;
    public Collider2D coll;
    public float AccelerateTime;
    public DistanceJoint2D sj;
    public List<GameObject> trunks;
    public GameObject back;
    float velocityX;
    float velocityY;
    public bool isDie;
    public float 测试;
    bool haveSound;
    public GameObject b;
    public GameObject boss;
    // Start is called before the first frame update
    void Start()
    {
        trunks = new List<GameObject>();
        rb = GetComponent<Rigidbody2D>();
        sj = GetComponent<DistanceJoint2D>();
        haveSound = false;
        foreach (Transform child in transform)
        {
            if (child.gameObject.name == "back")
            {
                sj.anchor = child.localPosition;
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Movement();
    }
    void Movement()
    {
        float horizontalmove = Input.GetAxis("Horizontal");
        测试 = Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad);
        //转动
        if (horizontalmove != 0)
        {
            float toAngle = transform.eulerAngles.z - horizontalmove * rotationSpeed * Time.fixedDeltaTime * 60 % 360;
            if (toAngle < 0) {
                toAngle += 360;
            }
            transform.rotation = Quaternion.Euler(0,0,toAngle) ;
        }
        if (Input.GetAxisRaw("Jump") == 1)
        {
            float cos = Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad);
            float sin = Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad);
            rb.velocity = new Vector2(Mathf.SmoothDamp(rb.velocity.x, runSpeed * Time.fixedDeltaTime * 60 * cos, ref velocityX, AccelerateTime),Mathf.SmoothDamp(rb.velocity.y, runSpeed * 60 *  Time.fixedDeltaTime * sin, ref velocityY, AccelerateTime));
            if (!haveSound)
            {
                GetComponent<AudioSource>().Play();
                haveSound = true;
            }
        }
        if (Input.GetAxisRaw("Jump") == 0)
        {
            rb.velocity = new Vector2(Mathf.SmoothDamp(rb.velocity.x, 0, ref velocityX, AccelerateTime), Mathf.SmoothDamp(rb.velocity.y, 0, ref velocityY, AccelerateTime));
            //rb.AddForce(new Vector2(Mathf.SmoothDamp(rb.velocity.x, 0, ref velocityX, AccelerateTime), Mathf.SmoothDamp(rb.velocity.y, 0, ref velocityY, AccelerateTime)));
            if (haveSound) {
                haveSound = false;
            }
        }
        if (Input.GetAxisRaw("Roll") == 1) 
        {
            float toAngle = transform.eulerAngles.z - rollRotationSpeed * Time.fixedDeltaTime * 60;
            transform.rotation = Quaternion.Euler(0, 0, toAngle);
        }
        if (isDie)
        {
            die();
        }

    }

    public void addTrunk(GameObject gb)
    {
        if (trunks.Count >= 1)
        {
            gb.GetComponent<TrunkStateMachine>().getIn(trunks[trunks.Count - 1], trunks.Count);
            trunks[trunks.Count - 1].GetComponent<TrunkStateMachine>().putOn(gb);
        }
        else {
            sj.enabled = true;
            sj.anchor = back.transform.localPosition;
            sj.connectedBody = gb.GetComponent<Rigidbody2D>();
            gb.GetComponent<TrunkStateMachine>().getIn(gameObject, trunks.Count);
        }
        trunks.Add(gb);
    }

    public void disableTrunk(int num) 
    {
        for (int i = trunks.Count-1; i >= num; i--) {
            trunks[i].GetComponent<TrunkStateMachine>().getOut();
            trunks.RemoveAt(i);
        }
    }

    public void cutOff() {
        sj.enabled = false;
    }

    public void die()
    {
        isDie = false;
        b.SetActive(true);
        boss.SetActive(false);
        StartCoroutine(DieFlash());
        enabled = false;
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Boss") {
            die();
        }
    }

    private IEnumerator DieFlash()
    {
        Material material = GetComponent<SpriteRenderer>().material;
        float coefficient = 1f;
        float maxAlpha = 1f;
        float value = maxAlpha / coefficient;
       // material.SetFloat("_Alpha", value);
        yield return null;
        while(material.GetFloat("_Alpha") < 1f - 0.0001)
        {
            coefficient += 0.07f;
            value = maxAlpha / coefficient;
            value = value < 0.02 ? 0 : value;
            material.SetFloat("_Alpha", 1f - value);
            yield return null;
        }
    }
}
