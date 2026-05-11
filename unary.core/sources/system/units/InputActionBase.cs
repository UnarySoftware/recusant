using Godot;
using System;
using System.Runtime.CompilerServices;

namespace Unary.Core
{
    public class InputActionBase
    {
        [Flags]
        public enum InputActionType : int
        {
            None = 0,
            Press = 1 << 0,
            Hold = 1 << 1,
            DoubleTap = 1 << 2,
            TrippleTap = 1 << 3,
            Release = 1 << 4,
            FastPress = 1 << 5,
            ReleaseAfterHold = 1 << 6,
            NoHold = Press | DoubleTap | TrippleTap | Release | FastPress | ReleaseAfterHold,
            All = Press | Hold | DoubleTap | TrippleTap | Release | FastPress | ReleaseAfterHold
        };

        public static string ToString(InputActionType type)
        {
            switch (type)
            {
                default:
                case InputActionType.None:
                    {
                        return "None";
                    }
                case InputActionType.Press:
                    {
                        return "Press";
                    }
                case InputActionType.Hold:
                    {
                        return "Hold";
                    }
                case InputActionType.DoubleTap:
                    {
                        return "Double Tap";
                    }
                case InputActionType.TrippleTap:
                    {
                        return "Tripple Tap";
                    }
                case InputActionType.Release:
                    {
                        return "Release";
                    }
                case InputActionType.FastPress:
                    {
                        return "Fast Press";
                    }
                case InputActionType.ReleaseAfterHold:
                    {
                        return "Release After Hold";
                    }
            }
        }

        public enum InputType
        {
            Keyboard,
            Mouse
        };

        public string ModId;
        public string Group;
        public string Name;
        public InputActionType AllowedActionTypes;

