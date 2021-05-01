using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New NetConfiguration", menuName = "Net Configuration")]
public class NetConfiguration : ScriptableObject
{
    public string HostAddress;
    public UInt16 listenerPort;
    public string lanDiscoveryRequest;
    public string lanDiscoveryResponse;
}
