using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Security.Permissions;
using System;
using UnityEditor;
using System.IO;
using System.Collections;


//CONVERT THIS TO CLIENT NOT SERVER!
// This class with handel commmunications with C++ FANN, collect data, and control agent GameObject
public class ReinforcementAgentPhysics1 : MonoBehaviour
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

    private float[] actions;
    private float[] states;

    private float reward = 0f;

    private int actionSize = 2;
    private int stateSize = 1;

    private int actionType = -1;
    private int workType = -1;

    private bool quit = false;

    private const int WORK_APPLY = 0;
    private const int WORK_RESET = 2;
    private const int WORK_GET = 1;
    private const int WORK_NULL = -1;


    private const int ACTION_NULL = -1;
    private const int ACTION_1 = 0;
    private const int ACTION_2 = 1;


    // Public values which can be set from the Unity inspector 
    public bool connect = false;
    public GameObject testPrefab;
    public float timeScale = 1f;
    public float epslion = 1f;
    public float epslionDecay = 0f;

    private GameObject test = null;
    private Rigidbody2D rBodies; // Size set by user
    private float angle;

    System.Random randomGenerator = new System.Random();
    // Use this for initialization
    private void Start()
    {
        Application.runInBackground = true;

        Reset();
        actions = new float[actionSize];
        states = new float[stateSize];

        
    }

    private void GetState()
    {
        float bDeg = 0;

        float deg = test.transform.eulerAngles.z - bDeg;
        if (deg >= 180)
        {
            deg = deg - 360f;
        }
        states[0] = deg * Mathf.Deg2Rad;
    }

    int theState = 0;
    float[] qval;
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

        if (quit == false)
        {
            try
            {
                LoopBack:
                if (theState == 0)
                {
                    GetState();
                    PrintState(1);
                    SendArray(states); //send state to network

                    qval = ReceiveFloatArray(actionSize); //receive prediction

                    //2. Pick with epsilion greedy method
                    actionType = ACTION_NULL;
                    float chance = (float)randomGenerator.NextDouble();
                    if (chance < epslion)
                    {
                        epslion -= epslionDecay;
                        actionType = randomGenerator.Next(actionSize);
                    }
                    else
                    {
                        float largestQValue = qval[ACTION_1];
                        actionType = ACTION_1;
                        for (int i = ACTION_2; i < actionSize; i++)
                        {
                            if (qval[i] >= largestQValue)
                            {
                                largestQValue = qval[i];
                                actionType = i;
                            }
                        }
                        Debug.Log("MAX Q " + epslion);
                    }

                    ApplyAction();

                    theState++;
                }
                else if (theState == 1) {
                    //4. Get reward for current state
                    GetState();

                    reward = 0f;
                    float deg = states[0] * (180f / (float)Math.PI);
                    deg = Math.Abs(deg);
                    reward = (180f - deg) / 180f;


                    reward = reward * reward * reward * reward;

                    
                    SendArray(states); //send state to netowrk
                    float[] newQ = ReceiveFloatArray(actionSize); //receive new prediction
                                                                  //-----------------  

                    //6. Calculate backpropagation error 
                    float largestNewQValue = newQ[ACTION_1];
                    for (int i = ACTION_2; i < actionSize; i++)
                    {
                        if (newQ[i] >= largestNewQValue)
                        {
                            largestNewQValue = newQ[i];
                        }
                    }

                    float update = reward + (largestNewQValue * 0f);
                    float[] expectedAction = new float[actionSize];
                    for (int i = 0; i < actionSize; i++)
                    {
                        expectedAction[i] = qval[i];
                    }
                    expectedAction[actionType] = update;


                    SendArray(expectedAction);

                    actionType = ACTION_NULL;
                    theState = 0;

                    GetState();
                    PrintState(0);
                    goto LoopBack;
                }
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
            Application.Quit();
        }
    }

    public void PrintState(int num)
    {
        Debug.Log(num+" "+states[0]);
    }

    public void Reset()
    {
        Destroy(test);
        test = Instantiate(testPrefab) as GameObject;
        rBodies = test.transform.GetComponent<Rigidbody2D>();


    }

    private void ApplyAction()
    {
        if (actionType == ACTION_1)
        {
            rBodies.angularVelocity = 250f;
        }
        else if (actionType == ACTION_2)
        {
            rBodies.angularVelocity = -250f;
        }
    }

    // Recieve float array 
    private float[] ReceiveFloatArray(int count)
    {
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

    // Test sending an array with 5 float values 
    private void TestSendArray()
    {
        float[] sendArray = { 1.24f, -0.425f, 67.245f, -101.45f, 2.7778f };
        SendArray(sendArray);
    }

    // Test receiveing an array with 5 float values
    private void TestReceiveArray()
    {
        float[] rcvArray = ReceiveFloatArray(5);
        Debug.Log(rcvArray[0] + " " + rcvArray[1] + " " + rcvArray[2] + " " + rcvArray[3] + " " + rcvArray[4]);
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

