using System.Net;

namespace SmartLightingApp.Models;

public class TasmotaDiscoveredDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public IPAddress IpAddress { get; set; } = IPAddress.None;
    public string Hostname { get; set; } = string.Empty;
    public DateTime LastSeen { get; set; }
    public int Port { get; set; } = 8888;

    public override string ToString()
    {
        return $"PlantController[{DeviceId}] v{Version} at {IpAddress}:{Port} ({Hostname})";
    }

    public bool IsValid()
    {
        return !string.IsNullOrEmpty(DeviceId) && 
               !IpAddress.Equals(IPAddress.None) && 
               Port > 0;
    }

    public TimeSpan TimeSinceLastSeen => DateTime.Now - LastSeen;
}