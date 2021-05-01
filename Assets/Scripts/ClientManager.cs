using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Net.Sockets;
using UnityEngine;
using System.Net;
using Data;

public class ClientManager : MonoBehaviour
{
    public NetConfiguration netConfiguration;
    private UdpClient ClientListener;
    private IPEndPoint ServerIPEndPoint;

    public static ClientManager Instance = null;
    private readonly Message PING = new Message() { Type = MessageType.Ping };

    private PhysicsScene client_physics_scene;
    private Scene client_scene;

    private void Start()
    {
        Instance = this;
        Application.runInBackground = true;
        IPAddress iPAddress = IPAddress.Parse(netConfiguration.HostAddress);
        ServerIPEndPoint = new IPEndPoint(iPAddress, netConfiguration.listenerPort);
        ClientListener = new UdpClient();

        Message message = new Message()
        {
            Type = MessageType.Client_Request_Connect,
            Content = new object()
        };
        byte[] Request = GameUtility.MessageSerialize(message);
        // 发送连接请求
        ClientListener.Send(Request, Request.Length, ServerIPEndPoint);
        Debug.Log("客户端启动 并发送 连接请求");
        InvokeRepeating("SendPing", 1, 1);
    }

    private void Update()
    {
        if (ClientListener.Available > 0)
        {
            IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            Message message = GameUtility.MessageDeserialization(ClientListener.Receive(ref remoteEndpoint));
            AnalyzeMessage(remoteEndpoint, message);
        }
    }

    public void OnDestroy()
    {
        ClientListener.Close();
    }

    private void AnalyzeMessage(IPEndPoint remoteEndpoint, Message message)
    {
        Debug.Log($"Received {message.Type} from address: {remoteEndpoint.Address}");

        //byte[] response = Encoding.UTF8.GetBytes(serverConfiguration.lanDiscoveryResponse + " " + GameSession.serverSession.serverPort + " " + goInGameServerSystem.connectedPlayers + " " + GameSession.serverSession.numberOfPlayers + " " + GameSession.serverSession.laps + " " + GameSession.serverSession.hostName);

        //if (goInGameServerSystem.connectedPlayers < GameSession.serverSession.numberOfPlayers && receivedMessage.Equals(serverConfiguration.lanDiscoveryRequest))
        //{
        //    Debug.Log("response to address: " + remoteEndpoint.Address);
        //    listener.Send(response, response.Length, remoteEndpoint);
        //}
    }

    private void SendPing()
    {
        byte[] response = GameUtility.MessageSerialize(PING);
        ClientListener.Send(response, response.Length, ServerIPEndPoint);
        //while (Application.isPlaying)
        //{
        //    yield return new WaitForSeconds(1);

        //}

    }
}
