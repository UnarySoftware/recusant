using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class InputManager : Node, IModSystem
    {
        private int _scope;

        public void SetScope<T>(T scope) where T : struct, IConvertible
        {
            _scope = Convert.ToInt32(scope);
        }

        public T GetScope<T>() where T : struct, IConvertible
        {
            return Unsafe.BitCast<int, T>(_scope);
        }

        // TODO These will have to be a runtime assignable variables
        public float FastClickTime = 0.1f;
        public float DoubleClickTime = 0.5f;
        public float TrippleClickTime = 0.75f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvertMouseMode()
        {
            if (Input.Singleton.MouseMode == Input.MouseModeEnum.Captured)
            {
                Input.Singleton.MouseMode = Input.MouseModeEnum.Visible;
            }
            else if (Input.Singleton.MouseMode == Input.MouseModeEnum.Visible)
            {
                Input.Singleton.MouseMode = Input.MouseModeEnum.Captured;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasScope<T>(T scope) where T : struct, IConvertible
        {
            return HasScope(Convert.ToInt32(scope));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasScope(int scope)
        {
            return (_scope & scope) == scope;
        }

        public bool IsActionJustReleased(StringName action, int scope)
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsActionJustReleased(action);
        }

        public bool IsActionJustPressed(StringName action, int scope)
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsActionJustPressed(action);
        }

        public bool IsActionPressed(StringName action, int scope)
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsActionPressed(action);
        }

        public Vector2 GetVector<T>(InputActionBase negativeX, InputActionBase positiveX, InputActionBase negativeY, InputActionBase positiveY, T scope, float delta) where T : struct, IConvertible
        {
            if (!HasScope(Convert.ToInt32(scope)))
            {
                return Vector2.Zero;
            }

            Vector2 result = new();

            if (negativeY.Poll(delta))
            {
                result.Y -= negativeY.GetActionStrength();
            }

            if (positiveY.Poll(delta))
            {
                result.Y += positiveY.GetActionStrength();
            }

            if (positiveX.Poll(delta))
            {
                result.X += positiveX.GetActionStrength();
            }

            if (negativeX.Poll(delta))
            {
                result.X -= negativeX.GetActionStrength();
            }

            result = result.Normalized();

            return result;
        }


        public bool IsKeyPressed(Key key, int scope)
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsKeyPressed(key);
        }

        public bool IsMouseButtonPressed(MouseButton button, int scope)
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsMouseButtonPressed(button);
        }

        public Dictionary<string, Dictionary<string, Dictionary<string, InputActionBase>>> Actions { get; private set; } = [];

        public static StringName MousePress = new("ui_mouse_press");

        bool ISystem.Initialize()
        {
            InputMap.Singleton.AddAction(MousePress);
            InputMap.Singleton.ActionAddEvent(MousePress, new InputEventMouseButton()
            {
                ButtonIndex = MouseButton.Left
            });

            Type[] types = typeof(InputManager).Assembly.GetTypes();

            foreach (var type in types)
            {
                FieldInfo[] properties = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

                foreach (var property in properties)
                {
                    if (property.FieldType != typeof(InputActionBase) &&
                    property.FieldType.BaseType != typeof(InputActionBase))
                    {
                        continue;
                    }

                    InputActionBase inputBase = (InputActionBase)property.GetValue(null);

                    inputBase.ModId = type.Namespace.ToPath();
                    inputBase.Action = new(inputBase.ModId + '/' + inputBase.Group.ToPath() + '/' + inputBase.Name.ToPath());

                    InputMap.Singleton.AddAction(inputBase.Action);

                    if (inputBase.Type == InputActionBase.InputType.Keyboard)
                    {
                        InputMap.Singleton.ActionAddEvent(inputBase.Action, new InputEventKey()
                        {
                            Keycode = inputBase.Key
                        });
                    }
                    else
                    {
                        InputMap.Singleton.ActionAddEvent(inputBase.Action, new InputEventMouseButton()
                        {
                            ButtonIndex = inputBase.MouseButton
                        });
                    }

                    string modId = inputBase.ModId;

                    if (!Actions.TryGetValue(modId, out var modIdEntries))
                    {
                        modIdEntries = [];
                        Actions.Add(modId, modIdEntries);
                    }

                    string group = inputBase.Group;

                    if (!modIdEntries.TryGetValue(group, out var entries))
                    {
                        entries = [];
                        modIdEntries.Add(group, entries);
                    }

                    inputBase.OnChange.Subscribe(OnChange, this);

                    entries.Add(inputBase.Name, inputBase);
                }
            }

            Load();

            return true;
        }

        private bool OnChange(ref InputActionBase.ChangeData data)
        {
            InputMap.Singleton.ActionEraseEvents(data.Input.Action);

            if (data.Input.Type == InputActionBase.InputType.Keyboard)
            {
                InputMap.Singleton.ActionAddEvent(data.Input.Action, new InputEventKey()
                {
                    Keycode = data.Input.Key
                });
            }
            else
            {
                InputMap.Singleton.ActionAddEvent(data.Input.Action, new InputEventMouseButton()
                {
                    ButtonIndex = data.Input.MouseButton
                });
            }

            return true;
        }

        void ISystem.Deinitialize()
        {
            foreach (var modId in Actions)
            {
                foreach (var group in modId.Value)
                {
                    foreach (var entry in group.Value)
                    {
                        entry.Value.OnChange.Unsubscribe(this);
                    }
                }
            }

            Save();
        }

        private void Save()
        {
            foreach (var modId in Actions)
            {
                List<InputActionBaseSerializable> _actions = [];

                foreach (var group in modId.Value)
                {
                    foreach (var entry in group.Value)
                    {
                        if (entry.Value.CanBeRebound)
                        {
                            _actions.Add(entry.Value.Serialize());
                        }
                    }
                }

                if (_actions.Count > 0)
                {
                    StorageManager.Singleton.WriteEntryText(modId.Key, nameof(InputManager), JsonSerializer.Serialize(_actions, JsonConverters.IndentedOptions));
                }
            }
        }

        private void Load()
        {
            foreach (var modId in Actions)
            {
                string content = StorageManager.Singleton.ReadEntryText(modId.Key, nameof(InputManager));

                if (content == string.Empty)
                {
                    continue;
                }

                List<InputActionBaseSerializable> _actions = JsonSerializer.Deserialize<List<InputActionBaseSerializable>>(content, JsonConverters.IndentedOptions);

                foreach (var action in _actions)
                {
                    if (!modId.Value.TryGetValue(action.Group, out var group))
                    {
                        this.Warning($"Input settings for modId \"{modId.Key}\" contained unknown group \"{action.Group}\", skipping...");
                        continue;
                    }

                    if (!group.TryGetValue(action.Name, out var entry))
                    {
                        this.Warning($"Input settings for modId \"{modId.Key}\" contained unknown name \"{action.Name}\" within a group \"{action.Group}\", skipping...");
                        continue;
                    }

                    if (!entry.CanBeRebound)
                    {
                        this.Warning($"Input setting for modId \"{modId.Key}\" at group \"{action.Group}\" with name \"{action.Name}\" just tried rebinding reserved input, skipping...");
                        continue;
                    }

                    entry.Deserialize(action);
                }
            }
        }
    }
}
