using UnityEngine;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Security.Permissions;
using System;
using System.Text;

// This class with handel commmunications with C++ FANN, collect data, and control agent GameObject
public class ReinforcementAgent : MonoBehaviour {

    private Thread thread;
    private TcpListener serverSocket;
    private TcpClient acceptSocket;
    private IPAddress address;
    private NetworkStream stream;

    // Use this for initialization
    void Start () {

        thread = new Thread(Worker); //Thread object to run Woker method
        thread.IsBackground = true; //not a dameon thread, must run in foreground
        thread.Start(); // start thread
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    //Worker thread which will deal with connected to C++ FANN
    public void Worker()
    {
        //Get local IP address and start listening to TCP socket on port 12345
        address = Dns.GetHostEntry("localhost").AddressList[0];
        serverSocket = new TcpListener(address, 12345);
        serverSocket.Start();

        //Accept data stream from TCP server socket
        acceptSocket = serverSocket.AcceptTcpClient();
        stream = acceptSocket.GetStream();

        //Client connected to this socket

        int arrayCount = 5;
        int sizeOfFloat = 4;
        int byteCount = sizeOfFloat * arrayCount;
        byte[] bytes = new byte[byteCount];
        stream.Read(bytes, 0, bytes.Length);
        float[] bytesToFloat = new float[arrayCount];

        for (int i = 0; i < byteCount; i += sizeOfFloat)
        {

            byte[] tempBytes = new byte[sizeOfFloat];

            tempBytes[0] = bytes[i + 3];
            tempBytes[1] = bytes[i + 2];
            tempBytes[2] = bytes[i + 1];
            tempBytes[3] = bytes[i];

            float value = BitConverter.ToSingle(tempBytes, 0);
            int index = i / sizeOfFloat;
            bytesToFloat[index] = value;
        }

        Debug.Log(bytesToFloat[0] + " ONET\n" +
            bytesToFloat[1] + " TWO\n" +
            bytesToFloat[2] + " THREE\n" +
            bytesToFloat[3] + " FOUR\n" +
            bytesToFloat[4] + " FIVE");

    }



    //If application quits makes sure all sockets and streams are closed
    void OnApplicationQuit()
    {
        // If thread is running
        if (thread.IsAlive)
        {
            // Close sockets and streams
            try
            {
                serverSocket.Stop();
                stream.Close();
                acceptSocket.Close();
            }
            catch (SocketException error)
            {
                Debug.Log(error);
            }

            // Kill this thread
            try
            {
                KillTheThread();
                Debug.Log(thread.IsAlive); // True (must be false)
            }
            catch (Exception error)
            {
                Debug.Log(error);
            }
        }
    }

    // Forced security close of this thread
    [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
    private void KillTheThread()
    {
        thread.Abort();
    }
}

