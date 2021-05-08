using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System.Text;
using System.Net;
using System;
using Data;

public class GameUtility 
{
    public static string GetLocalIP()
    {
        try
        {
            string HostName = Dns.GetHostName(); //得到主机名
            IPHostEntry IpEntry = Dns.GetHostEntry(HostName);
            for (int i = 0; i < IpEntry.AddressList.Length; i++)
            {
                //从IP地址列表中筛选出IPv4类型的IP地址
                //AddressFamily.InterNetwork表示此IP为IPv4,
                //AddressFamily.InterNetworkV6表示此地址为IPv6类型
                if (IpEntry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                {
                    return IpEntry.AddressList[i].ToString();
                }
            }
            return "";
        }
        catch (Exception ex)
        {
            Debug.LogError("获取本机IP出错:" + ex.Message);
            return "";
        }
    }

    public static byte[] MessageSerialize(Message message)
    {
        string json = message.ToJson();
        return Encoding.UTF8.GetBytes(json);
    }

    public static Message MessageDeserialization(byte[] message)
    {
        string json = Encoding.UTF8.GetString(message);
        return MessageDeserializer.FromJson(json);
    }
}


public struct Inputs
{
    public bool up;
    public bool down;
    public bool left;
    public bool right;
    public bool jump;

    public uint impulse;
}

public struct InputMessage
{
    public float delivery_time;
    public uint start_tick_number;
    public List<Inputs> inputs;
}

public struct ClientState
{
    public Vector3 position;
    public Quaternion rotation;
}

public struct StateMessage
{
    /// <summary>
    /// 发送时间
    /// </summary>
    public float delivery_time;
    public uint tick_number;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angular_velocity;
}
