using System.ComponentModel.DataAnnotations;

namespace sins.Models;

public class DomainUpstreamMapping
{
    public int Id { get; set; }

    [Required]
    [MaxLength(253)]
    public string Domain { get; set; } = string.Empty;

    [Required]
    [MaxLength(45)]
    public string UpstreamServer { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
