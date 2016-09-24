using UnityEngine;
using System.Collections;

public class Agent : MonoBehaviour {
    public Rigidbody2D r1;
    public Rigidbody2D r2;

    int trigger = 0;
    Vector2 velo1;


	void Start () {
        //r = GetComponent<Rigidbody2D>();

        r1.velocity = new Vector2(6, 7);
        r2.velocity = new Vector2(-6, 7);
        Invoke("one", 1f);
    }

    void one() {
        trigger = 1;
       
    }

    void FixedUpdate () {
        if (trigger == 25)
            trigger = -1;

        if (trigger > 1)
        {
            Debug.Log(trigger + " 1 " + r1.velocity);
            trigger++;

        }

        if (trigger == 1)
        {
            Debug.Log(trigger+ " Before1:  " + r1.velocity);
            velo1 = r1.velocity;
            r1.isKinematic = true;
            Debug.Log(trigger + " After1:  " + r1.velocity);
            trigger++;

            
        }


        if (trigger < -1)
        {
            Debug.Log(trigger + " 1 " + r1.velocity);
            trigger--;

        }

        if (trigger == -1) {
            
            Debug.Log(trigger+ " Before1:  " + r1.velocity);
            
            r1.isKinematic = false;
            r1.WakeUp();
            r1.velocity = velo1;
            Debug.Log(trigger + " After1:  " + r1.velocity);
            trigger--;
        }

        

        
    }
}
