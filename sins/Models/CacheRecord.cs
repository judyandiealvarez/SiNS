using System.ComponentModel.DataAnnotations;

namespace sins.Models;

public class CacheRecord
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(10)]
    public string Type { get; set; } = string.Empty;
    
    [Required]
    public string Response { get; set; } = string.Empty;
    
    public DateTime CachedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ExpiresAt { get; set; }
    
    public string? UpstreamServer { get; set; }
}
