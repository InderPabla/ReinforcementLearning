using UnityEngine;
using System.Collections;

public class CarCollider : MonoBehaviour {
    public bool inColl = false;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
       
	}

    void OnCollisionExit(Collision collisionInfo)
    {
        Debug.Log(inColl);
        inColl = false;
    }

    void OnCollisionStay(Collision collisionInfo)
    {
        Debug.Log(inColl);
        inColl = true;
    }
}
