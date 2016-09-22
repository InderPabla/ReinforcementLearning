using UnityEngine;
using System.Collections;

public class Agent : MonoBehaviour {
    Rigidbody2D r;
    Vector2 v = new Vector2();
	void Start () {
        r = GetComponent<Rigidbody2D>();

        Time.timeScale = 0f;
       
        r.velocity = new Vector2(6, 7);
        one();
        Invoke("one",2f);
    }

    void one() {
        v = r.velocity;
        r.isKinematic = true;
        two();
        Invoke("two", 0f);
    }

    void two() {
        Debug.Log("dfadf");
        r.isKinematic = false;
        r.velocity = new Vector2(-5,5);
        r.WakeUp();
        Time.timeScale = 1f;
        Invoke("three", 2f);
    }

    void three() {
        v = r.velocity;
        r.isKinematic = true;
    }


    void FixedUpdate () {
        Debug.Log("adfadfadfdafadfadf");
    }
}
