using System;
using System.Diagnostics;

namespace Weighbridge.Services
{
    public class LoggingService : ILoggingService
    {
        public void LogDebug(string message)
        {
            Debug.WriteLine($"[DEBUG] {message}");
        }

        public void LogInformation(string message)
        {
            Debug.WriteLine($"[INFO] {message}");
        }

        public void LogWarning(string message)
        {
            Debug.WriteLine($"[WARN] {message}");
        }

        public void LogError(string message, Exception exception = null)
        {
            Debug.WriteLine($"[ERROR] {message}");
            if (exception != null)
            {
                Debug.WriteLine($"Exception: {exception.GetType().Name} - {exception.Message}");
                Debug.WriteLine($"StackTrace: {exception.StackTrace}");
            }
        }

        public void LogCritical(string message, Exception exception = null)
        {
            Debug.WriteLine($"[CRITICAL] {message}");
            if (exception != null)
            {
                Debug.WriteLine($"Exception: {exception.GetType().Name} - {exception.Message}");
                Debug.WriteLine($"StackTrace: {exception.StackTrace}");
            }
        }
    }
}