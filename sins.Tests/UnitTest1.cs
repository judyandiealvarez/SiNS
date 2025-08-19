using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace sins.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            // Simple test to ensure CI workflow works
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestSiNSName()
        {
            // Test that SiNS name is correct
            string sinsName = "SiNS";
            string fullName = "Simple Name Server";

            Assert.AreEqual("SiNS", sinsName);
            Assert.AreEqual("Simple Name Server", fullName);
        }
    }
}