        public InputActionType ActionType
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                Reset();
            }
        }

        public InputType Type
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                Reset();
            }
        }

        public bool Toggle
        {

            get
            {
                return field;
            }
            set
            {
                field = value;
                Reset();
            }
        }

        public Key Key
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                Reset();
            }
        }

        public MouseButton MouseButton
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                Reset();
            }
        }

        public bool CanBeRebound = true;

        public struct ChangeData
        {
            public InputActionBase Input;
        }

        public EventFunc<ChangeData> OnChange = new();

        public int BaseScope;

        public InputActionBaseSerializable Serialize()
        {
            return new()
            {
                Group = Group,
                Name = Name,
                ActionType = ActionType,
                Type = Type,
                Toggle = Toggle,
                Key = Key,
                MouseButton = MouseButton,
            };
        }

        public void Deserialize(InputActionBaseSerializable data)
        {
            if (AllowedActionTypes.HasFlag(data.ActionType))
            {
                ActionType = data.ActionType;
            }

            Type = data.Type;
            Toggle = data.Toggle;

            if (data.Key != Key.Escape)
            {
                Key = data.Key;
            }

            if (data.MouseButton != MouseButton.WheelLeft && data.MouseButton != MouseButton.WheelRight)
            {
                MouseButton = data.MouseButton;
            }

            OnChange.Publish(new() { Input = this });
        }

        public StringName Action;

        private bool _toggled = false;

        int _pressedCount = 0;
        int _releasedCount = 0;

        private float _timer = 0.0f;

        public void Reset()
        {
            _toggled = false;
            _pressedCount = 0;
            _releasedCount = 0;
            _timer = 0.0f;
        }

        private bool Press()
        {
            if (!Toggle)
            {
                return InputManager.Singleton.IsActionJustPressed(Action, BaseScope);
            }

            if (InputManager.Singleton.IsActionJustPressed(Action, BaseScope))
            {
                _toggled = !_toggled;
            }

            return _toggled;
        }

        private bool Hold()
        {
            if (!Toggle)
            {
                return InputManager.Singleton.IsActionPressed(Action, BaseScope);
            }

            if (InputManager.Singleton.IsActionPressed(Action, BaseScope))
            {
                _toggled = !_toggled;
            }

            return _toggled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsMultipleTap(float delta, int targetPress, float timeout)
        {
            if (_pressedCount > 0)
            {
                _timer += delta;
            }

            if (InputManager.Singleton.IsActionJustPressed(Action, BaseScope))
            {
                _pressedCount++;
            }

            if (InputManager.Singleton.IsActionJustReleased(Action, BaseScope))
            {
                _releasedCount++;
            }

            if (_pressedCount == targetPress && _releasedCount == targetPress && _timer < timeout)
            {
                _pressedCount = 0;
                _releasedCount = 0;
                _timer = 0.0f;
                return true;
            }

            if (_timer > timeout)
            {
                _pressedCount = 0;
                _releasedCount = 0;
                _timer = 0.0f;
            }

            return false;
        }

        private bool DoubleTap(float delta)
        {
            if (!Toggle)
            {
                return IsMultipleTap(delta, 2, InputManager.Singleton.DoubleClickTime);
            }

            if (IsMultipleTap(delta, 2, InputManager.Singleton.DoubleClickTime))
            {
                _toggled = !_toggled;
            }

            return _toggled;
        }

        private bool TrippleTap(float delta)
        {
            if (!Toggle)
            {
                return IsMultipleTap(delta, 3, InputManager.Singleton.TrippleClickTime);
            }

            if (IsMultipleTap(delta, 3, InputManager.Singleton.TrippleClickTime))
            {
                _toggled = !_toggled;
            }

            return _toggled;
        }

        private bool Release()
        {
            if (!Toggle)
            {
                return InputManager.Singleton.IsActionJustReleased(Action, BaseScope);
            }

            if (InputManager.Singleton.IsActionJustReleased(Action, BaseScope))
            {
                _toggled = !_toggled;
            }

            return _toggled;
        }

        private bool FastPress(float delta)
        {
            if (!Toggle)
            {
                return IsMultipleTap(delta, 1, InputManager.Singleton.FastClickTime);
            }

            if (IsMultipleTap(delta, 1, InputManager.Singleton.FastClickTime))
            {
                _toggled = !_toggled;
            }

            return _toggled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsReleaseAfterHold(float delta)
        {
            if (InputManager.Singleton.IsActionPressed(Action, BaseScope))
            {
                _timer += delta;
            }
            else if (InputManager.Singleton.IsActionJustReleased(Action, BaseScope) && _timer > InputManager.Singleton.DoubleClickTime)
            {
                _timer = 0.0f;
                return true;
            }
            else
            {
                _timer = 0.0f;
            }

            return false;
        }

        private bool ReleaseAfterHold(float delta)
        {
            if (!Toggle)
            {
                return IsReleaseAfterHold(delta);
            }

            if (IsReleaseAfterHold(delta))
            {
                _toggled = !_toggled;
            }

            return _toggled;
        }

        public bool Poll(float delta)
        {
            switch (ActionType)
            {
                default:
                case InputActionType.Press:
                    {
                        return Press();
                    }
                case InputActionType.Hold:
                    {
                        return Hold();
                    }
                case InputActionType.DoubleTap:
                    {
                        return DoubleTap(delta);
                    }
                case InputActionType.TrippleTap:
                    {
                        return TrippleTap(delta);
                    }
                case InputActionType.Release:
                    {
                        return Release();
                    }
                case InputActionType.FastPress:
                    {
                        return FastPress(delta);
                    }
                case InputActionType.ReleaseAfterHold:
                    {
                        return ReleaseAfterHold(delta);
                    }
            }
        }

        public bool Poll(float delta, InputActionType reboundType)
        {
            InputActionType previousType = ActionType;
            ActionType = reboundType;
            bool result = Poll(delta);
            ActionType = previousType;
            return result;
        }

        public float GetActionStrength()
        {
            if (!InputManager.Singleton.HasScope(BaseScope))
            {
                return 0.0f;
            }

            if (!Toggle)
            {
                return Input.Singleton.GetActionStrength(Action);
            }

            if (_toggled)
            {
                return 1.0f;
            }
            else
            {
                return 0.0f;
            }
        }

    }
}
