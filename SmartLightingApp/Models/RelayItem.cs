using System.ComponentModel.DataAnnotations;

namespace SmartLightingApp.Models;

public class RelayItem
{
    public int Id { get; set; }
        
    public int BoardId { get; set; }
        
    public int RelayNumber { get; set; }
        
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
        
    public bool IsOn { get; set; }
        
    public bool HasTimer { get; set; }
        
    public DateTime LastToggled { get; set; }
        
    // Navigation property
    public RelayBoard? Board { get; set; }
        
    public RelayItem()
    {
        LastToggled = DateTime.Now;
    }
        
    public RelayItem(int relayNumber, string name = "") : this()
    {
        RelayNumber = relayNumber;
        Name = string.IsNullOrEmpty(name) ? $"Relay {relayNumber}" : name;
    }
}