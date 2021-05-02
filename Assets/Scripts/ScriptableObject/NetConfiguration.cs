using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New NetConfiguration", menuName = "Net Configuration")]
public class NetConfiguration : ScriptableObject
{
    public string HostAddress;
    public UInt16 listenerPort;
    public uint serverRate;
    public float serverFixedDeltaTime;
    public string lanDiscoveryRequest;
    public string lanDiscoveryResponse;
}
