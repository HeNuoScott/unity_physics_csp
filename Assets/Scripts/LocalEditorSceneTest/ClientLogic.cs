using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientLogic : MonoBehaviour
{
    public CommonData commonData;
    public GameObject client_player;
    public GameObject smoothed_client_player;
    public GameObject proxy_player;
    // client specific
    public bool client_enable_corrections = true;
    public bool client_correction_smoothing = true;
    public bool client_send_redundant_inputs = true;//是否使用 最新的状态帧 做为输入信息的开始帧
    private float client_timer;
    private uint client_tick_number;
    private uint client_last_received_state_tick;
    private const int c_client_buffer_size = 1024;
    private ClientState[] client_state_buffer; // client stores predicted moves here
    private Inputs[] client_input_buffer; // client stores predicted inputs here
    private Vector3 client_pos_error;
    private Quaternion client_rot_error;

    private Scene client_scene;
    private PhysicsScene client_physics_scene;

    void Start()
    {
        this.client_timer = 0.0f;
        this.client_tick_number = 0;
        this.client_last_received_state_tick = 0;
        this.client_state_buffer = new ClientState[c_client_buffer_size];
        this.client_input_buffer = new Inputs[c_client_buffer_size];
        this.client_pos_error = Vector3.zero;
        this.client_rot_error = Quaternion.identity;

        client_scene = SceneManager.LoadScene("client_physics_scene", new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
        client_physics_scene = client_scene.GetPhysicsScene();
        SceneManager.MoveGameObjectToScene(client_player, client_scene);
    }

    /// <summary>
    /// 客户端更新
    /// </summary>
    private void Update()
    {
        float fixedDeltaTime = Time.fixedDeltaTime;
        Rigidbody client_rigidbody = this.client_player.GetComponent<Rigidbody>();
        client_timer += Time.deltaTime;
        while (client_timer >= fixedDeltaTime)
        {
            client_timer -= fixedDeltaTime;

            uint buffer_slot = client_tick_number % c_client_buffer_size;

            // 收集这一帧的输入
            Inputs inputs = new Inputs
            {
                up = Input.GetKey(KeyCode.W),
                down = Input.GetKey(KeyCode.S),
                left = Input.GetKey(KeyCode.A),
                right = Input.GetKey(KeyCode.D),
                jump = Input.GetKey(KeyCode.Space)
            };
            this.client_input_buffer[buffer_slot] = inputs;

            // 存储此帧的状态，然后使用 当前状态 + 输入 进行步进模拟
            this.ClientStoreCurrentStateAndStep(ref this.client_state_buffer[buffer_slot], client_rigidbody, inputs, fixedDeltaTime);

            InputMessage input_msg = new InputMessage
            {
                delivery_time = Time.time + commonData.latency,
                start_tick_number = this.client_send_redundant_inputs ? this.client_last_received_state_tick : client_tick_number,
                inputs = new List<Inputs>()
            };
            for (uint tick = input_msg.start_tick_number; tick <= client_tick_number; ++tick)
            {
                input_msg.inputs.Add(this.client_input_buffer[tick % c_client_buffer_size]);
            }
            commonData.SendInputPacketToServer(input_msg);
            ++client_tick_number;
        }

        DealWithCheckServerState(fixedDeltaTime, client_rigidbody, client_tick_number);

        if (this.client_correction_smoothing)
        {
            this.client_pos_error *= 0.9f;
            this.client_rot_error = Quaternion.Slerp(this.client_rot_error, Quaternion.identity, 0.1f);
        }
        else
        {
            this.client_pos_error = Vector3.zero;
            this.client_rot_error = Quaternion.identity;
        }

        this.smoothed_client_player.transform.position = client_rigidbody.position + this.client_pos_error;
        this.smoothed_client_player.transform.rotation = client_rigidbody.rotation * this.client_rot_error;
    }

    /// <summary>
    /// 处理核验 服务器发挥的状态
    /// </summary>
    /// <param name="fixedDeltaTime"></param>
    /// <param name="client_rigidbody"></param>
    /// <param name="client_tick_number"></param>
    private void DealWithCheckServerState(float fixedDeltaTime, Rigidbody client_rigidbody, uint client_tick_number)
    {
        if (this.ClientHasStateMessage())
        {
            StateMessage state_msg = commonData.server_input_state_msgs.Dequeue();
            // 确保当前是最新的 可用状态消息
            while (this.ClientHasStateMessage()) state_msg = commonData.server_input_state_msgs.Dequeue();

            this.client_last_received_state_tick = state_msg.tick_number;

            this.proxy_player.transform.position = state_msg.position;
            this.proxy_player.transform.rotation = state_msg.rotation;

            if (this.client_enable_corrections)
            {
                uint buffer_slot = state_msg.tick_number % c_client_buffer_size;
                Vector3 position_error = state_msg.position - this.client_state_buffer[buffer_slot].position;
                float rotation_error = 1.0f - Quaternion.Dot(state_msg.rotation, this.client_state_buffer[buffer_slot].rotation);

                if (position_error.sqrMagnitude > 0.0000001f || rotation_error > 0.00001f)
                {
                    Debug.Log("当前错误帧 " + state_msg.tick_number + " (倒回 " + (client_tick_number - state_msg.tick_number) + " 帧)");
                    // capture the current predicted pos for smoothing
                    Vector3 prev_pos = client_rigidbody.position + this.client_pos_error;
                    Quaternion prev_rot = client_rigidbody.rotation * this.client_rot_error;

                    // rewind & replay
                    client_rigidbody.position = state_msg.position;
                    client_rigidbody.rotation = state_msg.rotation;
                    client_rigidbody.velocity = state_msg.velocity;
                    client_rigidbody.angularVelocity = state_msg.angular_velocity;

                    uint rewind_tick_number = state_msg.tick_number;
                    while (rewind_tick_number < client_tick_number)
                    {
                        buffer_slot = rewind_tick_number % c_client_buffer_size;
                        this.ClientStoreCurrentStateAndStep(ref this.client_state_buffer[buffer_slot], client_rigidbody, this.client_input_buffer[buffer_slot], fixedDeltaTime);

                        ++rewind_tick_number;
                    }

                    // if more than 2ms apart, just snap
                    if ((prev_pos - client_rigidbody.position).sqrMagnitude >= 4.0f)
                    {
                        this.client_pos_error = Vector3.zero;
                        this.client_rot_error = Quaternion.identity;
                    }
                    else
                    {
                        this.client_pos_error = prev_pos - client_rigidbody.position;
                        this.client_rot_error = Quaternion.Inverse(client_rigidbody.rotation) * prev_rot;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 客户端 储存当前状态 并模拟下一帧
    /// </summary>
    /// <param name="current_state">当前状态</param>
    /// <param name="rigidbody">刚体</param>
    /// <param name="inputs">输入</param>
    /// <param name="dt">时间 间隔</param>
    private void ClientStoreCurrentStateAndStep(ref ClientState current_state, Rigidbody rigidbody, Inputs inputs, float dt)
    {
        current_state.position = rigidbody.position;
        current_state.rotation = rigidbody.rotation;

        this.PrePhysicsStep(rigidbody, inputs);

        client_physics_scene.Simulate(dt);
    }

    /// <summary>
    /// 查找 可用 状态消息
    /// </summary>
    private bool ClientHasStateMessage()
    {
        return commonData.server_input_state_msgs.Count > 0 && Time.time >= commonData.server_input_state_msgs.Peek().delivery_time;
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
        if (commonData.local_player_camera_transform != null)
        {
            if (inputs.up)
            {
                rigidbody.AddForce(commonData.local_player_camera_transform.forward * commonData.player_movement_impulse, ForceMode.Impulse);
            }
            if (inputs.down)
            {
                rigidbody.AddForce(-commonData.local_player_camera_transform.forward * commonData.player_movement_impulse, ForceMode.Impulse);
            }
            if (inputs.left)
            {
                rigidbody.AddForce(-commonData.local_player_camera_transform.right * commonData.player_movement_impulse, ForceMode.Impulse);
            }
            if (inputs.right)
            {
                rigidbody.AddForce(commonData.local_player_camera_transform.right * commonData.player_movement_impulse, ForceMode.Impulse);
            }
            if (rigidbody.transform.position.y <= commonData.player_jump_y_threshold && inputs.jump)
            {
                rigidbody.AddForce(commonData.local_player_camera_transform.up * commonData.player_movement_impulse, ForceMode.Impulse);
            }
        }
    }
}
