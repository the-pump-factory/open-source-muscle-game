using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class loginServerTest : MonoBehaviour
{

    public string hostName;
    public int port;

    public bool connect;
    public bool disconnect;

    private TcpClient client;

    public string message;
    public bool send;
    public bool sendASCII;

    void Update()
    {
        if (connect)
        {
            connect = false; 
            Connect();
        }
        if (disconnect)
        {
            disconnect = false;
            Disconnect();
        }
        if (send)
        {
            send = false;
            Send(false);
        }
        if (sendASCII)
        {
            sendASCII = false;
            Send(true);
        }

        Receive();
    }

    public void Connect()
    {
        Disconnect();

        client = new TcpClient(hostName, port);
        Debug.Log("Connected");
    }

    public void Send(bool ascii)
    {
        if (client == null) return;

        if (ascii)
        {
            client.GetStream().Write(System.Text.Encoding.ASCII.GetBytes(message));
        } 
        else
        {
            client.GetStream().Write(System.Text.Encoding.UTF8.GetBytes(message));
        }
        Debug.Log($"Sent '{message}'");
    }
    byte[] buffer = new byte[1024];
    public void Receive()
    {
        if (client != null && client.Available > 0)
        {
            int bytesRead = client.GetStream().Read(buffer, 0, buffer.Length);
            Debug.Log($"Received '{System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead)}'"); 
        }
    }

    public void Disconnect()
    {
        if (client != null)
        {
            client.GetStream().Write(System.Text.Encoding.ASCII.GetBytes("cya"));
            client.Close();
            client = null;
            Debug.Log("Disconnected"); 
        }
    }

    protected void OnDestroy()
    {
        Disconnect();
    }

}
