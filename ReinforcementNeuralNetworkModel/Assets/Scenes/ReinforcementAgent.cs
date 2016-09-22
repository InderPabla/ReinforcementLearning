using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Security.Permissions;
using System;
using UnityEditor;
using System.IO;
using System.Collections;

// This class with handel commmunications with C++ FANN, collect data, and control agent GameObject
public class ReinforcementAgent : MonoBehaviour
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
    private int stateSize = 3;

    private int actionType = -1;
    private int workType = -1;

    private bool quit = false;

    private const int WORK_APPLY = 0;
    private const int WORK_RESET = 2;
    private const int WORK_GET = 1;
    private const int WORK_NULL = -1;
    private const int WORK_WAIT_UNPAUSE = 3;

    private const int ACTION_NULL = -1;
    private const int ACTION_1 = 0;
    private const int ACTION_2 = 1;

    //private float epslion = 1f;
    //private float epslionDecay = 0f;//0.00001f;

    //private Semaphore critical;

    // Public values which can be set from the Unity inspector 
    public bool connect = false;
    public GameObject testPrefab;
    public float timeScale = 1f;
    public float epslion = 1f;
    public float epslionDecay = 0f;

    private GameObject test = null;
    private Rigidbody2D[] rBodies /*= new Rigidbody2D[2]*/; // Size set by user
    private Vector2[] rVelocity;
    private float[] rAngularVelocity;

    // Use this for initialization
    private void Start()
    {
        Application.runInBackground = true;

        Reset();

        Time.timeScale = timeScale;

        //critical = new Semaphore(1,1);
        actions = new float[actionSize];
        states = new float[stateSize];

        if (connect == true)
        {
            OpenConnection();
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (quit == false)
        {
            //critical.WaitOne();
            if (workType == WORK_APPLY)
            {
                Unpause();
                workType = WORK_WAIT_UNPAUSE;
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
            else if (workType == WORK_WAIT_UNPAUSE)
            {
                ApplyAction();

                actionType = ACTION_NULL;
                workType = WORK_GET;
            }
            //critical.Release();
        }
        else
        {
            Application.Quit();
        }
    }

    private void GetState()
    {
        states[0] = rBodies[1].velocity.x;
        states[1] = rBodies[0].angularVelocity; //pole angualr 
        states[2] = rBodies[0].transform.eulerAngles.z * Mathf.Deg2Rad;
    }

    public void Reset()
    {
        /*rBodies[0].velocity = Vector2.zero;
        rBodies[0].angularVelocity = 0f;

        rBodies[1].velocity = Vector2.zero;
        rBodies[1].angularVelocity = 0f;

        rBodies[1].transform.position = new Vector3(0,0,0f);
        rBodies[0].transform.eulerAngles = new Vector3(0, 0, 0f);*/

        Destroy(test);
        test = Instantiate(testPrefab) as GameObject;
        rBodies = test.transform.GetComponentsInChildren<Rigidbody2D>();
        rVelocity = new Vector2[rBodies.Length];
        rAngularVelocity = new float[rBodies.Length];
        Pause();


    }

    public void Pause()
    {
        //rBodies[0].Sleep();
        //rBodies[1].Sleep();

        for (int i = 0; i < rBodies.Length; i++)
        {
            rVelocity[i] = rBodies[i].velocity;
            rAngularVelocity[i] = rBodies[i].angularVelocity;
            if(i == 1)
                Debug.Log(rVelocity[i]);
            rBodies[i].isKinematic = true;
            //rBodies[i].Sleep();
        }

    }

    public void Unpause()
    {
        for (int i = 0; i < rBodies.Length; i++)
        {
            rBodies[i].isKinematic = false;
            
            //rBodies[i].AddForce(rBodies[i].mass * rVelocity[i] / Time.deltaTime);
            //rBodies[i].AddTorque(rBodies[i].mass * rAngularVelocity[i] / Time.deltaTime);
            /*if(i == 1)
                Debug.Log("Before Unpause "+ rAngularVelocity[i]);*/
            //rBodies[i].velocity = rVelocity[i];
            //rBodies[i].angularVelocity = rAngularVelocity[i];
            rBodies[i].WakeUp();
        }
        //rBodies[0].WakeUp();
        //rBodies[1].WakeUp();
    }

    private void ApplyAction()
    {
        rBodies[0].velocity = rVelocity[0];
        rBodies[0].angularVelocity = rAngularVelocity[0];

        rBodies[1].angularVelocity = rAngularVelocity[1];

        if (actionType == ACTION_1)
        {
            

            //rBodies[1].AddForce(new Vector2(rBodies[1].mass * 50 / Time.deltaTime, 0f));
            //Debug.Log("After Upause " + rBodies[1].velocity);
            rBodies[1].velocity += (new Vector2(50 * Time.deltaTime, 0f) + rVelocity[1]);
        }
        else if (actionType == ACTION_2)
        {
            //rBodies[1].AddForce(new Vector2(rBodies[1].mass * -50 / Time.deltaTime, 0f));
            rBodies[1].velocity += (new Vector2(-50 * Time.deltaTime, 0f) + rVelocity[1]);
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

                SendArray(states); //send state to netowrk


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
                    float largestQValue = float.MinValue;
                    for (int i = 0; i < actionSize; i++)
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
                //critical.WaitOne();
                float failDegree = 45f;
                float pole1AngleDegree = (states[2] * 180f) / (float)Math.PI;
                /*if (pole1AngleDegree >= 90f && pole1AngleDegree <= 270f)
                    reward = -10f;
                else if (pole1AngleDegree >= failDegree && pole1AngleDegree <= 90f)
                    reward = -1f;
                else if (pole1AngleDegree <= (360f - failDegree) && pole1AngleDegree >= 270f)
                    reward = -1f;
                else
                    reward = 10f;*/

                if (pole1AngleDegree >= (failDegree / 2) && pole1AngleDegree <= (360 - (failDegree / 2f)))
                    reward = -100f;
                else if (pole1AngleDegree <= failDegree)
                {
                    reward = ((failDegree - pole1AngleDegree) / failDegree) * 100f;
                    reward = reward * reward;
                }
                else if (pole1AngleDegree >= (360f - failDegree))
                {
                    reward = ((failDegree - (360 - pole1AngleDegree)) / failDegree) * 100f;
                    reward = reward * reward;
                }



                if (!((pole1AngleDegree <= failDegree && pole1AngleDegree >= 0) || (pole1AngleDegree <= 360 && pole1AngleDegree >= (360 - failDegree))))
                {
                    reset = true;
                }
                //critical.Release();
                //----------------- 


                //5. Make predcition on sate after action
                Work(WORK_GET, ACTION_NULL); //get state after acction 

                //critical.WaitOne();
                SendArray(states); //send state to netowrk
                float[] newQ = ReceiveFloatArray(actionSize); //receive new prediction
                //-----------------  

                //6. Calculate backpropagation error 
                float largestNewQValue = 0f;
                for (int i = 0; i < actionSize; i++)
                {
                    if (newQ[i] >= largestNewQValue)
                    {
                        largestNewQValue = newQ[i];
                    }
                }

                float update = reward + (largestNewQValue * 0.9f);
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

