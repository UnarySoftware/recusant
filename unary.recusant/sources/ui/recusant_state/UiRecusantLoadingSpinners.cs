using System;
using System.Text;
using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiRecusantLoadingSpinners : UiUnit<UiRecusantState>
    {
        private struct IntPool
        {
            public int Count;
            public int Min;
            public int Max;
        }

        private readonly IntPool[] _pool =
        [
            new() { Count = 4, Min = 1111, Max = 9999 },
            new() { Count = 4, Min = 11111, Max = 99999 },
            new() { Count = 4, Min = 111111, Max = 999999 },
            new() { Count = 4, Min = 1111111, Max = 9999999 },
        ];

        private int[] _pooledInts;

        private readonly StringBuilder _builder = new();

        private string BuildLabel()
        {
            _builder.Append(_pooledInts[GD.RandRange(0, _pooledInts.Length - 1)]).Append('\n')
                .Append(_pooledInts[GD.RandRange(0, _pooledInts.Length - 1)]);
            string result = _builder.ToString();
            _builder.Clear();
            return result;
        }

        public float ForegroundSpinSpeed = 100.0f;
        public float BackgroundSpinSpeed = 50.0f;
        public float FadeoutTimer = 3.0f;
        public float LabelChangeTimer = 0.2f;

        private float _labelTimer = 0.0f;

        private Color _loadingColor = new(1.0f, 1.0f, 1.0f, 0.0f);
        private float _loadingTimer = 0.0f;

        private Color _savingColor = new(1.0f, 1.0f, 1.0f, 0.0f);
        private float _savingTimer = 0.0f;

        [UiElement("%Loading")]
        private Control _loading;

        [UiElement("%LoadingForeground")]
        private TextureRect _loadingForeground;

        [UiElement("%LoadingBackground")]
        private TextureRect _loadingBackground;

        [UiElement("%LoadingLabel")]
        private Label _loadingLabel;

        [UiElement("%Saving")]
        private Control _saving;

        [UiElement("%SavingForeground")]
        private TextureRect _savingForeground;

        [UiElement("%SavingBackground")]
        private TextureRect _savingBackground;

        [UiElement("%SavingLabel")]
        private Label _savingLabel;

        public override void Initialize()
        {
            int finalCount = 0;

            foreach (var entry in _pool)
            {
                finalCount += entry.Count;
            }

            _pooledInts = new int[finalCount];

            int index = 0;

            foreach (var entry in _pool)
            {
                for (int i = 0; i < entry.Count; i++)
                {
                    _pooledInts[index] = GD.RandRange(entry.Min, entry.Max);
                    index++;
                }
            }

            // TODO Attribute based assignment resolution
            _saving.Visible = false;
        }

        public override void Process(float delta)
        {
            if (LoadingManager.Singleton.IsLoading)
            {
                _loadingTimer = FadeoutTimer;
            }
            else
            {
                _loadingTimer -= delta;
            }

            _loadingTimer = Mathf.Clamp(_loadingTimer, 0.0f, FadeoutTimer);

            _loadingColor.A = Mathf.Clamp(_loadingTimer, 0.0f, 1.0f);

            _loading.Modulate = _loadingColor;

            if (_loadingTimer > 0.0f)
            {
                _labelTimer += delta;

                if (_labelTimer >= LabelChangeTimer)
                {
                    _labelTimer = 0.0f;
                    _loadingLabel.Text = BuildLabel();
                }

                _loadingForeground.RotationDegrees += delta * ForegroundSpinSpeed;

                if (_loadingForeground.RotationDegrees >= 360.0f)
                {
                    _loadingForeground.RotationDegrees = -360.0f;
                }

                _loadingBackground.RotationDegrees -= delta * BackgroundSpinSpeed;

                if (_loadingBackground.RotationDegrees <= -360.0f)
                {
                    _loadingBackground.RotationDegrees = 360.0f;
                }
            }
        }
    }
}
