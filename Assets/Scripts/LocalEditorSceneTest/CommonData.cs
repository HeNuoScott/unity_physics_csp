using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommonData : MonoBehaviour
{
    public ServerLogic serverLogic;
    public ClientLogic clientLogic;

    public Transform local_player_camera_transform;
    public float player_movement_impulse = 0.05f;// 玩家刚体移动脉冲力
    public float player_jump_y_threshold = -3f;// 玩家跳跃高度限制
    public float latency = 0.05f;
    public float packet_loss_chance = 0.05f;
    public uint server_snapshot_rate;//服务器多少帧同步一次数据

    //Server=>Client
    public Queue<InputMessage> server_input_msgs = new Queue<InputMessage>(); //接收到服务器的消息
    public Queue<StateMessage> server_input_state_msgs = new Queue<StateMessage>(); // 服务器计算  客户端执行变动之后的状态

    //Client=>Server
    public Queue<InputMessage> client_input_msgs = new Queue<InputMessage>();//接收到客户端的消息

    public void SendInputPacketToServer(InputMessage inputMessage)
    {
        if (Random.value < packet_loss_chance) return;
        client_input_msgs.Enqueue(inputMessage);
        //if (inputMessage.inputs.Count>1)
        //{
        //    Debug.Log($"输入了:{inputMessage.inputs.Count}次");
        //}
    }

    public void SendInputPacketToClient(InputMessage inputMessage)
    {

    }

    public void SendInputStatePacketToClient(StateMessage stateMessage)
    {
        if (Random.value < packet_loss_chance) return;

        server_input_state_msgs.Enqueue(stateMessage);
    }

    public Toggle corrections_toggle;//开启 修正
    public Toggle correction_smoothing_toggle;//平滑 修正
    public Toggle redundant_inputs_toggle;//发送 冗余
    public Toggle server_player_toggle;// 显示服务器玩家
    public Toggle proxy_player_toggle;// 显示 代理玩家
    public Slider packet_loss_slider;
    public Text packet_loss_label;
    public Slider latency_slider;
    public Text latency_label;
    public Slider snapshot_rate_slider;
    public Text snapshot_rate_label;

    public void Start()
    {
        Time.fixedDeltaTime = 0.015625f;
        Physics.autoSimulation = false;
        //Physics.autoSyncTransforms = false;
        corrections_toggle.isOn = true;
        correction_smoothing_toggle.isOn = true;
        redundant_inputs_toggle.isOn = true;
        server_player_toggle.isOn = true;
        proxy_player_toggle.isOn = true;
        packet_loss_slider.value =packet_loss_chance;
        latency_slider.value = latency;
        snapshot_rate_slider.value = Mathf.Log(server_snapshot_rate, 2.0f);

        corrections_toggle.onValueChanged.AddListener(OnToggleCorrections);
        correction_smoothing_toggle.onValueChanged.AddListener(OnToggleCorrectionSmoothing);
        redundant_inputs_toggle.onValueChanged.AddListener(OnToggleSendRedundantInputs);
        server_player_toggle.onValueChanged.AddListener(OnToggleServerPlayer);
        proxy_player_toggle.onValueChanged.AddListener(OnToggleProxyPlayer);
        packet_loss_slider.onValueChanged.AddListener(OnPacketLossSliderChanged);
        latency_slider.onValueChanged.AddListener(OnLatencySliderChanged);
        snapshot_rate_slider.onValueChanged.AddListener(OnSnapshotRateSliderChanged);
    }

    // 显示服务器玩家
    public void OnToggleServerPlayer(bool enabled)
    {
        serverLogic.server_display_player.SetActive(enabled);
    }
    // 显示代理玩家
    public void OnToggleProxyPlayer(bool enabled)
    {
        clientLogic.proxy_player.SetActive(enabled);
    }

    public void OnToggleCorrections(bool enabled)
    {
        clientLogic.client_enable_corrections = enabled;
        this.correction_smoothing_toggle.interactable = enabled;
    }

    public void OnToggleCorrectionSmoothing(bool enabled)
    {
        clientLogic.client_correction_smoothing = enabled;
    }

    public void OnToggleSendRedundantInputs(bool enable)
    {
        clientLogic.client_send_redundant_inputs = enable;
    }

    public void OnPacketLossSliderChanged(float value)
    {
        this.packet_loss_label.text = string.Format("丢包率 - {0:F1}%", value * 100.0f);
        packet_loss_chance = value;
    }

    public void OnLatencySliderChanged(float value)
    {
        this.latency_label.text = string.Format("延迟 - {0}ms", (int)(value * 1000.0f));
        latency = value;
    }

    public void OnSnapshotRateSliderChanged(float value)
    {
        uint rate = (uint)Mathf.Pow(2, value);
        snapshot_rate_label.text = string.Format("帧率 - {0}hz", 64 / rate);
        server_snapshot_rate = rate;
    }
}
