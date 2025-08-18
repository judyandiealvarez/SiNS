using System.ComponentModel.DataAnnotations;

namespace sins.Models;

public class DnsRecord
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Value { get; set; } = string.Empty;
    
    public int Ttl { get; set; } = 3600;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
}
