using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerLogic : MonoBehaviour
{
    public CommonData commonData;
    public GameObject server_player;
    public GameObject server_display_player;

    
    private uint server_tick_number;
    private uint server_tick_accumulator;

    private Scene server_scene;
    private PhysicsScene server_physics_scene;

    private void Start()
    {
        this.server_tick_number = 0;
        this.server_tick_accumulator = 0;
        server_scene = SceneManager.LoadScene("server_physics_scene", new LoadSceneParameters() { loadSceneMode = LoadSceneMode.Additive, localPhysicsMode = LocalPhysicsMode.Physics3D });
        server_physics_scene = server_scene.GetPhysicsScene();
        SceneManager.MoveGameObjectToScene(server_player, server_scene);
    }

    /// <summary>
    /// 服务器更新
    /// </summary>
    private void Update()
    {
        float fixedDeltaTime = Time.fixedDeltaTime;
        Rigidbody server_rigidbody = this.server_player.GetComponent<Rigidbody>();

        while (commonData.client_input_msgs.Count > 0 && Time.time >= commonData.client_input_msgs.Peek().delivery_time)
        {
            InputMessage input_msg = commonData.client_input_msgs.Dequeue();

            // 消息包含一个输入数组，计算最后一个帧
            uint max_tick = input_msg.start_tick_number + (uint)input_msg.inputs.Count - 1;

            // 如果输入帧 大于服务器 当前帧 客户端有输入
            if (max_tick >= server_tick_number)
            {
                // 数组中可能有一些我们已经有的输入，所以找出从哪里开始
                uint start_i = server_tick_number > input_msg.start_tick_number ? (server_tick_number - input_msg.start_tick_number) : 0;
                //Debug.Log(start_i);
                // 运行所有相关输入，并推动玩家前进
                for (int i = (int)start_i; i < input_msg.inputs.Count; ++i)
                {
                    this.PrePhysicsStep(server_rigidbody, input_msg.inputs[i]);
                    server_physics_scene.Simulate(fixedDeltaTime);

                    ++server_tick_number;
                    ++server_tick_accumulator;
                    if (server_tick_accumulator >= commonData.server_snapshot_rate)
                    {
                        server_tick_accumulator = 0;
                        StateMessage state_msg = new StateMessage
                        {
                            delivery_time = Time.time + commonData.latency,
                            tick_number = server_tick_number,
                            position = server_rigidbody.position,
                            rotation = server_rigidbody.rotation,
                            velocity = server_rigidbody.velocity,
                            angular_velocity = server_rigidbody.angularVelocity
                        };
                        commonData.SendInputStatePacketToClient(state_msg);
                    }
                }

                this.server_display_player.transform.position = server_rigidbody.position;
                this.server_display_player.transform.rotation = server_rigidbody.rotation;
            }
        }
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
