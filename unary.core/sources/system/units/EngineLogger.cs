using Godot;
using System.Text;

namespace Unary.Core
{
    public partial class EngineLogger : Logger
    {
        private readonly StringBuilder _builder = new();

        public override void _LogError(string function, string file, int line, string code, string rationale, bool editorNotify, int errorType, Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
        {
            ErrorType errorTypeCasted = (ErrorType)errorType;

            RuntimeLogger.LogEventData newData = new();

            _builder.Clear();

            bool error = true;

            switch (errorTypeCasted)
            {
                case ErrorType.Script:
                    {
                        _builder.Append("[SCRIPT] ");
                        break;
                    }
                case ErrorType.Shader:
                    {
                        _builder.Append("[SHADER] ");
                        break;
                    }
                case ErrorType.Warning:
                    {
                        error = false;
                        break;
                    }
            }

            if (error)
            {
                newData.Type = RuntimeLogger.LogType.Error;
            }
            else
            {
                newData.Type = RuntimeLogger.LogType.Warning;
            }

            if (string.IsNullOrEmpty(rationale))
            {
                if (error)
                {
                    string[] lines = code.Replace("\r", "").Split('\n');

                    if (lines.Length == 1)
                    {
                        _builder.Append(code);
                    }
                    else
                    {
                        if (lines[0].Contains("Exception"))
                        {
                            _builder.Append(lines[0]);

                            newData.Message = _builder.ToString();

                            _builder.Clear();

                            int backCounter = -1;

                            foreach (var targetLine in lines)
                            {
                                if (backCounter == -1)
                                {
                                    _builder.Append("\n C# backtrace (most recent call first):");
                                    backCounter++;
                                    continue;
                                }

                                _builder.Append('\n').Append(targetLine.Replace(" at ", $" [{backCounter}] ").Replace('\\', '/'));

                                backCounter++;
                            }

                            newData.StackTrace = _builder.ToString();

                            RuntimeLogger.OnLog.Publish(newData);
                            return;
                        }
                        else
                        {
                            _builder.Append(code);
                        }
                    }

                }
                else
                {
                    _builder.Append(code);
                }
            }
            else
            {
                _builder.Append(rationale);
            }

            newData.Message = _builder.ToString();

            _builder.Clear();

            if (scriptBacktraces.Count == 0)
            {
                RuntimeLogger.OnLog.Publish(newData);
                return;
            }

            int counter = 1;

            foreach (var backtrace in scriptBacktraces)
            {
                _builder.Append(backtrace.Format(1, 1)).Replace('\\', '/');

                if (counter != scriptBacktraces.Count)
                {
                    _builder.Append('\n');
                }

                counter++;
            }

            newData.StackTrace = _builder.ToString();

            RuntimeLogger.OnLog.Publish(newData);
        }

        public override void _LogMessage(string message, bool error)
        {
            RuntimeLogger.LogEventData newData = new();

            if (error)
            {
                newData.Type = RuntimeLogger.LogType.Error;
            }
            else
            {
                newData.Type = RuntimeLogger.LogType.Log;
            }

            newData.StackTrace = null;
            newData.Message = message.Trim('\n');

            RuntimeLogger.OnLog.Publish(newData);
        }
    }
}
