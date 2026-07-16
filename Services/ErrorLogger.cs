using System;
using System.IO;

namespace CooldownReady.Services
{
    /// <summary>
    /// %LOCALAPPDATA%\CooldownReady\error.log 에 오류를 기록합니다.
    /// </summary>
    public static class ErrorLogger
    {
        public static string AppDataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CooldownReady");

        public static string LogPath => Path.Combine(AppDataDirectory, "error.log");

        public static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
            try
            {
                Directory.CreateDirectory(AppDataDirectory);
                File.AppendAllText(LogPath, $"[{DateTime.Now}] {message}\n\n");
            }
            catch
            {
                // 로그 기록 실패는 무시
            }
        }

        public static void Log(string context, Exception exception)
        {
            Log($"{context}: {exception}");
        }
    }
}
