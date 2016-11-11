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
public class ReinforcementAgentPhysics : MonoBehaviour
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
    
    // Use this for initialization
    private void Start()
    {
        Application.runInBackground = true;

        Reset();

        //critical = new Semaphore(1,1);
        actions = new float[actionSize];
        states = new float[stateSize];

        if (connect == true)
        {
            OpenConnection();
        }
    }


    // Update is called once per frame
    private void FixedUpdate()
    {
        if (quit == false)
        {
            if (workType == WORK_APPLY)
            {
                Unpause();
                ApplyAction();
                actionType = ACTION_NULL;
                workType = WORK_GET;
            }
            else if (workType == WORK_GET)
            {
                GetState();
                Pause();
                workType = WORK_NULL;
            }
            else if (workType == WORK_RESET)
            {
                Reset();
                workType = WORK_NULL;

            }
          
           
        }
        else
        {
            Application.Quit();
        }
    }

    private void GetState()
    {
        float bDeg = 180f;

        float deg = test.transform.eulerAngles.z - bDeg;
        if (deg >= 180)
        {
            deg = deg - 360f;
        }
        states[0] = deg * Mathf.Deg2Rad;
    }

    public void Reset()
    {
        Destroy(test);
        test = Instantiate(testPrefab) as GameObject;
        rBodies = test.transform.GetComponent<Rigidbody2D>();
        Pause();


    }

    public void Pause()
    {
        angle = rBodies.angularVelocity;
        rBodies.isKinematic = true;

    }

    public void Unpause()
    {
        rBodies.isKinematic = false;
        rBodies.WakeUp();
        rBodies.angularVelocity = angle;
    }

    private void ApplyAction()
    {

        //Debug.Log(rBodies[1].velocity);
        if (actionType == ACTION_1)
        {
            rBodies.angularVelocity = 250f;
        }
        else if (actionType == ACTION_2)
        {
            rBodies.angularVelocity = -250f;
        }
    }



    //Worker thread which will deal with connected to C++ FANN
    private void Worker()
    {

        //Get local IP address and start listening to TCP socket on port 12345
        address = Dns.GetHostEntry(addressName).AddressList[0];
        serverSocket = new TcpListener(address, port);
        serverSocket.Start();

        acceptSocket = serverSocket.AcceptTcpClient();  // Accept data stream from TCP server socket
        stream = acceptSocket.GetStream(); // Client connected to this socket
        Debug.Log("Client Has Connected.");
        System.Random randomGenerator = new System.Random();

        while (quit == false)
        {
            try
            {
                //Might need to add semaphores! There will be a context swtich from here to update!
                bool reset = false;
                //1. Make prediction on current state 
                Work(WORK_GET, ACTION_NULL); //get current state 

                //critical.WaitOne();

                SendArray(states); //send state to network


                float[] qval = ReceiveFloatArray(actionSize); //receive prediction
                //-----------------  

                
                //2. Pick with epsilion greedy method
                int someAction = ACTION_NULL;
                float chance = (float)randomGenerator.NextDouble();
                if (chance < epslion)
                {
                    epslion -= epslionDecay;
                    someAction = randomGenerator.Next(actionSize);
                }
                else
                {
                    float largestQValue = qval[ACTION_1];
                    someAction = ACTION_1;
                    for (int i = ACTION_2; i < actionSize; i++)
                    {
                        if (qval[i] >= largestQValue)
                        {
                            largestQValue = qval[i];
                            someAction = i;
                        }
                    }
                    Debug.Log("MAX Q " + epslion);
                }
                //critical.Release();
                //----------------- 


                //3. Apply action and get state
                Work(WORK_APPLY, someAction);


                //4. Get reward for current state
                reward = 0f;
                float deg = states[0] * (180f / (float)Math.PI);
                deg = Math.Abs(deg);
                reward = (180f - deg) / 180f;


                reward = reward * reward * reward * reward;
                //reward = reward / 2f; 
                //----------------- 


                //5. Make predcition on sate after action
                Work(WORK_GET, ACTION_NULL); //get state after acction 

                //critical.WaitOne();
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
                expectedAction[someAction] = update;


                SendArray(expectedAction);
                //critical.Release();

                if (reset == true)
                {
                    Work(WORK_RESET, ACTION_NULL);
                }
                //-----------------  

            }
            catch (Exception err)
            {
                Debug.LogError("NetworkStream Write Error");
                Debug.LogError(err);
                quit = true;
            }
        }

        Debug.Log("Client Has Diconnected.");
    }

    private void Work(int workType, int actionType)
    {
        this.actionType = actionType;
        this.workType = workType;
        while (this.workType != WORK_NULL) { }; // wait
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

    private void TestActionAndWork()
    {
        // intial getting of state
        Work(WORK_GET, ACTION_NULL); // Work till work type is null

        Debug.Log("Pre Action: " + states[0] + " " + states[1] + " " + states[2]);

        //applying action 1
        Work(WORK_APPLY, ACTION_1); // Work till work type is null

        Debug.Log("Post Action 1: " + states[0] + " " + states[1] + " " + states[2]);

        //applying action 2
        Work(WORK_APPLY, ACTION_2); // Work till work type is null

        Debug.Log("Post Action 2: " + states[0] + " " + states[1] + " " + states[2]);
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

    private void OpenConnection()
    {
        thread = new Thread(Worker); // Thread object to run Woker method
        thread.IsBackground = true; // Not a dameon thread, must run in foreground
        thread.Start(); // Start thread
    }

    // Forced security close of this thread
    [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
    private void KillTheThread()
    {
        thread.Abort();
    }
}

