using Godot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiSettingsControls : UiSettingsTabBase
    {
        private static readonly LazyResource<PackedScene> _longButtonScene = new("uid://bsrpbqj62j0c7");
        private static readonly LazyResource<PackedScene> _squareButtonScene = new("uid://jvruq0x4kvgh");
        private static readonly LazyResource<PackedScene> _mouseScene = new("uid://ds6k02un5kca7");
        private static readonly LazyResource<PackedScene> _unknownKey = new("uid://dnidv3hscag0s");

        private static readonly LazyResource<PackedScene> _group = new("uid://duhv7k76yomgm");
        private static readonly LazyResource<PackedScene> _entry = new("uid://cirs6ikvuaiol");

        [UiElement("%ControlTabs")]
        private TabBar _tabBar;

        private readonly Dictionary<int, Control> _groups = [];
        private int _currentGroup = 0;

        private struct Entry
        {
            public Button Button;
            public OptionButton Options;
            public CheckBox CheckBox;
        }

        private readonly Dictionary<InputActionBase, Entry> _entries = [];
        private InputActionBase _listener = null;
        private bool _released = false;

        private AspectRatioContainer CreateControl(InputActionBase.InputType inputType, Key key, MouseButton mouseButton)
        {
            if (inputType == InputActionBase.InputType.Mouse)
            {
                if (mouseButton is MouseButton.None or MouseButton.WheelLeft or MouseButton.WheelRight)
                {
                    return null;
                }

                AspectRatioContainer mouseContainer = (AspectRatioContainer)_mouseScene.Cache.Instantiate();
                UiMouseIcon mouse = mouseContainer.GetNode<UiMouseIcon>("%Mouse");

                if (mouseButton == MouseButton.Left) mouse.LeftType = UiMouseIcon.ButtonType.Pressed;
                else if (mouseButton == MouseButton.Right) mouse.RightType = UiMouseIcon.ButtonType.Pressed;
                else if (mouseButton == MouseButton.Middle) mouse.Scroll = UiMouseIcon.ScrollType.Pressed;
                else if (mouseButton == MouseButton.WheelUp) mouse.Scroll = UiMouseIcon.ScrollType.GradientUp;
                else if (mouseButton == MouseButton.WheelDown) mouse.Scroll = UiMouseIcon.ScrollType.GradientDown;
                else if (mouseButton == MouseButton.Xbutton1) mouse.ScrollX1 = UiMouseIcon.ButtonType.Pressed;
                else if (mouseButton == MouseButton.Xbutton2) mouse.ScrollX2 = UiMouseIcon.ButtonType.Pressed;

                return mouseContainer;
            }
            else if (inputType == InputActionBase.InputType.Keyboard)
            {
                if (key == Key.None)
                {
                    return null;
                }

                long keyCode = (long)key;
                string keyString = OS.Singleton.GetKeycodeString(key);
                bool longButton = keyString.Length > 1 && !(keyCode >= (long)Key.F1 && keyCode <= (long)Key.F35);

                AspectRatioContainer keyContainer = (AspectRatioContainer)(longButton
                    ? _longButtonScene.Cache.Instantiate()
                    : _squareButtonScene.Cache.Instantiate());

                keyContainer.GetNode<UiLabelAutosize>("%Label").Text = keyString;

                return keyContainer;
            }

            return null;
        }

        private static void SetButtonControl(Button button, Control control)
        {
            for (int i = 0; i < button.GetChildCount(); i++)
            {
                button.GetChild(i).QueueFree();
            }

            control.SetAnchorsPreset(LayoutPreset.FullRect);
            button.AddChild(control);
        }

        private void CancelListen()
        {
            AspectRatioContainer control = CreateControl(_listener.Type, _listener.Key, _listener.MouseButton);
            SetButtonControl(_entries[_listener].Button, control);
            _listener = null;
            _released = false;
        }

        private void CommitListen(InputActionBase.InputType inputType, Key key, MouseButton mouseButton)
        {
            AspectRatioContainer control = CreateControl(inputType, key, mouseButton);

            if (control != null)
            {
                _listener.Type = inputType;
                _listener.Key = key;
                _listener.MouseButton = mouseButton;
                _listener.OnChange.Publish(new() { Input = _listener });
            }
            else
            {
                control = CreateControl(_listener.Type, _listener.Key, _listener.MouseButton);
            }

            SetButtonControl(_entries[_listener].Button, control);
            _listener = null;
            _released = false;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (_listener == null)
            {
                return;
            }

            if (@event is InputEventKey newKey)
            {
                CommitListen(InputActionBase.InputType.Keyboard, newKey.Keycode, MouseButton.None);
            }
            else if (@event is InputEventMouseButton newMouseButton)
            {
                CommitListen(InputActionBase.InputType.Mouse, Key.None, newMouseButton.ButtonIndex);
            }
            else
            {
                CancelListen();
            }
        }

        private void CreateEntry(InputActionBase action, VBoxContainer entriesContainer, ColorRect newEntry)
        {
            Label label = newEntry.GetNode<Label>("%Label");
            label.Text = ' ' + action.Name + ' ';

            Button button = newEntry.GetNode<Button>("%Button");
            AspectRatioContainer control = CreateControl(action.Type, action.Key, action.MouseButton);

            if (control != null)
            {
                control.SetAnchorsPreset(LayoutPreset.FullRect);
                button.AddChild(control);
            }

            button.Pressed += () =>
            {
                AspectRatioContainer unknown = (AspectRatioContainer)_unknownKey.Cache.Instantiate();
                SetButtonControl(button, unknown);
                _listener = action;
            };

            long counter = 0;
            Dictionary<long, InputActionBase.InputActionType> actions = [];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void TryAdd(InputActionBase.InputActionType type)
            {
                if (action.AllowedActionTypes.HasFlag(type))
                {
                    actions[counter++] = type;
                }
            }

            TryAdd(InputActionBase.InputActionType.Press);
            TryAdd(InputActionBase.InputActionType.Hold);
            TryAdd(InputActionBase.InputActionType.DoubleTap);
            TryAdd(InputActionBase.InputActionType.TrippleTap);
            TryAdd(InputActionBase.InputActionType.Release);
            TryAdd(InputActionBase.InputActionType.FastPress);
            TryAdd(InputActionBase.InputActionType.ReleaseAfterHold);

            OptionButton options = newEntry.GetNode<OptionButton>("%Options");
            foreach (var entry in actions)
            {
                options.AddItem(InputActionBase.ToString(entry.Value), (int)entry.Key);

                if (action.ActionType == entry.Value)
                {
                    options.Select((int)entry.Key);
                }
            }

            options.ItemSelected += (long selected) =>
            {
                action.ActionType = actions[selected];
                action.OnChange.Publish(new() { Input = action });
            };

            CheckBox checkBox = newEntry.GetNode<CheckBox>("%CheckBox");
            checkBox.Pressed += () =>
            {
                action.Toggle = checkBox.ButtonPressed;
                action.OnChange.Publish(new() { Input = action });
            };

            entriesContainer.AddChild(newEntry);

            _entries[action] = new()
            {
                Button = button,
                Options = options,
                CheckBox = checkBox
            };
        }

        public override void Initialize()
        {
            var actions = InputManager.Singleton.Actions;

            Dictionary<string, List<(string modId, string groupName, string name)>> entryNames = [];
            List<string> groupNames = [];

            foreach (var modId in actions)
            {
                foreach (var group in modId.Value)
                {
                    if (!entryNames.ContainsKey(group.Key))
                    {
                        groupNames.Add(group.Key);
                        entryNames.Add(group.Key, []);
                    }

                    foreach (var entry in group.Value)
                    {
                        if (entry.Value.CanBeRebound)
                        {
                            entryNames[group.Key].Add((modId.Key, group.Key, entry.Key));
                        }
                    }
                }
            }

            groupNames.Sort();

            int groupCounter = 0;

            foreach (var group in groupNames)
            {
                if (entryNames[group].Count == 0)
                {
                    continue;
                }

                _tabBar.AddTab(group);

                Control newGroup = (Control)_group.Cache.Instantiate();
                VBoxContainer entriesContainer = newGroup.GetNode<VBoxContainer>("%Entries");

                AddChild(newGroup);

                _groups[groupCounter] = newGroup;

                foreach (var (modId, groupName, name) in entryNames[group])
                {
                    ColorRect newEntry = (ColorRect)_entry.Cache.Instantiate();
                    CreateEntry(actions[modId][groupName][name], entriesContainer, newEntry);
                }

                newGroup.Visible = groupCounter == 0;
                groupCounter++;
            }

            _group.ClearCache();
            _entry.ClearCache();

            _longButtonScene.Precache();
            _squareButtonScene.Precache();
            _mouseScene.Precache();
            _unknownKey.Precache();

            _tabBar.TabSelected += OnTabSelected;
        }

        public override void Deinitialize()
        {
            _tabBar.TabSelected -= OnTabSelected;
        }

        private void OnTabSelected(long tab)
        {
            int index = (int)tab;

            _groups[_currentGroup].Visible = false;
            _currentGroup = index;
            _groups[_currentGroup].Visible = true;
            Close();
        }

        public override void Process(float delta)
        {
            if (_listener == null)
            {
                return;
            }

            if (Input.IsActionJustReleased(InputManager.MousePress))
            {
                _released = true;
                return;
            }

            if (!_released)
            {
                return;
            }

            MouseButton mouseButton = MouseButton.None;

            MouseButton[] buttons =
            [
                MouseButton.Left, MouseButton.Right, MouseButton.Middle,
                MouseButton.WheelUp, MouseButton.WheelDown, MouseButton.WheelLeft,
                MouseButton.WheelRight, MouseButton.Xbutton1, MouseButton.Xbutton2,
            ];

            foreach (MouseButton button in buttons)
            {
                if (Input.IsMouseButtonPressed(button))
                {
                    mouseButton = button;
                    break;
                }
            }

            if (mouseButton == MouseButton.None)
            {
                return;
            }

            CommitListen(InputActionBase.InputType.Mouse, Key.None, mouseButton);
        }

        public override void Close()
        {
            if (_listener == null)
            {
                return;
            }

            CancelListen();
        }
    }
}
