using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Security.Permissions;
using System;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.Vehicles.Car;

//CONVERT THIS TO CLIENT NOT SERVER!
// This class with handel commmunications with C++ FANN, collect data, and control agent GameObject
public class ReinforcementAgentCar : MonoBehaviour
{

    private Thread thread; // Thread worker

    // TCP sockets and streams and address used to connect to C++
    private TcpListener serverSocket;
    private TcpClient acceptSocket;
    private NetworkStream stream;

    // Address connection details
    private IPAddress address;
    private string addressName = "localhost";
    private int port = 12345;


    // Public values which can be set from the Unity inspector 
    public bool connect = false;

    public float epslion = 1f;
    public float minEpslionAmount = 0.1f;
    public float epslionDecay = 0f;
    public int actionSize = 2;
    public int stateSize = 2;
    public float gamma = 0f;

    private bool quit = false;
    private float[] qVal;
    private float[] state;
    private int agentAction = -1;
    private int agentState = 0;
    private float reward = 0f;
    private float update = 0f;

    public CarController controller;
    public SensorTouch[] touch;
    public CarCollider carCollider;

    // Use this for initialization
    private void Start()
    {
        Application.runInBackground = true;
        Reset();
        //controller.Move(0f, 1f, 1f, 0f);
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (connect == false)
        {
            //Get local IP address and start listening to TCP socket on port 12345
            address = Dns.GetHostEntry(addressName).AddressList[0];
            serverSocket = new TcpListener(address, port);
            serverSocket.Start();

            acceptSocket = serverSocket.AcceptTcpClient();  // Accept data stream from TCP server socket
            stream = acceptSocket.GetStream(); // Client connected to this socket
            Debug.Log("Client Has Connected.");
            
            connect = true;
        }

        if (quit == false && connect == true)
        {
            try
            {
                StateUpdate();
            }
            catch (Exception err)
            {
                Debug.LogError("NetworkStream Write Error");
                Debug.LogError(err);
                quit = true;
            }
        }
        else
        {
            Application.Quit(); //quit if quit is false!
        }
    }

    public void Reset()
    {
        controller.transform.position = Vector3.zero;
        controller.transform.eulerAngles = Vector3.zero;
        controller.GetComponent<Rigidbody>().velocity = Vector3.zero;
        controller.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }


    public void StateUpdate()
    {
    STATE_ZERO:

        ResetTest();
        if (agentState == 0)
        {
            qVal = GetStatePrediction(); //get initial prediction
            agentAction = GetAction();
            ApplyAction();
            agentState = 1;
        }
        else if (agentState == 1) //after new move
        {

            float[] oldState = state;
            float[] newQVal = GetStatePrediction(); //get initial prediction

            reward = GetReward();
            float maxQValue = GetMaxValue(newQVal);

            float[] expectedQVal = new float[actionSize];
            for (int i = 0; i < actionSize; i++)
                expectedQVal[i] = qVal[i];

            update = reward + (gamma * maxQValue);
            expectedQVal[agentAction] = update;

            SendArray(expectedQVal);
            agentState = 0;


            goto STATE_ZERO;
        }

    }

    public void ResetTest()
    {
        
        if (carCollider.inColl == true) {
            controller.transform.position = Vector3.zero;
            controller.transform.eulerAngles = Vector3.zero;
            controller.GetComponent<Rigidbody>().velocity = Vector3.zero;
            controller.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }

    public float GetReward()
    {
        if (carCollider.inColl == false)
            return 0.25f;
        else
            return -1;

    }


    public float GetMaxValue(float[] qValue)
    {
        float value = qValue[0];
        for (int i = 1; i < qValue.Length; i++)
        {
            if (value >= qValue[i])
            {
                value = qValue[i];
            }
        }

        return value;
    }

    public void ApplyAction()
    {
        if (agentAction == 0)
        {
            controller.Move(-1f,1f,1f,0f);
        }
        if (agentAction == 1)
        {
            controller.Move(0f, 1f, 1f, 0f);
        }
        if (agentAction == 2)
        {
            controller.Move(1f, 1f, 1f, 0f);
        }
    }



    public float[] GetStatePrediction()
    {
        state = GetState();
        SendArray(state);
        return ReceiveFloatArray();
    }

    public int GetAction()
    {
        int action = -1;
        float chance = UnityEngine.Random.Range(0f, 1f);
        if (/*minEpslionAmount> epslion &&*/ chance < epslion)
        {
            epslion -= epslionDecay;

            return UnityEngine.Random.Range(0, actionSize);
        }


        float largestQValue = qVal[0];
        action = 0;
        for (int i = 1; i < actionSize; i++)
        {
            if (qVal[i] > largestQValue)
            {
                largestQValue = qVal[i];
                action = i;
            }
        }

        return action;
    }



    public float[] GetState()
    {
        float[] state = new float[stateSize];
        state[0] = controller.GetComponent<Rigidbody>().velocity.magnitude/10f;
        for (int i = 0; i < 5; i++)
            state[i + 1] = touch[i].distance == -1f ? 1f : touch[i].distance / 6f;

        return state;
    }

    // Recieve float array 
    private float[] ReceiveFloatArray()
    {
        int count = actionSize;
        int sizeOfFloat = 4;
        int byteCount = sizeOfFloat * count;
        byte[] bytes = new byte[byteCount];
        stream.Read(bytes, 0, bytes.Length);
        float[] bytesToFloat = new float[count];

        for (int i = 0; i < byteCount; i += sizeOfFloat)
        {

            byte[] tempBytes = new byte[sizeOfFloat];

            tempBytes[3] = bytes[i + 3];
            tempBytes[2] = bytes[i + 2];
            tempBytes[1] = bytes[i + 1];
            tempBytes[0] = bytes[i];

            float value = BitConverter.ToSingle(tempBytes, 0); //convert from float to 

            int index = i / sizeOfFloat;
            bytesToFloat[index] = value;
        }

        return bytesToFloat;
    }

    // Send float array 
    private void SendArray(float[] array)
    {
        int count = array.Length;
        int sizeOfFloat = 4;
        int byteCount = array.Length * sizeOfFloat;

        byte[] convertedData = new byte[byteCount];
        Buffer.BlockCopy(array, 0, convertedData, 0, convertedData.Length);

        stream.Write(convertedData, 0, convertedData.Length);
    }

    private void SendInt(int integer)
    {
        byte[] convertedData = BitConverter.GetBytes(integer);
        stream.Write(convertedData, 0, convertedData.Length);
    }

    public float Sigmoid(float x)
    {
        return (float)(2 / (1 + Math.Exp(-2 * x)) - 1);
    }

    // If application quits makes sure all sockets and streams are closed
    private void OnApplicationQuit()
    {
        // If thread is running
        if (connect == true && thread.IsAlive)
        {
            // Close sockets and streams
            try
            {
                CloseConnection();
            }
            catch (SocketException error)
            {
                Debug.Log(error);
            }

            // Kill this thread
            try
            {
                KillTheThread();
                Debug.Log("Worker Thread State: " + thread.IsAlive);
            }
            catch (Exception error)
            {
                Debug.Log(error);
            }
        }
    }

    // Close all connection sockets and streams
    private void CloseConnection()
    {
        try
        {
            serverSocket.Stop();
        }
        catch (Exception error)
        {

        }

        try
        {
            stream.Close();
        }
        catch (Exception error)
        {

        }

        try
        {
            acceptSocket.Close();
        }
        catch (Exception error)
        {

        }
    }

    // Forced security close of this thread
    [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
    private void KillTheThread()
    {
        thread.Abort();
    }
}

