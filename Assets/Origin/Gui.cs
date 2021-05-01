﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Gui : MonoBehaviour
{
    public Logic logic;
    public GameObject gui;
    public GameObject server_display_player;
    public GameObject proxy_player;

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
        this.corrections_toggle.isOn = true;
        this.correction_smoothing_toggle.isOn = true;
        this.redundant_inputs_toggle.isOn = true;
        this.server_player_toggle.isOn = false;
        this.proxy_player_toggle.isOn = false;
        this.packet_loss_slider.value = this.logic.packet_loss_chance;
        this.latency_slider.value = this.logic.latency;
        this.snapshot_rate_slider.value = Mathf.Log(this.logic.server_snapshot_rate, 2.0f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            this.gui.SetActive(!this.gui.activeSelf);
        }
    }
    // 显示服务器玩家
    public void OnToggleServerPlayer(bool enabled)
    {
        this.server_display_player.SetActive(enabled);
    }
    // 显示代理玩家
    public void OnToggleProxyPlayer(bool enabled)
    {
        this.proxy_player.SetActive(enabled);
    }

    public void OnToggleCorrections(bool enabled)
    {
        this.logic.client_enable_corrections = enabled;
        this.correction_smoothing_toggle.interactable = enabled;
    }

    public void OnToggleCorrectionSmoothing(bool enabled)
    {
        this.logic.client_correction_smoothing = enabled;
    }

    public void OnToggleSendRedundantInputs(bool enable)
    {
        this.logic.client_send_redundant_inputs = enable;
    }

    public void OnPacketLossSliderChanged(float value)
    {
        this.packet_loss_label.text = string.Format("丢包率 - {0:F1}%", value * 100.0f);
        this.logic.packet_loss_chance = value;
    }

    public void OnLatencySliderChanged(float value)
    {
        this.latency_label.text = string.Format("延迟 - {0}ms", (int)(value * 1000.0f));
        this.logic.latency = value;
    }

    public void OnSnapshotRateSliderChanged(float value)
    {
        uint rate = (uint)Mathf.Pow(2, value);
        this.snapshot_rate_label.text = string.Format("帧率 - {0}hz", 64 / rate);
        this.logic.server_snapshot_rate = rate;
    }
}
