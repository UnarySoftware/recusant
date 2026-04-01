
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UiCoreConsole : UiUnit<UiCoreState>
    {
        private bool _enabled = false;

        [UiElement("%ConsoleRoot")]
        private VBoxContainer _consoleRoot;

        [UiElement("%ConsoleHeader")]
        private MarginContainer _consoleHeader;

        [UiElement("%FontIncrease")]
        private MarginContainer _fontIncrease;

        [UiElement("%FontIncreaseButton")]
        private Button _fontIncreaseButton;

        [UiElement("%FontDecrease")]
        private MarginContainer _fontDecrease;

        [UiElement("%FontDecreaseButton")]
        private Button _fontDecreaseButton;

        [UiElement("%Close")]
        private MarginContainer _close;

        [UiElement("%CloseButton")]
        private Button _closeButton;

        [UiElement("%ConsoleBackground")]
        private ColorRect _background;

        [UiElement("%ConsoleScroll")]
        private ScrollContainer _scroll;

        [UiElement("%ConsoleEntries")]
        private VBoxContainer _entries;

        private struct Entry
        {
            public MarginContainer Root;
            public Label Label;
        }

        private readonly List<Entry> _entriesList = [];
        private readonly Queue<Entry> _entriesQueue = [];
        private int _usedEntries = 0;
        public const int MaxEntries = 256;

        private int _currentFontSize = 20;
        public const int MinFontSize = 8;
        public const int MaxFontSize = 36;
        public const int FontChangePerClick = 4;

        private void ToggleVisibility(bool newValue)
        {
            if (newValue)
            {
                _consoleRoot.MouseFilter = Control.MouseFilterEnum.Stop;
            }
            else
            {
                _consoleRoot.MouseFilter = Control.MouseFilterEnum.Ignore;
            }

            _consoleHeader.Visible = newValue;
            _fontIncrease.Visible = newValue;
            _fontDecrease.Visible = newValue;
            _close.Visible = newValue;
            _background.Visible = newValue;

            State.GetUnit<UiCoreConsoleCounters>().Visible = newValue;

            _enabled = newValue;
        }

        private Input.MouseModeEnum _mouseMode;

        public void Enable()
        {
            ToggleVisibility(true);
            _mouseMode = Input.Singleton.MouseMode;
            Input.Singleton.MouseMode = Input.MouseModeEnum.Visible;
        }

        public void Disable()
        {
            ToggleVisibility(false);
            _currentLabel = null;
            Input.Singleton.MouseMode = _mouseMode;
        }

        public void Toggle()
        {
            if (_enabled)
            {
                Disable();
            }
            else
            {
                Enable();
            }
        }

        private Label _currentLabel;

        private void OnMouseEntered(Label label)
        {
            _currentLabel = label;
        }

        private void OnMouseExited()
        {
            _currentLabel = null;
        }

        private readonly StringBuilder _builder = new();

        public override void Process(float delta)
        {
            if (_currentLabel == null)
            {
                return;
            }

            if (!Input.IsMouseButtonPressed(MouseButton.Left) && !Input.IsMouseButtonPressed(MouseButton.Right))
            {
                return;
            }

            _builder.Append(_currentLabel.Text);

            if (!string.IsNullOrEmpty(_currentLabel.TooltipText))
            {
                _builder.Append('\n').Append(_currentLabel.TooltipText);
            }

            DisplayServer.Singleton.ClipboardSet(_builder.ToString());

            _builder.Clear();
        }

        private UiCoreConsoleData _data;
        private AudioStreamPlayer _warnVoice;
        private AudioStreamPlayer _errorVoice;

        public override void Initialize()
        {
            _data = (UiCoreConsoleData)Resources.Singleton.LoadPatched("res://unary.core/ui/console_data.tres", nameof(UiCoreConsoleData));
            _warnVoice = new()
            {
                Stream = _data.Warning,
                Bus = "UI"
            };
            AddChild(_warnVoice);
            _errorVoice = new()
            {
                Stream = _data.Error,
                Bus = "UI"
            };
            AddChild(_errorVoice);

            _fontIncreaseButton.Pressed += () => { UpdateFont(FontChangePerClick); };
            _fontDecreaseButton.Pressed += () => { UpdateFont(-FontChangePerClick); };
            _closeButton.Pressed += () => { Toggle(); };

            MarginContainer entry = Root.GetNode<MarginContainer>("%ConsoleEntry");
            Label label = entry.GetNode<Label>("Text");
            label.Text = string.Empty;
            label.LabelSettings.FontSize = _currentFontSize;
            entry.Visible = false;

            label.MouseEntered += () => { OnMouseEntered(label); };
            label.MouseExited += () => { OnMouseExited(); };

            Entry newEntry = new()
            {
                Root = entry,
                Label = label
            };

            _entriesList.Add(newEntry);
            _entriesQueue.Enqueue(newEntry);

            for (int i = 0; i < MaxEntries; i++)
            {
                MarginContainer duplicatedEntry = (MarginContainer)entry.Duplicate();
                Label duplicatedLabel = duplicatedEntry.GetNode<Label>("Text");
                duplicatedLabel.LabelSettings = (LabelSettings)duplicatedLabel.LabelSettings.DuplicateDeep();

                _entries.AddChild(duplicatedEntry);

                duplicatedLabel.MouseEntered += () => { OnMouseEntered(duplicatedLabel); };
                duplicatedLabel.MouseExited += () => { OnMouseExited(); };

                Entry duplicatedData = new()
                {
                    Root = duplicatedEntry,
                    Label = duplicatedLabel
                };

                _entriesList.Add(duplicatedData);
                _entriesQueue.Enqueue(duplicatedData);
            }

            RuntimeLogger.OnLog.Subscribe(OnLog, this);

            ToggleVisibility(false);
        }

        public override void Deinitialize()
        {
            RuntimeLogger.OnLog.Unsubscribe(this);
        }

        private void UpdateFont(int change)
        {
            _currentFontSize = Math.Clamp(_currentFontSize + change, MinFontSize, MaxFontSize);

            foreach (var entry in _entriesList)
            {
                entry.Label.LabelSettings.FontSize = _currentFontSize;
            }
        }

        private Entry FetchEntry()
        {
            Entry result;

            if (_usedEntries < MaxEntries)
            {
                result = _entriesList[_usedEntries];
                result.Root.Visible = true;
                _usedEntries++;
            }
            else
            {
                result = _entriesQueue.Dequeue();
                _entriesQueue.Enqueue(result);
                _entries.MoveChild(result.Root, -1);
            }

            return result;
        }

        private readonly Color white = new(1.0f, 1.0f, 1.0f, 1.0f);
        private readonly Color yellow = new(1.0f, 1.0f, 0.0f, 1.0f);
        private readonly Color red = new(1.0f, 0.0f, 0.0f, 1.0f);

        private readonly Color black = new(0.0f, 0.0f, 0.0f, 1.0f);
        private readonly Color deepBlue = new(0.0f, 0.0f, 0.5f, 1.0f);
        //private readonly Color  = new(1.0f, 1.0f, 1.0f, 1.0f);

        private bool _queueScroll = false;

        private bool OnLog(ref RuntimeLogger.LogEventData data)
        {
            Entry entry = FetchEntry();

            switch (data.Type)
            {
                default:
                case RuntimeLogger.LogType.Log:
                    {
                        entry.Label.LabelSettings.FontColor = white;
                        entry.Label.LabelSettings.OutlineColor = black;
                        break;
                    }
                case RuntimeLogger.LogType.Warning:
                    {
                        entry.Label.LabelSettings.FontColor = yellow;
                        entry.Label.LabelSettings.OutlineColor = black;
                        _warnVoice.Stop();
                        _warnVoice.Play();
                        break;
                    }
                case RuntimeLogger.LogType.Error:
                    {
                        entry.Label.LabelSettings.FontColor = red;
                        entry.Label.LabelSettings.OutlineColor = deepBlue;
                        _errorVoice.Stop();
                        _errorVoice.Play();
                        break;
                    }
            }

            if (string.IsNullOrEmpty(data.StackTrace))
            {
                entry.Label.TooltipText = string.Empty;
                entry.Label.MouseDefaultCursorShape = Control.CursorShape.Arrow;
            }
            else
            {
                entry.Label.TooltipText = data.StackTrace;
                entry.Label.MouseDefaultCursorShape = Control.CursorShape.Help;
            }

            entry.Label.Text = data.Message;

            if (!_queueScroll)
            {
                _queueScroll = true;
                _ = Scroll();
            }

            return true;
        }

        private async Task Scroll()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            VScrollBar vScrollBar = _scroll.GetVScrollBar();

            if (vScrollBar != null)
            {
                _scroll.ScrollVertical = (int)vScrollBar.MaxValue;
            }

            _queueScroll = false;
        }
    }
}
