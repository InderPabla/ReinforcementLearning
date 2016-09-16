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
    private void Start () {

        thread = new Thread(Worker); //Thread object to run Woker method
        thread.IsBackground = true; //not a dameon thread, must run in foreground
        thread.Start(); // start thread
    }

    // Update is called once per frame
    private void Update () {
	
	}

    //Worker thread which will deal with connected to C++ FANN
    private void Worker()
    {
        //Get local IP address and start listening to TCP socket on port 12345
        address = Dns.GetHostEntry("localhost").AddressList[0];
        serverSocket = new TcpListener(address, 12345);
        serverSocket.Start();
       
        acceptSocket = serverSocket.AcceptTcpClient();  //Accept data stream from TCP server socket
        stream = acceptSocket.GetStream(); //Client connected to this socket



        float[] rcvArray = ReceiveFloatArray(5);

        Debug.Log(rcvArray[0] + " ONEd\n" +
            rcvArray[1] + " TWOf\n" +
            rcvArray[2] + " THREE\n" +
            rcvArray[3] + " FOUR\n" +
            rcvArray[4] + " FIVE");

        float[] sendArray = {1.24f,-0.425f,67.245f,-101.45f,2.7778f};
        SendArray(sendArray);
    }

    // Recieve float array 
    private float[] ReceiveFloatArray(int count) {
        int sizeOfFloat = 4;
        int byteCount = sizeOfFloat * count;
        byte[] bytes = new byte[byteCount];
        stream.Read(bytes, 0, bytes.Length);
        float[] bytesToFloat = new float[count];

        for (int i = 0; i < byteCount; i += sizeOfFloat) {

            byte[] tempBytes = new byte[sizeOfFloat];

            // Flip from little endian to big endian
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
    private void SendArray(float[] array) {
        int count = array.Length;
        int sizeOfFloat = 4;
        int byteCount = array.Length * sizeOfFloat;
        
        byte[] convertedData = new byte[byteCount];
        Buffer.BlockCopy(array, 0, convertedData, 0, convertedData.Length);

        stream.Write(convertedData, 0, convertedData.Length);
    }

    //If application quits makes sure all sockets and streams are closed
    private void OnApplicationQuit()
    {
        // If thread is running
        if (thread.IsAlive)
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
                Debug.Log(thread.IsAlive); // True (must be false)
            }
            catch (Exception error)
            {
                Debug.Log(error);
            }
        }
    }

    // Close all connection sockets and streams
    private void CloseConnection() {
        serverSocket.Stop();
        stream.Close();
        acceptSocket.Close();
    }

    // Forced security close of this thread
    [SecurityPermissionAttribute(SecurityAction.Demand, ControlThread = true)]
    private void KillTheThread()
    {
        thread.Abort();
    }
}

