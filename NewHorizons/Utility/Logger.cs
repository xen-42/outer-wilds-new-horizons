using OWML.Common;
using System;
using System.ComponentModel;
using UnityEngine;
namespace NewHorizons.Utility
{
    public static class Logger
    {

        private static LogType _logLevel = LogType.Error;

        public static void UpdateLogLevel(LogType newLevel)
        {
            _logLevel = newLevel;
        }

        private static void Log(string text, LogType type)
        {
            if (type < _logLevel) return;
            Main.Instance.ModHelper.Console.WriteLine($"{Enum.GetName(typeof(LogType), type)} : {text}", LogTypeToMessageType(type));
        }

        public static void LogVerbose(string text) => Log(text, LogType.Verbose);
        public static void Log(string text) => Log(text, LogType.Log);
        public static void LogWarning(string text) => Log(text, LogType.Warning);
        public static void LogError(string text) => Log(text, LogType.Error);

        public enum LogType
        {
            Verbose,
            Log,
            Warning,
            Error,
        }

        private static MessageType LogTypeToMessageType(LogType t) =>
            t switch
            {
                LogType.Error => MessageType.Error,
                LogType.Warning => MessageType.Warning,
                _ => MessageType.Info
            };
    }
}
