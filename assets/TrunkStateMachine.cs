using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrunkStateMachine : MonoBehaviour
{
    private Istate currentState;
    private Dictionary<string, Istate> states = new Dictionary<string, Istate>();
    public GameObject player;
    public int num;
    public DistanceJoint2D dj;
    public GameObject fore;
    public float multiple;
    public Rigidbody2D rb;
    public float magicNumber = 160f;
    public string state;
    GameObject back;
    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        rb = gameObject.GetComponent<Rigidbody2D>();
        dj = gameObject.AddComponent<DistanceJoint2D>();
        states.Add("Running", new RunningState(this));
        states.Add("Stopping", new StoppingState(this));
        TransitionState("Stopping");
        currentState = states["Stopping"];
        rb = gameObject.GetComponent<Rigidbody2D>();
        foreach (Transform child in transform)
        {
            if (child.gameObject.name == "back")
            {
                back = child.gameObject;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        currentState.onUpdate();
    }

    public void TransitionState(string str)
    {
        state = str;
        if (currentState != null)
        {
            currentState.onExit();
        }
        currentState = states[str];
        currentState.onEnter();
    }

    public void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Boss") {
            //扣血
            
            player.GetComponent<TrainEngineController>().disableTrunk(num);
        }
        if (other.gameObject.tag == "BossAttack" && state == "Running") { 
        
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player") {
            collision.gameObject.GetComponent<TrainEngineController>().addTrunk(gameObject);
        }
        if (collision.gameObject.tag == "Boss" && state == "Running")
        {
            //扣血
            collision.gameObject.GetComponent<FSM>().getHurt();
            player.GetComponent<TrainEngineController>().disableTrunk(num);
        }
    }

    public void getOut()
    {
        if (num != 0)
        {
            fore.GetComponent<TrunkStateMachine>().cutOff();
        }
        else {
            fore.GetComponent<TrainEngineController>().cutOff();
        }
        num = 0;
        TransitionState("Stopping");
    }

    public void cutOff() {
        dj.enabled = false;
    }

    public void putOn(GameObject gb) {
        dj.enabled = true;
        dj.anchor = back.transform.localPosition;
        dj.connectedBody = gb.GetComponent<Rigidbody2D>();
        dj.distance = 0.4f;
        TransitionState("Running");
    }

    public void getIn(GameObject gb,int num) {
        fore = gb;
        this.num = num;
        TransitionState("Running");
    }



}
