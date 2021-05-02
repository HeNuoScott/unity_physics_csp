using UnityEngine.SceneManagement;
using System.Collections.Generic;
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

    private uint server_snapshot_rate;//服务器帧率
    private uint server_tick_number;
    private uint server_tick_accumulator;
    public GameObject server_player;
    private PhysicsScene server_physics_scene;
    private Scene server_scene;
    private float packet_loss_chance; // 丢包率
    private float latency; // 延迟
    private Queue<InputMessage> server_input_msgs;//等待处理的用户输入
    private Queue<StateMessage> server_state_msgs;//等待发送的用户状态
    private Queue<Message> server_broad_msgs;//等待发送消息
    private Dictionary<int, IPEndPoint> allPlayer;
    private Dictionary<IPEndPoint, GameObject> allPlayer_Object;
    private uint networkId;


    private void Start()
    {
        Instance = this;
        Application.runInBackground = true;
        Time.fixedDeltaTime = netConfiguration.serverFixedDeltaTime;
        this.networkId = 0;
        this.server_snapshot_rate = netConfiguration.serverRate;
        this.server_tick_number = 0;
        this.server_tick_accumulator = 0;
        this.server_input_msgs = new Queue<InputMessage>();
        this.server_state_msgs = new Queue<StateMessage>();
        this.server_broad_msgs = new Queue<Message>();
        this.allPlayer = new Dictionary<int, IPEndPoint>();
        this.allPlayer_Object = new Dictionary<IPEndPoint, GameObject>();

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
        ServerUpdate(Time.fixedDeltaTime);

        BroadMessage();
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
                SendMessage(PONG, remoteEndpoint);
                break;
            case MessageType.Client_Request_Connect:
                Client_Request_Connect request_Connect = (Client_Request_Connect)message.Content;
                // 分配用户id
                uint NetId = request_Connect.networkId == 0 ? networkId++ : request_Connect.networkId;
                // 服务器立即回应
                Server_Responses_Connect Responses_Connect = new Server_Responses_Connect()
                {
                    networkId = NetId
                };
                SendMessage(new Message() { Type = MessageType.Server_Responses_Connect, Content = Responses_Connect }, remoteEndpoint);
                // 添加消息队列 广播所有玩家
                Broad_Connect broad_Connect = new Broad_Connect
                {
                    networkId = NetId,
                    isReConnect = request_Connect.networkId != 0
                };
                server_broad_msgs.Enqueue(new Message() { Type = MessageType.Broad_Connect, Content = broad_Connect });
                break;
            case MessageType.Client_Request_Operation:
                break;
            default:
                break;
        }

    }



    private void ServerUpdate(float fixedDeltaTime)
    {
        uint server_tick_number = this.server_tick_number;
        uint server_tick_accumulator = this.server_tick_accumulator;
        Rigidbody server_rigidbody = this.server_player.GetComponent<Rigidbody>();

        while (this.server_input_msgs.Count > 0 && Time.time >= this.server_input_msgs.Peek().delivery_time)
        {
            InputMessage input_msg = this.server_input_msgs.Dequeue();

            // 消息包含一个输入数组，计算最后一个帧
            uint max_tick = input_msg.start_tick_number + (uint)input_msg.inputs.Count - 1;

            // 如果输入帧 大于服务器 当前帧 客户端有输入
            if (max_tick >= server_tick_number)
            {
                // 数组中可能有一些我们已经有的输入，所以找出从哪里开始
                uint start_i = server_tick_number > input_msg.start_tick_number ? (server_tick_number - input_msg.start_tick_number) : 0;

                // 运行所有相关输入，并推动玩家前进
                for (int i = (int)start_i; i < input_msg.inputs.Count; ++i)
                {
                    this.PrePhysicsStep(server_rigidbody, input_msg.inputs[i]);
                    server_physics_scene.Simulate(fixedDeltaTime);

                    ++server_tick_number;
                    ++server_tick_accumulator;
                    if (server_tick_accumulator >= this.server_snapshot_rate)
                    {
                        server_tick_accumulator = 0;

                        if (Random.value > this.packet_loss_chance)
                        {
                            StateMessage state_msg;
                            state_msg.delivery_time = Time.time + this.latency;
                            state_msg.tick_number = server_tick_number;
                            state_msg.position = server_rigidbody.position;
                            state_msg.rotation = server_rigidbody.rotation;
                            state_msg.velocity = server_rigidbody.velocity;
                            state_msg.angular_velocity = server_rigidbody.angularVelocity;
                            this.server_state_msgs.Enqueue(state_msg);
                        }
                    }
                }
            }
        }

        this.server_tick_number = server_tick_number;
        this.server_tick_accumulator = server_tick_accumulator;
    }

    /// <summary>
    /// 物理刚体 提前 输入一帧操作
    /// </summary>
    /// <param name="rigidbody">刚体</param>
    /// <param name="inputs">输入</param>
    private void PrePhysicsStep(Rigidbody rigidbody, Inputs inputs)
    {
        // ForceMode.Impulse
        // 利用刚体的质量，向刚体添加瞬时力脉冲。
        if (inputs.up)
        {
            rigidbody.AddForce(Vector3.forward * inputs.impulse, ForceMode.Impulse);
        }
        if (inputs.down)
        {
            rigidbody.AddForce(-Vector3.forward * inputs.impulse, ForceMode.Impulse);
        }
        if (inputs.left)
        {
            rigidbody.AddForce(-Vector3.right * inputs.impulse, ForceMode.Impulse);
        }
        if (inputs.right)
        {
            rigidbody.AddForce(Vector3.right * inputs.impulse, ForceMode.Impulse);
        }
        if (inputs.jump)
        {
            rigidbody.AddForce(Vector3.up * inputs.impulse, ForceMode.Impulse);
        }
    }

    //发送消息
    private void SendMessage(Message msg, IPEndPoint remoteEndpoint)
    {
        byte[] response = GameUtility.MessageSerialize(msg);
        ServerListener.Send(response, response.Length, remoteEndpoint);
    }

    // 广播消息到所有玩家
    private void BroadMessage()
    {
        while (this.server_broad_msgs.Count > 0)
        {
            Message msg = this.server_broad_msgs.Dequeue();
            byte[] response = GameUtility.MessageSerialize(msg);
            foreach (var item in allPlayer)
            {
                ServerListener.Send(response, response.Length, item.Value);
            }

        }
    }
}
