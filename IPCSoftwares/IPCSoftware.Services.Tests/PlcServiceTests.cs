using System;
using IPCSoftware.Core.Interfaces.AppLoggerInterface;
using IPCSoftware.Services;
using Moq;
using Xunit;

namespace IPCSoftware.Services.Tests
{
    public class PlcServiceTests
    {
        private readonly Mock<IAppLogger> _logger = new();

        private PlcService CreateSut() => new PlcService(_logger.Object);

        [Fact]
        public void WritePlcDateTime_SetsSimulatedTimeAndReturnsTrue()
        {
            var sut = CreateSut();
            var target = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Local);

            var result = sut.WritePlcDateTime(target);
            var readBack = sut.ReadPlcDateTime();

            Assert.True(result);
            Assert.NotNull(readBack);
            Assert.Equal(target.AddSeconds(1), readBack.Value); // Read increments by 1s
        }

        [Fact]
        public void ReadPlcDateTime_IncrementsSimulatedTime()
        {
            var sut = CreateSut();
            var first = sut.ReadPlcDateTime();
            var second = sut.ReadPlcDateTime();

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.True(second > first);
        }
    }
}
