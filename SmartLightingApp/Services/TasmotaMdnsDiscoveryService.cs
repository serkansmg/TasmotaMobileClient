using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Makaretu.Dns;
using SmartLightingApp.Models;

namespace SmartLightingApp.Services;

public class TasmotaMdnsDiscoveryService : IDisposable
{
    private const string TasmotaHttpService = "_http._tcp";
    private const string TasmotaMqttService = "_mqtt._tcp";
    
    private MulticastService? _multicastService;
    private ServiceDiscovery? _serviceDiscovery;
    private readonly List<TasmotaDiscoveredDevice> _discoveredDevices;
    private readonly Dictionary<string, TasmotaDiscoveredDevice> _pendingDevices;
    private readonly object _lockObject = new();
    private bool _isRunning;

    public event Action<TasmotaDiscoveredDevice>? DeviceDiscovered;
    public event Action<TasmotaDiscoveredDevice>? DeviceLost;
    public event Action<string>? LogMessage;

    public IReadOnlyList<TasmotaDiscoveredDevice> DiscoveredDevices
    {
        get
        {
            lock (_lockObject)
            {
                return _discoveredDevices.ToList();
            }
        }
    }

    public bool IsRunning => _isRunning;

    public TasmotaMdnsDiscoveryService()
    {
        _discoveredDevices = new List<TasmotaDiscoveredDevice>();
        _pendingDevices = new Dictionary<string, TasmotaDiscoveredDevice>(StringComparer.OrdinalIgnoreCase);

    }

