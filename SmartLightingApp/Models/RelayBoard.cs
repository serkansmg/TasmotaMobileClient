using System.ComponentModel.DataAnnotations;

namespace SmartLightingApp.Models;
public class RelayBoard
{
    public int Id { get; set; }
        
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
        
    [Required]
    public string IpAddress { get; set; } = string.Empty;
    
    // YENİ: Tasmota cihaz bilgileri
    public string? Hostname { get; set; } = string.Empty;
    public string? MacAddress { get; set; } = string.Empty;
        
    public bool IsOnline { get; set; }
        
    public DateTime LastSeen { get; set; }
        
    public List<RelayItem> Relays { get; set; } = new();
        
    // Computed properties
    public int TotalRelays => Relays.Count;
    public int ActiveRelays => Relays.Count(r => r.IsOn);
    public string RelaysSummary => IsOnline ? $"{ActiveRelays}/{TotalRelays} açık" : "Bilinmiyor";      
    public RelayBoard()
    {
        LastSeen = DateTime.Now;
    }
}