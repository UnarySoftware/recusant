using System;
using DiscordRPC.Logging;
using Godot;

namespace Unary.Core.Editor
{
    public class PluginDiscordLogger : ILogger
    {
        public LogLevel Level { get; set; }

        public PluginDiscordLogger(LogLevel level)
        {
            Level = level;
        }

        public void Trace(string message, params object[] args)
        {
            if (Level > LogLevel.Trace)
            {
                return;
            }

            if (args.Length > 0)
            {
                PluginLogger.Log(this, string.Format(message, args));
            }
            else
            {
                PluginLogger.Log(this, message);
            }
        }

        public void Info(string message, params object[] args)
        {
            if (Level > LogLevel.Info)
            {
                return;
            }

            if (args.Length > 0)
            {
                PluginLogger.Log(this, string.Format(message, args));
            }
            else
            {
                PluginLogger.Log(this, message);
            }
        }

        public void Warning(string message, params object[] args)
        {
            if (Level > LogLevel.Warning)
            {
                return;
            }

            if (args.Length > 0)
            {
                PluginLogger.Warning(this, string.Format(message, args));
            }
            else
            {
                PluginLogger.Warning(this, message);
            }
        }

        public void Error(string message, params object[] args)
        {
            if (Level > LogLevel.Error)
            {
                return;
            }

            if (args.Length > 0)
            {
                PluginLogger.Error(this, string.Format(message, args));
            }
            else
            {
                PluginLogger.Error(this, message);
            }
        }

    }
}