    public async Task TestMulticastSocket()
    {
        try
        {
            Log("Testing multicast socket...");
            
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            
            var localEndPoint = new IPEndPoint(IPAddress.Any, 5353);
            socket.Bind(localEndPoint);
            
            var multicastAddress = IPAddress.Parse("224.0.0.251");
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, 
                new MulticastOption(multicastAddress));
            
            Log("✅ Multicast socket test successful");
        }
        catch (Exception ex)
        {
            Log($"❌ Multicast socket test failed: {ex.Message}");
        }
    }

    public async Task StartDiscoveryAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
        {
            Log("Discovery already running, restarting...");
            StopDiscovery();
            await Task.Delay(500);
        }

        try
        {
            Log("🔍 Starting Tasmota mDNS discovery...");

            lock (_lockObject)
            {
                _discoveredDevices.Clear();
                _pendingDevices.Clear();
            }

            _multicastService = new MulticastService();
            _serviceDiscovery = new ServiceDiscovery(_multicastService);
        
            _serviceDiscovery.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
            _serviceDiscovery.ServiceInstanceShutdown += OnServiceInstanceShutdown;
            _multicastService.AnswerReceived += OnAnswerReceived;

            _multicastService.Start();
            await Task.Delay(100);

            _serviceDiscovery.QueryServiceInstances(TasmotaHttpService);
            _serviceDiscovery.QueryServiceInstances(TasmotaMqttService);
        
            _isRunning = true;
            Log("✅ Tasmota mDNS discovery active");
        }
        catch (Exception ex)
        {
            Log($"❌ Discovery start failed: {ex.Message}");
            throw;
        }
    }

    public void StopDiscovery()
    {
        if (!_isRunning) return;

        try
        {
            Log("⏹️ Stopping discovery...");
            _isRunning = false;

            if (_serviceDiscovery != null)
            {
                _serviceDiscovery.ServiceInstanceDiscovered -= OnServiceInstanceDiscovered;
                _serviceDiscovery.ServiceInstanceShutdown -= OnServiceInstanceShutdown;
            }

            if (_multicastService != null)
            {
                _multicastService.AnswerReceived -= OnAnswerReceived;
            }

            _serviceDiscovery?.Dispose();
            _multicastService?.Stop();

            _serviceDiscovery = null;
            _multicastService = null;

            lock (_lockObject)
            {
                _pendingDevices.Clear();
            }

            Log("🔴 Discovery stopped");
        }
        catch (Exception ex)
        {
            Log($"❌ Stop discovery error: {ex.Message}");
        }
    }

    public void ClearDiscoveredDevices()
    {
        lock (_lockObject)
        {
            _discoveredDevices.Clear();
        }
        Log("🧹 Cleared discovered devices");
    }

    public void LogNetworkInterfaces()
    {
        try
        {
            Log("📡 Network interfaces:");
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            
            foreach (var iface in interfaces)
            {
                if (iface.OperationalStatus == OperationalStatus.Up)
                {
                    var props = iface.GetIPProperties();
                    foreach (var addr in props.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            Log($"   {iface.Name}: {addr.Address} (Multicast: {iface.SupportsMulticast})");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"❌ Network interface error: {ex.Message}");
        }
    }

    public async Task SendManualQueries()
    {
        try
        {
            Log("🔧 Sending manual queries...");
            
            if (_multicastService != null)
            {
                _multicastService.SendQuery("tasmota-B774E0-5344.local", type: DnsType.A);
                await Task.Delay(200);
                
                _multicastService.SendQuery("_http._tcp.local", type: DnsType.PTR);
                await Task.Delay(200);
                
                _multicastService.SendQuery("_services._dns-sd._udp.local", type: DnsType.PTR);
            }
        }
        catch (Exception ex)
        {
            Log($"❌ Manual query error: {ex.Message}");
        }
    }

    private void OnServiceInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs e)
    {
        try
        {
            var serviceName = e.ServiceInstanceName.ToString();

            bool isTasmotaService = serviceName.Contains("_http._tcp", StringComparison.OrdinalIgnoreCase) ||
                                   serviceName.Contains("_mqtt._tcp", StringComparison.OrdinalIgnoreCase);
            
            if (!isTasmotaService) return;

            var instanceName = serviceName.Split('.')[0];
            instanceName = instanceName.ToLowerInvariant();
            
            bool isTasmotaDevice = instanceName.StartsWith("tasmota", StringComparison.OrdinalIgnoreCase) ||
                                  instanceName.Contains("esp", StringComparison.OrdinalIgnoreCase) ||
                                  instanceName.Contains("relay", StringComparison.OrdinalIgnoreCase);

            if (!isTasmotaDevice) return;

            Log($"🎯 Found Tasmota device: {instanceName}");

            var device = new TasmotaDiscoveredDevice
            {
                Hostname = serviceName.ToLowerInvariant(),
                DeviceId = instanceName,
                LastSeen = DateTime.Now
            };

            lock (_lockObject)
            {
                _pendingDevices[serviceName] = device;
            }

            _multicastService?.SendQuery(e.ServiceInstanceName, type: DnsType.SRV);
            _multicastService?.SendQuery(e.ServiceInstanceName, type: DnsType.TXT);
            
            var hostname = instanceName + ".local";
            _multicastService?.SendQuery(hostname, type: DnsType.A);
        }
        catch (Exception ex)
        {
            Log($"❌ Service discovery error: {ex.Message}");
        }
    }

    private void OnServiceInstanceShutdown(object? sender, ServiceInstanceShutdownEventArgs e)
    {
        try
        {
            var serviceName = e.ServiceInstanceName.ToString();
            
            lock (_lockObject)
            {
                _pendingDevices.Remove(serviceName);
            }

            var deviceToRemove = _discoveredDevices
                .FirstOrDefault(d => string.Equals(d.Hostname, serviceName, StringComparison.OrdinalIgnoreCase));
            if (deviceToRemove != null)
            {
                RemoveDevice(deviceToRemove.DeviceId);
            }
        }
        catch (Exception ex)
        {
            Log($"❌ Service shutdown error: {ex.Message}");
        }
    }

    private void OnAnswerReceived(object? sender, MessageEventArgs e)
    {
        try
        {
            // Process SRV records
            var srvRecords = e.Message.Answers.OfType<SRVRecord>();
            foreach (var srv in srvRecords)
            {
                var serviceName = srv.Name.ToString();
                
                bool isTasmotaSrv = serviceName.Contains("_http._tcp", StringComparison.OrdinalIgnoreCase) ||
                                   serviceName.Contains("_mqtt._tcp", StringComparison.OrdinalIgnoreCase);
                
                if (!isTasmotaSrv) continue;

                lock (_lockObject)
                {
                    if (_pendingDevices.TryGetValue(serviceName, out var device))
                    {
                        device.Port = srv.Port;
                        
                        if (srv.Target.ToString() != device.DeviceId + ".local")
                        {
                            _multicastService?.SendQuery(srv.Target, type: DnsType.A);
                        }
                    }
                }
            }

            // Process TXT records
            var txtRecords = e.Message.Answers.OfType<TXTRecord>();
            foreach (var txt in txtRecords)
            {
                var serviceName = txt.Name.ToString();
                
                bool isTasmotaTxt = serviceName.Contains("_http._tcp", StringComparison.OrdinalIgnoreCase) ||
                                   serviceName.Contains("_mqtt._tcp", StringComparison.OrdinalIgnoreCase);
                
                if (!isTasmotaTxt) continue;

                lock (_lockObject)
                {
                    if (_pendingDevices.TryGetValue(serviceName, out var device))
                    {
                        foreach (var txtString in txt.Strings)
                        {
                            var parts = txtString.Split('=', 2);
                            if (parts.Length == 2)
                            {
                                var key = parts[0].Trim();
                                var value = parts[1].Trim();

                                if (key.Equals("version", StringComparison.OrdinalIgnoreCase) ||
                                    key.Equals("fw_ver", StringComparison.OrdinalIgnoreCase))
                                    device.Version = value;
                            }
                        }
                    }
                }
            }

            // Process A records
            var aRecords = e.Message.Answers.OfType<ARecord>();
            foreach (var aRecord in aRecords)
            {
                var hostName = aRecord.Name.ToString().ToLowerInvariant();

                lock (_lockObject)
                {
                    // Check pending devices
                    var deviceToUpdate = _pendingDevices.Values.FirstOrDefault(d => 
                    {
                        var instanceName = d.Hostname.Split('.')[0].ToLowerInvariant();
                        var expectedHostname = instanceName + ".local";
                        return expectedHostname == hostName;
                    });

                    if (deviceToUpdate != null)
                    {
                        deviceToUpdate.IpAddress = aRecord.Address;
                        
                        if (deviceToUpdate.IsBasicallyValid())
                        {
                            Log($"✅ Complete: {deviceToUpdate.DeviceId} -> {aRecord.Address}");
                            AddOrUpdateDevice(deviceToUpdate);
                            _pendingDevices.Remove(deviceToUpdate.Hostname);
                        }
                    }
                    else
                    {
                        // Direct Tasmota hostname match
                        if (hostName.StartsWith("tasmota", StringComparison.OrdinalIgnoreCase))
                        {
                            var deviceId = hostName.Replace(".local", "");
                            var device = new TasmotaDiscoveredDevice
                            {
                                DeviceId = deviceId,
                                Hostname = hostName,
                                IpAddress = aRecord.Address,
                                Port = 80,
                                LastSeen = DateTime.Now
                            };
                            
                            AddOrUpdateDevice(device);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"❌ Answer processing error: {ex.Message}");
        }
    }

    private void AddOrUpdateDevice(TasmotaDiscoveredDevice device)
    {
        lock (_lockObject)
        {
            var existing = _discoveredDevices
                .FirstOrDefault(d => string.Equals(d.DeviceId, device.DeviceId, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.IpAddress = device.IpAddress;
                existing.Version = device.Version;
                existing.Hostname = device.Hostname;
                existing.Port = device.Port;
                existing.LastSeen = device.LastSeen;
            }
            else
            {
                _discoveredDevices.Add(device);
                DeviceDiscovered?.Invoke(device);
                Log($"✅ New device: {device.DeviceId} -> {device.IpAddress}:{device.Port}");
            }
        }
    }

    private void RemoveDevice(string deviceId)
    {
        lock (_lockObject)
        {
            var device = _discoveredDevices
                .FirstOrDefault(d => string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
            if (device != null)
            {
                _discoveredDevices.Remove(device);
                DeviceLost?.Invoke(device);
                Log($"❌ Device lost: {deviceId}");
            }
        }
    }

    public TasmotaDiscoveredDevice? GetDeviceById(string deviceId)
    {
        lock (_lockObject)
        {
            return _discoveredDevices
                .FirstOrDefault(d => string.Equals(d.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
        }
    }

    public TasmotaDiscoveredDevice? GetDeviceByIp(string ipAddress)
    {
        lock (_lockObject)
        {
            return _discoveredDevices.FirstOrDefault(d => d.IpAddress.ToString() == ipAddress);
        }
    }

    private void Log(string message)
    {
        var logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogMessage?.Invoke(logMessage);
        System.Diagnostics.Debug.WriteLine(logMessage);
    }

    public void Dispose()
    {
        StopDiscovery();
    }
}

public static class TasmotaDiscoveredDeviceExtensions
{
    public static bool IsBasicallyValid(this TasmotaDiscoveredDevice device)
    {
        return !string.IsNullOrEmpty(device.DeviceId) && 
               device.IpAddress != null && 
               !device.IpAddress.Equals(IPAddress.None);
    }
}