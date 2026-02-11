using System;
using System.Reflection;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Shared.Models.ConfigModels;
using IPCSoftware.Services;
using Xunit;

namespace IPCSoftware_UnitTesting.Service_Tests
//D:\IPCSoftware_UnitTesting\Vertex\IPCSoftwares\tests\IPCSoftware.Services.Tests\TagConfigLoaderTests.cs
{
    // Minimal no-op logger for tests
    internal class TestLogger : IAppLogger
    {
        public void LogInfo(string message, LogType type) { }
        public void LogWarning(string message, LogType type) { }
        public void LogError(string message, LogType type, string memberName = "", string filePath = "", int lineNumber = 0) { }
    }

    public class TagConfigLoaderTests
    {
        private readonly TagConfigLoader _loader;
        private readonly Type _loaderType;

        public TagConfigLoaderTests()
        {
            _loader = new TagConfigLoader(new TestLogger());
            _loaderType = typeof(TagConfigLoader);
        }

        // Helper: invoke private instance method returning int
        private int InvokePrivateInt(string methodName, params object[] parameters)
        {
            var m = _loaderType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);
            var result = m.Invoke(_loader, parameters);
            return Convert.ToInt32(result);
        }

        // Helper: invoke private instance method returning bool
        private bool InvokePrivateBool(string methodName, params object[] parameters)
        {
            var m = _loaderType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);
            var result = m.Invoke(_loader, parameters);
            return Convert.ToBoolean(result);
        }

        [Theory]
        [InlineData("INT", 1)]
        [InlineData("int16", 1)]
        [InlineData("word", 2)]
        [InlineData("dint", 2)]
        [InlineData("dword", 2)]
        [InlineData("int32", 2)]
        [InlineData("bit", 3)]
        [InlineData("bool", 3)]
        [InlineData("fp", 4)]
        [InlineData("float", 4)]
        [InlineData("real", 4)]
        [InlineData("string", 5)]
        [InlineData("uint", 6)]
        [InlineData("uint16", 6)]
        [InlineData("uint32", 7)]
        [InlineData("unknown-type", 1)] // default fallback
        public void ParseDataType_ReturnsExpectedCode(string input, int expected)
        {
            int actual = InvokePrivateInt("ParseDataType", new object[] { input });
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("Bit", "5", 5)]
        [InlineData("bit", "-1", 0)]   // clamped to 0
        [InlineData("Bit", "20", 15)]  // clamped to 15
        [InlineData("Bool", "3", 0)]   // not a Bit type -> 0
        [InlineData("Bit", "NotANumber", 0)] // parse failure -> 0
        [InlineData(null, "3", 0)]     // null dataTypeText -> 0
        public void ParseBitNo_HandlesVariousInputs(string dataTypeText, string bitValue, int expected)
        {
            int actual = InvokePrivateInt("ParseBitNo", new object[] { dataTypeText, bitValue });
            Assert.Equal(expected, actual);
        }

        [Theory]
        // Int16/Bit/UInt16 -> always 1
        [InlineData(1, 10, 1)]
        [InlineData(3, 5, 1)]
        [InlineData(6, 2, 1)]
        // Word32/FP/UInt32 -> 2
        [InlineData(2, 1, 2)]
        [InlineData(4, 1, 2)]
        [InlineData(7, 10, 2)]
        // String -> clamped between 1 and 50
        [InlineData(5, 0, 1)]
        [InlineData(5, 1, 1)]
        [InlineData(5, 50, 50)]
        [InlineData(5, 60, 50)]
        // Unknown -> default 1
        [InlineData(999, 10, 1)]
        public void EnforceDataLength_EnforcesExpectedLength(int dataType, int configuredLength, int expected)
        {
            int actual = InvokePrivateInt("EnforceDataLength", new object[] { dataType, configuredLength });
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("1", true)]
        [InlineData("false", false)]
        [InlineData("0", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void ParseBoolean_ParsesExpectedValues(string input, bool expected)
        {
            bool actual = InvokePrivateBool("ParseBoolean", new object[] { input });
            Assert.Equal(expected, actual);
        }
    }
}