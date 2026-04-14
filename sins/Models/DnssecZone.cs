using System.ComponentModel.DataAnnotations;

namespace sins.Models;

/// <summary>
/// Authoritative DNSSEC zone configuration (ECDSAP256SHA256). Private keys are PEM (PKCS#8);
/// protect the database or prefer external key storage for production.
/// </summary>
public class DnssecZone
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Apex { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    /// <summary>IANA algorithm number (13 = ECDSAP256SHA256).</summary>
    public int Algorithm { get; set; } = 13;

    [Required]
    public string KskPrivateKeyPem { get; set; } = string.Empty;

    [Required]
    public string ZskPrivateKeyPem { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
