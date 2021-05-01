using UnityEngine.SceneManagement;
using System.Net.Sockets;
using UnityEngine;
using System.Net;
using Data;

public class ServerManager : MonoBehaviour
{
    public NetConfiguration netConfiguration;
    private UdpClient ServerListener;
    public static ServerManager Instance = null;

    private readonly Message PONG = new Message() { Type = MessageType.Pong };
    private PhysicsScene server_physics_scene;
    private Scene server_scene;
    private void Start()
    {
        Instance = this;
        Application.runInBackground = true;
        ServerListener = new UdpClient(netConfiguration.listenerPort);
        Debug.Log("服务端启动 开启监听");
        server_scene = SceneManager.LoadScene("physics_scene", new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
        server_physics_scene = server_scene.GetPhysicsScene();
    }

    private void Update()
    {
        if (ServerListener.Available > 0)
        {
            IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] byteMessage = ServerListener.Receive(ref remoteEndpoint);
            Message message = GameUtility.MessageDeserialization(byteMessage);
            AnalyzeMessage(remoteEndpoint, message);
        }
    }

    public void OnDestroy()
    {
        ServerListener.Close();
    }

    private void AnalyzeMessage(IPEndPoint remoteEndpoint, Message message)
    {
        Debug.Log($"Received {message.Type} from address: {remoteEndpoint.Address}");

        switch (message.Type)
        {
            case MessageType.Ping:
                byte[] response = GameUtility.MessageSerialize(PONG);
                ServerListener.Send(response, response.Length, remoteEndpoint);
                break;
            case MessageType.Client_Request_Connect:
                break;
            case MessageType.Client_Request_DisConnect:
                break;
            case MessageType.Client_Request_Operation:
                break;
            default:
                break;
        }
        
        //byte[] response = Encoding.UTF8.GetBytes(serverConfiguration.lanDiscoveryResponse + " " + GameSession.serverSession.serverPort + " " + goInGameServerSystem.connectedPlayers + " " + GameSession.serverSession.numberOfPlayers + " " + GameSession.serverSession.laps + " " + GameSession.serverSession.hostName);

        //if (goInGameServerSystem.connectedPlayers < GameSession.serverSession.numberOfPlayers && receivedMessage.Equals(serverConfiguration.lanDiscoveryRequest))
        //{
        //    Debug.Log("response to address: " + remoteEndpoint.Address);
        //    listener.Send(response, response.Length, remoteEndpoint);
        //}
    }

}
