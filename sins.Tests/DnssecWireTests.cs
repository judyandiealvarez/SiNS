using Microsoft.VisualStudio.TestTools.UnitTesting;
using sins.Services.Dnssec;

namespace sins.Tests;

[TestClass]
public class DnssecWireTests
{
    [TestMethod]
    public void DnsCanonical_SortsLabelsRightToLeft()
    {
        var names = new List<string> { "www.example.com", "example.com", "a.example.com" };
        DnsCanonical.SortNames(names);
        CollectionAssert.AreEqual(new[] { "example.com", "a.example.com", "www.example.com" }, names);
    }

    [TestMethod]
    public void DnssecKeyTag_IsStableForSameRdata()
    {
        var rdata = new byte[68];
        rdata[0] = 0x01;
        rdata[1] = 0x01;
        rdata[2] = 3;
        rdata[3] = 13;
        var tag = DnssecKeyTag.Compute(rdata);
        Assert.AreEqual(DnssecKeyTag.Compute(rdata), tag);
    }

    [TestMethod]
    public void NsecTypeBitmap_EncodesWindowZero()
    {
        var b = NsecTypeBitmap.Encode(new ushort[] { 1, 2, 47 });
        Assert.IsTrue(b.Length >= 4);
        Assert.AreEqual(0, b[0]);
    }

    [TestMethod]
    public void DnsQueryParser_ReadsDoFromOptAdditional()
    {
        var q = new List<byte>
        {
            0xab, 0xcd, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
        };
        q.Add(3);
        q.AddRange("www"u8.ToArray());
        q.Add(7);
        q.AddRange("example"u8.ToArray());
        q.Add(3);
        q.AddRange("com"u8.ToArray());
        q.Add(0);
        q.AddRange(new byte[] { 0x00, 0x01, 0x00, 0x01 });
        q.Add(0);
        q.AddRange(new byte[] { 0x00, 0x29, 0x04, 0xd0, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00 });
        Assert.IsTrue(DnsQueryParser.TryParseDetailed(q.ToArray(), out var r));
        Assert.IsTrue(r.DnssecOk);
        Assert.AreEqual(1232, r.ClientUdpPayloadSize);
        Assert.AreEqual("www.example.com", r.QName);
    }
}
