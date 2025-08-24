using NUnit.Framework;
using Weighbridge.Services;
using System.Diagnostics;
using System;

namespace TestProject1
{
    [TestFixture]
    public class LoggingServiceTests
    {
        private LoggingService _loggingService;

        [SetUp]
        public void Setup()
        {
            _loggingService = new LoggingService();
        }

        [Test]
        public void LogDebug_WritesToDebugOutput()
        {
            // Arrange
            string message = "This is a debug message.";

            // Act
            _loggingService.LogDebug(message);

            // Assert
            // Cannot directly assert Debug.WriteLine output in a unit test.
            // This test primarily ensures the method can be called without throwing exceptions.
            Assert.Pass();
        }

        [Test]
        public void LogInformation_WritesToDebugOutput()
        {
            // Arrange
            string message = "This is an info message.";

            // Act
            _loggingService.LogInformation(message);

            // Assert
            Assert.Pass();
        }

        [Test]
        public void LogWarning_WritesToDebugOutput()
        {
            // Arrange
            string message = "This is a warning message.";

            // Act
            _loggingService.LogWarning(message);

            // Assert
            Assert.Pass();
        }

        [Test]
        public void LogError_WritesToDebugOutput()
        {
            // Arrange
            string message = "This is an error message.";
            Exception ex = new InvalidOperationException("Test exception.");

            // Act
            _loggingService.LogError(message, ex);

            // Assert
            Assert.Pass();
        }

        [Test]
        public void LogCritical_WritesToDebugOutput()
        {
            // Arrange
            string message = "This is a critical message.";
            Exception ex = new OutOfMemoryException("Test critical exception.");

            // Act
            _loggingService.LogCritical(message, ex);

            // Assert
            Assert.Pass();
        }
    }
}