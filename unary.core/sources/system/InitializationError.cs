using System.Collections.Generic;
using Godot;

namespace Unary.Core
{
    public static class InitializationError
    {
        public static void Show(ErrorType type, params object[] parameters)
        {
            string formatted = type.ToString().Replace("_", " ");
            OS.Singleton.Alert(GetLocalizedError(type, parameters), formatted);
        }

        public static string GetLocalizedError(ErrorType type, params object[] parameters)
        {
            string newLines = "\n\n";
            string result = $"Failed to find a localized string for an error type \"{type}\"\n";
            string validate = "Failed to find a localized string for a file validation request";

            string language = OS.Singleton.GetLocaleLanguage();

            if (_validateGameRequest.TryGetValue(language, out var validated))
            {
                validate = validated;
            }
            else if (_validateGameRequest.TryGetValue("en", out var validatedEnglish))
            {
                validate = validatedEnglish;
            }

            if (!_localized.TryGetValue(type, out var error))
            {
                return result + newLines + validate;
            }

            if (error.TryGetValue(language, out string localizedString))
            {
                return string.Format(localizedString, parameters) + newLines + validate;
            }

            if (error.TryGetValue("en", out string englishString))
            {
                return string.Format(englishString, parameters) + newLines + validate;
            }

            return result + newLines + validate;
        }

        public enum ErrorType
        {
            // Generic
            File_Missing,
            File_Corrupted_Exception,
            // Steam-specific
            Steamworks_Pack_Size,
            Steamworks_Dll_Check,
            Steamworks_Dll_Not_Found,
            Steamworks_Init_Failed,
            Steamworks_Not_Launched,
            Steamworks_EnvironmentVariable_Failed,
            // Systems-specific
            System_Invalid_Namespace
        }

        private static readonly Dictionary<string, string> _validateGameRequest = new()
    {
        {
            "en", "If this issue persists - try validating game files with Steam."
        }
    };

        private static readonly Dictionary<ErrorType, Dictionary<string, string>> _localized = new()
    {
        {
            ErrorType.File_Missing, new()
            {
                {
                    "en", "Missing \"{0}\" file at path \"{1}\"."
                }
            }
        },
        {
            ErrorType.File_Corrupted_Exception, new()
            {
                {
                    "en", "Failed loading \"{0}\" file at path \"{1}\".\n" +
                    "Error message:\n" +
                    "{2}\n" +
                    "Stack trace:\n" +
                    "{3}"
                }
            }
        },
        {
            ErrorType.Steamworks_Pack_Size, new()
            {
                {
                    "en", "Wrong version of Steamworks.NET is being run in this platform."
                }
            }
        },
        {
            ErrorType.Steamworks_Dll_Check, new()
            {
                {
                    "en", "One or more of the Steamworks binaries seems to be the wrong version."
                }
            }
        },
        {
            ErrorType.Steamworks_Dll_Not_Found, new()
            {
                {
                    "en", "Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location.\n" +
                    "Error message:\n" +
                    "{0}\n" +
                    "Stack trace:\n" +
                    "{1}"
                }
            }
        },
        {
            ErrorType.Steamworks_Init_Failed, new()
            {
                {
                    "en", "Steamworks failed to initialize.\n" +
                    "Error message:\n" +
                    "{0}\n" +
                    "Stack trace:\n" +
                    "{1}"
                }
            }
        },
        {
            ErrorType.Steamworks_Not_Launched, new()
            {
                {
                    "en", "Steam is required to play this game.\n" +
                    "Make sure that it is turned on and try launching the game again."
                }
            }
        },
        {
            ErrorType.Steamworks_EnvironmentVariable_Failed, new()
            {
                {
                    "en", "Steamworks failed to setup environment variables.\n" +
                    "Error message:\n" +
                    "{0}\n" +
                    "Stack trace:\n" +
                    "{1}"
                }
            }
        },
        {
            ErrorType.System_Invalid_Namespace, new()
            {
                {
                    "en", "Class with a fullpath \"{0}\" has bad full class name\n" +
                    "If this issue persists - try validating game files with Steam."
                }
            }
        },
    };

    }
}