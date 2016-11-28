using UnityEngine;
using System.Collections;

public class SensorTouch : MonoBehaviour {

    public Transform point;
    public float distance = -1f;
    int num = 0;
    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter(Collider other)
    {
        num++;
    }

    void OnTriggerExit(Collider other)
    {
        num--;
        if(num==0)
            distance = -1f;
    }

    void OnTriggerStay(Collider other)
    {
        distance = Vector3.Distance(other.ClosestPointOnBounds(point.position),point.position);
        //Debug.Log(distance);
    }
}
