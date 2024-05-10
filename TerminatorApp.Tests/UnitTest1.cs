using NUnit.Framework;
using System;
using TerminatorApp;

namespace TerminatorApp.Tests
{
    [TestFixture]
    public class ConfigurationTests
    {
        [Test]
        public void ParseInput_ValidInput_ReturnsConfig()
        {
            var input = "notepad 5 1";
            var result = Terminator.ParseInput(input);
            Assert.That(result.ProcessName, Is.EqualTo("notepad"));
            Assert.That(result.MaxRuntimeMs, Is.EqualTo(300000));
            Assert.That(result.CheckIntervalMs, Is.EqualTo(60000));
        }

        [Test]
        public void ParseInput_InvalidFormat_ThrowsArgumentException()
        {
            var input = "notepad 5";
            Assert.Throws<ArgumentException>(() => Terminator.ParseInput(input));
        }

        [Test]
        public void ParseInput_InvalidNumbers_ThrowsArgumentException()
        {
            var input = "notepad five one";
            Assert.Throws<ArgumentException>(() => Terminator.ParseInput(input));
        }
    }
}