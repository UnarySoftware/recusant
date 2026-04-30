using Godot;
using System;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UiCoreConsoleCounters : UiUnit<UiCoreState>
    {
        public bool Visible;

        private Color _visibleColor = new(1.0f, 1.0f, 1.0f, 1.0f);

        private int _logCounterValue = 0;
        private float _logValue = 0.0f;
        private Color _logColor = new(1.0f, 1.0f, 1.0f, 1.0f);

        [UiElement("%LogCounter")]
        private MarginContainer _logCounter;

        [UiElement("%LogCounterText")]
        private Label _logLabel;

        private int _warningCounterValue = 0;
        private float _warningValue = 0.0f;
        private Color _warningColor = new(1.0f, 1.0f, 1.0f, 1.0f);

        [UiElement("%WarningCounter")]
        private MarginContainer _warningCounter;

        [UiElement("%WarningCounterText")]
        private Label _warningLabel;

        private int _errorCounterValue = 0;
        private float _errorValue = 0.0f;
        private Color _errorColor = new(1.0f, 1.0f, 1.0f, 1.0f);

        [UiElement("%ErrorCounter")]
        private MarginContainer _errorCounter;

        [UiElement("%ErrorCounterText")]
        private Label _errorLabel;

        public override void Initialize()
        {
            _logLabel.Text = "0";
            _warningLabel.Text = "0";
            _errorLabel.Text = "0";

            RuntimeLogger.OnLog.Subscribe(OnLog, this);
        }

        public override void Deinitialize()
        {
            RuntimeLogger.OnLog.Unsubscribe(this);
        }

        private bool OnLog(ref RuntimeLogger.LogEventData data)
        {
            switch (data.Type)
            {
                case RuntimeLogger.LogType.Log:
                    {
                        _logValue = 4.0f;
                        _logCounterValue++;
                        _logLabel.Text = _logCounterValue.ToString();
                        break;
                    }
                case RuntimeLogger.LogType.Warning:
                    {
                        _warningValue = 4.0f;
                        _warningCounterValue++;
                        _warningLabel.Text = _warningCounterValue.ToString();
                        break;
                    }
                case RuntimeLogger.LogType.Error:
                    {
                        _errorValue = 4.0f;
                        _errorCounterValue++;
                        _errorLabel.Text = _errorCounterValue.ToString();
                        break;
                    }
            }

            return true;
        }

        public override void Process(float delta)
        {
            _logColor.A = Math.Clamp(_logValue, 0.0f, 1.0f);
            _warningColor.A = Math.Clamp(_warningValue, 0.0f, 1.0f);
            _errorColor.A = Math.Clamp(_errorValue, 0.0f, 1.0f);

            if (Visible)
            {
                _logCounter.Modulate = _visibleColor;
                _warningCounter.Modulate = _visibleColor;
                _errorCounter.Modulate = _visibleColor;
            }
            else
            {
                _logCounter.Modulate = _logColor;
                _warningCounter.Modulate = _warningColor;
                _errorCounter.Modulate = _errorColor;
            }

            _logValue -= delta;
            _warningValue -= delta;
            _errorValue -= delta;
        }
    }
}
