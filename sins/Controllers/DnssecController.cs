using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sins.Data;
using sins.Models;
using sins.Services;
using sins.Services.Dnssec;

namespace sins.Controllers;

/// <summary>
/// DNSSEC zone administration. After creating a zone, publish the <c>DS</c> record from
/// <see cref="GetDs"/> at the <b>parent</b> zone (registrar or parent nameserver) so validating resolvers
/// can establish a chain of trust to this server.
/// </summary>
[ApiController]
[Route("api/dnssec")]
[Authorize]
public class DnssecController : ControllerBase
{
    private readonly DnsContext _context;
    private readonly IDnssecCatalog _dnssecCatalog;
    private readonly ILogger<DnssecController> _logger;

    public DnssecController(DnsContext context, IDnssecCatalog dnssecCatalog, ILogger<DnssecController> logger)
    {
        _context = context;
        _dnssecCatalog = dnssecCatalog;
        _logger = logger;
    }

    public sealed class DnssecZoneRowDto
    {
        public int Id { get; set; }
        public string Apex { get; set; } = string.Empty;
        public bool Enabled { get; set; }
        public int Algorithm { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public sealed class CreateDnssecZoneRequest
    {
        public string Apex { get; set; } = string.Empty;
        public bool GenerateKeys { get; set; } = true;
    }

    public sealed class UpdateDnssecZoneRequest
    {
        public bool Enabled { get; set; }
    }

    [HttpGet("zones")]
    public async Task<IActionResult> ListZones()
    {
        var rows = await _context.DnssecZones.AsNoTracking()
            .OrderBy(z => z.Apex)
            .Select(z => new DnssecZoneRowDto
            {
                Id = z.Id,
                Apex = z.Apex,
                Enabled = z.Enabled,
                Algorithm = z.Algorithm,
                CreatedAt = z.CreatedAt,
                UpdatedAt = z.UpdatedAt
            })
            .ToListAsync();
        return Ok(rows);
    }

    [HttpGet("zones/{id:int}")]
    public async Task<IActionResult> GetZone(int id)
    {
        var z = await _context.DnssecZones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (z == null) return NotFound();
        return Ok(new DnssecZoneRowDto
        {
            Id = z.Id,
            Apex = z.Apex,
            Enabled = z.Enabled,
            Algorithm = z.Algorithm,
            CreatedAt = z.CreatedAt,
            UpdatedAt = z.UpdatedAt
        });
    }

    [HttpPost("zones")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateZone([FromBody] CreateDnssecZoneRequest body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.Apex))
            return BadRequest(new { message = "Apex is required." });

        var apex = DnsCanonical.NormalizeOwner(body.Apex);
        if (apex.Length == 0)
            return BadRequest(new { message = "Invalid apex." });

        if (await _context.DnssecZones.AnyAsync(z => z.Apex == apex))
            return Conflict(new { message = "A DNSSEC zone for this apex already exists." });

        if (!body.GenerateKeys)
            return BadRequest(new { message = "Only generateKeys=true is supported in this version." });

        using var ksk = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var zsk = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var kskPem = ksk.ExportPkcs8PrivateKeyPem();
        var zskPem = zsk.ExportPkcs8PrivateKeyPem();

        var zone = new DnssecZone
        {
            Apex = apex,
            Enabled = true,
            Algorithm = DnsTypes.AlgorithmEcdsaP256Sha256,
            KskPrivateKeyPem = kskPem,
            ZskPrivateKeyPem = zskPem,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.DnssecZones.Add(zone);
        await _context.SaveChangesAsync();
        _dnssecCatalog.InvalidateZone(apex);
        _logger.LogInformation("DNSSEC zone created for apex {Apex}", apex);
        return CreatedAtAction(nameof(GetZone), new { id = zone.Id },
            new DnssecZoneRowDto
            {
                Id = zone.Id,
                Apex = zone.Apex,
                Enabled = zone.Enabled,
                Algorithm = zone.Algorithm,
                CreatedAt = zone.CreatedAt,
                UpdatedAt = zone.UpdatedAt
            });
    }

    [HttpPut("zones/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateZone(int id, [FromBody] UpdateDnssecZoneRequest body)
    {
        if (body == null) return BadRequest(new { message = "Body is required." });
        var z = await _context.DnssecZones.FindAsync(id);
        if (z == null) return NotFound();
        z.Enabled = body.Enabled;
        z.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _dnssecCatalog.InvalidateZone(z.Apex);
        return Ok(new DnssecZoneRowDto
        {
            Id = z.Id,
            Apex = z.Apex,
            Enabled = z.Enabled,
            Algorithm = z.Algorithm,
            CreatedAt = z.CreatedAt,
            UpdatedAt = z.UpdatedAt
        });
    }

    [HttpDelete("zones/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        var z = await _context.DnssecZones.FindAsync(id);
        if (z == null) return NotFound();
        var apex = z.Apex;
        _context.DnssecZones.Remove(z);
        await _context.SaveChangesAsync();
        _dnssecCatalog.InvalidateZone(apex);
        return NoContent();
    }

    [HttpGet("zones/{id:int}/ds")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDs(int id)
    {
        var z = await _context.DnssecZones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (z == null) return NotFound();
        try
        {
            using var mat = DnssecZoneKeyMaterial.Load(z);
            var digest = mat.ComputeDsDigestSha256();
            var hex = Convert.ToHexString(digest).ToLowerInvariant();
            var line = $"{z.Apex}. IN DS {mat.KskKeyTag} {z.Algorithm} {DnsTypes.DigestSha256} {hex}";
            return Ok(new
            {
                z.Apex,
                keyTag = mat.KskKeyTag,
                algorithm = z.Algorithm,
                digestType = DnsTypes.DigestSha256,
                digestHex = hex,
                dsRecordLine = line
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DS export failed for zone {Id}", id);
            return BadRequest(new { message = "Could not compute DS record. Check key PEM data." });
        }
    }

    [HttpGet("zones/{id:int}/dnskeys")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDnskeys(int id)
    {
        var z = await _context.DnssecZones.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (z == null) return NotFound();
        try
        {
            using var ksk = ECDsa.Create();
            ksk.ImportFromPem(z.KskPrivateKeyPem);
            using var zsk = ECDsa.Create();
            zsk.ImportFromPem(z.ZskPrivateKeyPem);
            return Ok(new
            {
                z.Apex,
                kskPublicPem = ksk.ExportSubjectPublicKeyInfoPem(),
                zskPublicPem = zsk.ExportSubjectPublicKeyInfoPem()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DNSKEY export failed for zone {Id}", id);
            return BadRequest(new { message = "Could not export public keys." });
        }
    }
}
