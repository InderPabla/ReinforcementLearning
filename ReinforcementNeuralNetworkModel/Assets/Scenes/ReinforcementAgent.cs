using UnityEngine;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Security.Permissions;
using System;
using UnityEditor;
using System.IO;

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

    private Semaphore critical; //critical update 

    private float[] actions;
    private float[] states;
    private float reward = 0f;
    private int actionSize = 2;
    private int stateSize = 3;

    private bool quit = false;

    // Public values which can be set from the Unity inspector 
    public bool connect = false;
    public Rigidbody2D[] rBodies = new Rigidbody2D[2]; // Size set by user


    // Use this for initialization
    private void Start ()
    {
        Application.runInBackground = true;
        actions = new float[actionSize];
        states = new float[stateSize];

        critical = new Semaphore(1, 1);
        if (connect == true) {
            OpenConnection();
        }
    }

    // Update is called once per frame
    private void Update ()
    {
        if (quit == false)
        {
            critical.WaitOne();
            //Critical region update
             
            critical.Release();
        }
        else {
            Application.Quit();
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

        while (quit == false)
        {
           
            try
            {
                critical.WaitOne();
                SendArray(states);
                float[] rcvArray = ReceiveFloatArray(2);
                actions[0] = rcvArray[0];
                actions[1] = rcvArray[1];
                critical.Release();
                
                

            }
            catch (IOException err) {
                Debug.LogError("NetworkStream Write Error");
                quit = true;
            }
        }

        Debug.Log("Client Has Diconnected.");
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
        Debug.Log(rcvArray[0]+" "+rcvArray[1]+" "+rcvArray[2]+" "+rcvArray[3]+" "+rcvArray[4]);
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
                Debug.Log("Worker Thread State: "+thread.IsAlive); 
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
        try {
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
        catch (Exception error) {

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

