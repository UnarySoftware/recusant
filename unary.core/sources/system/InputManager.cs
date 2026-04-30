using Godot;
using System;
using System.Runtime.CompilerServices;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class InputManager : Node, IModSystem
    {
        public void SetScope<T>(T scope) where T : struct, IConvertible
        {
            _scope = Convert.ToInt32(scope);
        }

        public T GetScope<T>() where T : struct, IConvertible
        {
            return Unsafe.BitCast<int, T>(_scope);
        }

        private int _scope { get; set; }

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
            int intScope = Convert.ToInt32(scope);

            return (_scope & intScope) == intScope;
        }

        public bool IsActionJustReleased<T>(StringName action, T scope) where T : struct, IConvertible
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsActionJustReleased(action);
        }

        public bool IsActionJustPressed<T>(StringName action, T scope) where T : struct, IConvertible
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsActionJustPressed(action);
        }

        public bool IsActionPressed<T>(StringName action, T scope) where T : struct, IConvertible
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsActionPressed(action);
        }

        public float GetActionStrength<T>(StringName action, T scope) where T : struct, IConvertible
        {
            if (!HasScope(scope))
            {
                return 0.0f;
            }

            return Input.Singleton.GetActionStrength(action);
        }

        public Vector2 GetVector<T>(StringName negativeX, StringName positiveX, StringName negativeY, StringName positiveY, T scope, float deadzone = -1.0f) where T : struct, IConvertible
        {
            if (!HasScope(scope))
            {
                return Vector2.Zero;
            }

            return Input.Singleton.GetVector(negativeX, positiveX, negativeY, positiveY, deadzone);
        }

        public bool IsKeyPressed<T>(Key key, T scope) where T : struct, IConvertible
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsKeyPressed(key);
        }

        public bool IsMouseButtonPressed<T>(MouseButton button, T scope) where T : struct, IConvertible
        {
            if (!HasScope(scope))
            {
                return false;
            }

            return Input.Singleton.IsMouseButtonPressed(button);
        }

        bool ISystem.Initialize()
        {
            return true;
        }
    }
}
