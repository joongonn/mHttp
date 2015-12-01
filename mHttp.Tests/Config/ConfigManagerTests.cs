using System;

using NUnit.Framework;

namespace m.Config
{
    class TestConfig : IConfigurable
    {
        public string NotConfigured { get; set; }

        [EnvironmentVariable("PATH")]
        public string SystemPath { get; set; }

        [EnvironmentVariable("someListenPort")]
        public int SomeOverriddenListenPort { get; set; }

        [EnvironmentVariable("someTimeout")]
        public TimeSpan SomeTimeout { get; set; }

        [EnvironmentVariable("notSpecifiedInEnvironment")]
        public string SomeOtherString { get; set; }

        public TestConfig()
        {
            NotConfigured = "OK";
            SomeOverriddenListenPort = 80;
            SomeOtherString = null;
        }
    }

    [TestFixture]
    public class ConfigManagerTests
    {
        [Test]
        public void TestLoad()
        {
            Environment.SetEnvironmentVariable("someListenPort", "8080");
            Environment.SetEnvironmentVariable("someTimeout", "01:30:00");

            TestConfig config = ConfigManager.Load<TestConfig>();

            Assert.AreEqual(config.NotConfigured, "OK");
            Assert.AreEqual(Environment.GetEnvironmentVariable("PATH"), config.SystemPath);
            Assert.AreEqual(8080, config.SomeOverriddenListenPort);
            Assert.AreEqual(TimeSpan.FromMinutes(90), config.SomeTimeout);
            Assert.IsNull(config.SomeOtherString);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestLoadException()
        {
            Environment.SetEnvironmentVariable("someTimeout", "01,30,00!");

            ConfigManager.Load<TestConfig>();
        }
    }
}
