using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        private List<InputActionBase> _actions = [];

        bool ISystem.Initialize()
        {
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

                    inputBase.Action = new(type.Namespace.ToPath() + '/' + inputBase.Group.ToPath() + '/' + inputBase.Name.ToPath());

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

                    _actions.Add(inputBase);
                }
            }

            return true;
        }
    }
}
