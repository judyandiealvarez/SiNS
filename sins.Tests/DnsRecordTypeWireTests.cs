using Microsoft.VisualStudio.TestTools.UnitTesting;
using sins.Services;

namespace sins.Tests;

[TestClass]
public class DnsRecordTypeWireTests
{
    [TestMethod]
    [DataRow("DNSKEY", 48)]
    [DataRow("dnskey", 48)]
    [DataRow("DS", 43)]
    [DataRow("RRSIG", 46)]
    [DataRow("NSEC", 47)]
    [DataRow("OPT", 41)]
    [DataRow("A", 1)]
    [DataRow("AAAA", 28)]
    public void ToWireType_KnownNames(string name, int expected) =>
        Assert.AreEqual((ushort)expected, DnsRecordTypeWire.ToWireType(name));

    [TestMethod]
    public void ToWireType_NumericString_ParsesAsCode() =>
        Assert.AreEqual(48, DnsRecordTypeWire.ToWireType("48"));

    /// <summary>Regression: mis-mapping DNSKEY to A caused dig "Question section mismatch" on recursive answers.</summary>
    [TestMethod]
    public void ToWireType_Dnskey_IsNotA() =>
        Assert.AreNotEqual(DnsRecordTypeWire.ToWireType("A"), DnsRecordTypeWire.ToWireType("DNSKEY"));

    [TestMethod]
    public void ToWireType_RecursiveQueryQuestionBytes_EndWithDnskeyType()
    {
        // Same wire layout as DnsServer.CreateDnsRequest question tail: QTYPE + QCLASS(IN).
        var name = "yourzone.example";
        var type = DnsRecordTypeWire.ToWireType("DNSKEY");
        var buf = new List<byte>();
        foreach (var part in name.Split('.'))
        {
            buf.Add((byte)part.Length);
            buf.AddRange(System.Text.Encoding.ASCII.GetBytes(part));
        }

        buf.Add(0);
        buf.Add((byte)(type >> 8));
        buf.Add((byte)(type & 0xFF));
        buf.Add(0);
        buf.Add(1);
        var arr = buf.ToArray();
        Assert.IsTrue(arr.Length >= 4);
        Assert.AreEqual(0x00, arr[^4]);
        Assert.AreEqual(0x30, arr[^3]);
        Assert.AreEqual(0x00, arr[^2]);
        Assert.AreEqual(0x01, arr[^1]);
    }
}
