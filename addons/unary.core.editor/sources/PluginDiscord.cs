#if TOOLS

using DiscordRPC;
using DiscordRPC.Logging;
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginDiscord : IPluginSystem
    {

        private static readonly EditorSettingVariable<bool> _enabled = new()
        {
            EditorDefault = true,
            RuntimeDefault = false
        };

        private static readonly EditorSettingVariable<float> _refreshTimer = new()
        {
            EditorDefault = 7.0f,
            PropertyHint = PropertyHint.Range,
            HintText = "7.0,30.0,1.0"
        };

        private bool _initialized = false;
        private DiscordRpcClient _client;
        private float _timer = 0.0f;

        bool ISystem.Initialize()
        {
            if (!_enabled.Value)
            {
                return true;
            }

            _client = new DiscordRpcClient(DiscordRpc.AppId, autoEvents: false)
            {
                Logger = new PluginDiscordLogger(LogLevel.Warning)
            };

            _initialized = _client.Initialize();

            _timer = _refreshTimer.Value;

            return true;
        }

        private string _previousDetails = string.Empty;
        private string _previousState = string.Empty;

        void ISystem.Process(float delta)
        {
            _client.Invoke();

            _timer += delta;

            if (_timer < _refreshTimer.Value)
            {
                return;
            }

            _timer = 0.0f;

            string details = "Idling";
            string state = string.Empty;

            var inspector = EditorInterface.Singleton.GetInspector();

            var editedObject = inspector.GetEditedObject();

            if (editedObject != null)
            {
                if (editedObject is Node node)
                {
                    var script = node.GetScript();

                    if (script.VariantType == Variant.Type.Object)
                    {
                        var target = script.Obj as Script;
                        details = "Editing: " + target.GetGlobalName() + " (" + target.GetClass() + ")";
                    }
                    else
                    {
                        details = "Editing: " + node.GetType().FullName;
                    }

                    var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();

                    if (sceneRoot != null)
                    {
                        state = "At: " + sceneRoot.SceneFilePath;
                    }
                }
                else if (editedObject is Resource resource)
                {
                    details = "Editing: " + resource.GetType().FullName;
                    state = "At: " + resource.ResourcePath;
                }
                else
                {
                    details = "Editing: " + editedObject.GetType().FullName;
                }
            }
            else
            {
                var sceneRoot = EditorInterface.Singleton.GetEditedSceneRoot();

                if (sceneRoot != null)
                {
                    details = "Editing: " + sceneRoot.SceneFilePath;
                }
            }

            if (details == _previousDetails && state == _previousState)
            {
                return;
            }

            var richPresence = new RichPresence()
            {
                // TODO https://github.com/Lachee/discord-rpc-csharp/issues/284
                Assets = new Assets()
                {
                    LargeImageKey = "main",
                    LargeImageText = "Recusant Editor",
                    SmallImageKey = "cog",
                    SmallImageText = "Recusant Editor"
                },
            };

            if (details != string.Empty)
            {
                richPresence.Details = details;
            }

            if (state != string.Empty)
            {
                richPresence.State = state;
            }

            _client.SetPresence(richPresence);

            _previousDetails = details;
            _previousState = state;
        }

        void ISystem.Deinitialize()
        {
            if (!_initialized)
            {
                return;
            }

            _client.Dispose();
        }
    }
}

#endif
