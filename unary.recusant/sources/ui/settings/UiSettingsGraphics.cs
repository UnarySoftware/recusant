using Godot;
using System;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiSettingsGraphics : UiSettingsTabBase
    {
        [UiElement("%RootTabs")]
        private TabBar _rootTabs;

        private static readonly LazyResource<ShaderMaterial> _blurMaterial = new("uid://cgxwenjv21f3h");

        [UiElement("%VeryLowPreset")]
        private Button _veryLowPreset;

        [UiElement("%LowPreset")]
        private Button _lowPreset;

        [UiElement("%MediumPreset")]
        private Button _mediumPreset;

        [UiElement("%HighPreset")]
        private Button _highPreset;

        [UiElement("%UltraPreset")]
        private Button _ultraPreset;

        [UiElement("%UIScaleOptions")]
        private OptionButton _uiScaleOptions;

        [UiElement("%ResolutionOptions")]
        private OptionButton _resolutionOptions;

        [UiElement("%RenderScaleSlider")]
        private HSlider _renderScaleSlider;

        [UiElement("%RenderScaleInput")]
        private LineEdit _renderScaleInput;

        [UiElement("%WindowScalingModeOptions")]
        private OptionButton _windowScalingModeOptions;

        [UiElement("%Fsr")]
        private HBoxContainer _fsrGroup;

        [UiElement("%FSRSharpnessSlider")]
        private HSlider _fsrSharpnessSlider;

        [UiElement("%FSRSharpnessInput")]
        private LineEdit _fsrSharpnessInput;

        [UiElement("%DisplayModeOptions")]
        private OptionButton _displayModeOptions;

        [UiElement("%VsyncOptions")]
        private OptionButton _vsyncOptions;

        [UiElement("%FpsLimit")]
        private HBoxContainer _fpsGroup;

        [UiElement("%FpsSlider")]
        private HSlider _fpsSlider;

        [UiElement("%FpsInput")]
        private LineEdit _fpsInput;

        [UiElement("%TaaOptions")]
        private OptionButton _taaOptions;

        [UiElement("%MsaaOptions")]
        private OptionButton _msaaOptions;

        [UiElement("%ScreenSpaceAAOptions")]
        private OptionButton _screenSpaceAAOptions;

        [UiElement("%ShadowSizeOptionButton")]
        private OptionButton _shadowSizeOptionButton;

        [UiElement("%ShadowFilterOptionButton")]
        private OptionButton _shadowFilterOptionButton;

        [UiElement("%ModelQualityOptions")]
        private OptionButton _modelQualityOptions;

        [UiElement("%GiOptions")]
        private OptionButton _giOptions;

        [UiElement("%BloomOptions")]
        private OptionButton _bloomOptions;

        [UiElement("%AoOptions")]
        private OptionButton _aoOptions;

        [UiElement("%SsrOptions")]
        private OptionButton _ssrOptions;

        [UiElement("%SslOptions")]
        private OptionButton _sslOptions;

        [UiElement("%VolumetricFogOptions")]
        private OptionButton _volumetricFogOptions;

        [UiElement("%BrightnessSlider")]
        private HSlider _brightnessSlider;

        [UiElement("%BrightnessInput")]
        private LineEdit _brightnessInput;

        [UiElement("%ContrastSlider")]
        private HSlider _contrastSlider;

        [UiElement("%ContrastInput")]
        private LineEdit _contrastInput;

        [UiElement("%SaturationSlider")]
        private HSlider _saturationSlider;

        [UiElement("%SaturationInput")]
        private LineEdit _saturationInput;

        private void InitOptions<[MustBeVariant] T>(OptionButton button, GraphicsSetting<T> setting, Func<T, string> labelFiller) where T : struct
        {
            int selection = 0;

            for (int i = 0; i < setting.Options.Count; i++)
            {
                T value = setting.Options[i];
                string label;

                if (EqualityComparer<T>.Default.Equals(value, setting.Value))
                {
                    selection = i;
                }

                if (labelFiller != null)
                {
                    label = labelFiller(value);
                }
                else
                {
                    label = setting.OptionsLabels[i];
                }

                button.AddItem(label);
            }

            button.Select(selection);

            button.ItemSelected += index =>
            {
                int intIndex = (int)index;
                setting.Value = setting.Options[intIndex];
            };
        }

        private void InitRange(HSlider slider, LineEdit lineEdit, GraphicsSetting<float> setting, float multiplier)
        {
            slider.MinValue = setting.Options[0];
            slider.MaxValue = setting.Options[1];
            slider.Value = setting.Value;

            slider.ValueChanged += (value) =>
            {
                setting.Value = (float)value;

                if (multiplier != 0.0f)
                {
                    value *= multiplier;
                }

                lineEdit.Text = value.ToString("0");
            };

            setting.OnChanged.Subscribe((ref data) =>
            {
                float value = data;

                slider.Value = value;

                if (multiplier != 0.0f)
                {
                    value *= multiplier;
                }

                lineEdit.Text = value.ToString("0");
                return true;
            }, this);

            float value = setting.Value;

            if (multiplier != 0.0f)
            {
                value *= multiplier;
            }

            lineEdit.Text = value.ToString("0");

            lineEdit.TextSubmitted += (value) =>
            {
                if (float.TryParse(value, out float result))
                {
                    if (multiplier != 0.0f)
                    {
                        setting.Value = result / multiplier;
                    }
                    else
                    {
                        setting.Value = result;
                    }
                }
                else
                {
                    float fallbackValue = setting.Value;

                    if (multiplier != 0.0f)
                    {
                        fallbackValue *= multiplier;
                    }

                    lineEdit.Text = fallbackValue.ToString("0");
                }
            };
        }

        private void InitRangeFPS(HSlider slider, LineEdit lineEdit, GraphicsSetting<int> setting)
        {
            const string infinity = "∞";

            slider.MinValue = setting.Options[0] + 10.0f;
            slider.MaxValue = setting.Options[1];
            slider.Value = setting.Value;

            slider.ValueChanged += (value) =>
            {
                float result = (float)value;

                if (result == slider.MaxValue)
                {
                    result = 0.0f;
                    lineEdit.Text = infinity;
                }
                else
                {
                    lineEdit.Text = value.ToString("0");
                }

                setting.Value = (int)result;
            };

            setting.OnChanged.Subscribe((ref data) =>
            {
                if (data == 0.0f)
                {
                    slider.Value = setting.Options[1];
                    lineEdit.Text = infinity;
                }
                else
                {
                    slider.Value = data;
                    lineEdit.Text = data.ToString("0");
                }

                return true;
            }, this);

            float value = setting.Value;

            if (value == 0.0f)
            {
                lineEdit.Text = infinity;
            }
            else
            {
                lineEdit.Text = value.ToString("0");
            }

            lineEdit.TextSubmitted += (value) =>
            {
                if (float.TryParse(value, out float result))
                {
                    setting.Value = (int)result;
                }
                else
                {
                    float fallbackValue = setting.Value;

                    if (fallbackValue == 0.0f)
                    {
                        lineEdit.Text = infinity;
                    }
                    else
                    {
                        lineEdit.Text = fallbackValue.ToString("0");
                    }
                }
            };
        }

        private void UpdatePresets()
        {
            _taaOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _taaOptions.Selected);
            _msaaOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _msaaOptions.Selected);
            _screenSpaceAAOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _screenSpaceAAOptions.Selected);
            _shadowSizeOptionButton.EmitSignal(OptionButton.SignalName.ItemSelected, _shadowSizeOptionButton.Selected);
            _shadowFilterOptionButton.EmitSignal(OptionButton.SignalName.ItemSelected, _shadowFilterOptionButton.Selected);
            _modelQualityOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _modelQualityOptions.Selected);
            _giOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _giOptions.Selected);
            _bloomOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _bloomOptions.Selected);
            _aoOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _aoOptions.Selected);
            _ssrOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _ssrOptions.Selected);
            _sslOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _sslOptions.Selected);
            _volumetricFogOptions.EmitSignal(OptionButton.SignalName.ItemSelected, _volumetricFogOptions.Selected);
        }

        private void OnVeryLowPreset()
        {
            _taaOptions.Select(0);
            _msaaOptions.Select(0);
            _screenSpaceAAOptions.Select(0);
            _shadowSizeOptionButton.Select(0);
            _shadowFilterOptionButton.Select(0);
            _modelQualityOptions.Select(0);
            _giOptions.Select(0);
            _bloomOptions.Select(0);
            _aoOptions.Select(0);
            _ssrOptions.Select(0);
            _sslOptions.Select(0);
            _volumetricFogOptions.Select(0);
            UpdatePresets();
        }

        private void OnLowPreset()
        {
            _taaOptions.Select(0);
            _msaaOptions.Select(0);
            _screenSpaceAAOptions.Select(1);
            _shadowSizeOptionButton.Select(1);
            _shadowFilterOptionButton.Select(1);
            _modelQualityOptions.Select(1);
            _giOptions.Select(0);
            _bloomOptions.Select(0);
            _aoOptions.Select(0);
            _ssrOptions.Select(0);
            _sslOptions.Select(0);
            _volumetricFogOptions.Select(0);
            UpdatePresets();
        }

        private void OnMediumPreset()
        {
            _taaOptions.Select(1);
            _msaaOptions.Select(0);
            _screenSpaceAAOptions.Select(2);
            _shadowSizeOptionButton.Select(2);
            _shadowFilterOptionButton.Select(2);
            _modelQualityOptions.Select(1);
            _giOptions.Select(1);
            _bloomOptions.Select(1);
            _aoOptions.Select(1);
            _ssrOptions.Select(1);
            _sslOptions.Select(0);
            _volumetricFogOptions.Select(1);
            UpdatePresets();
        }

        private void OnHighPreset()
        {
            _taaOptions.Select(1);
            _msaaOptions.Select(0);
            _screenSpaceAAOptions.Select(2);
            _shadowSizeOptionButton.Select(3);
            _shadowFilterOptionButton.Select(3);
            _modelQualityOptions.Select(2);
            _giOptions.Select(1);
            _bloomOptions.Select(2);
            _aoOptions.Select(2);
            _ssrOptions.Select(2);
            _sslOptions.Select(2);
            _volumetricFogOptions.Select(2);
            UpdatePresets();
        }

        private void OnUltraPreset()
        {
            _taaOptions.Select(1);
            _msaaOptions.Select(1);
            _screenSpaceAAOptions.Select(2);
            _shadowSizeOptionButton.Select(4);
            _shadowFilterOptionButton.Select(4);
            _modelQualityOptions.Select(3);
            _giOptions.Select(2);
            _bloomOptions.Select(2);
            _aoOptions.Select(3);
            _ssrOptions.Select(3);
            _sslOptions.Select(3);
            _volumetricFogOptions.Select(2);
            UpdatePresets();
        }

        public override void Initialize()
        {
            _veryLowPreset.Pressed += OnVeryLowPreset;
            _lowPreset.Pressed += OnLowPreset;
            _mediumPreset.Pressed += OnMediumPreset;
            _highPreset.Pressed += OnHighPreset;
            _ultraPreset.Pressed += OnUltraPreset;

            InitOptions(_uiScaleOptions, GraphicsManager.Singleton.UiScale, null);
            InitOptions(_resolutionOptions, GraphicsManager.Singleton.Resolution, (value) => { return $"{value.X}x{value.Y}"; });
            InitRange(_renderScaleSlider, _renderScaleInput, GraphicsManager.Singleton.RenderScale, 100.0f);
            InitOptions(_windowScalingModeOptions, GraphicsManager.Singleton.WindowScalingMode, null);

            GraphicsManager.Singleton.WindowScalingMode.OnChanged.Subscribe(OnWindowScaleChange, this);

            InitRange(_fsrSharpnessSlider, _fsrSharpnessInput, GraphicsManager.Singleton.FsrSharpness, 100.0f);

            InitOptions(_displayModeOptions, GraphicsManager.Singleton.DisplayMode, null);
            InitOptions(_vsyncOptions, GraphicsManager.Singleton.VSync, null);

            GraphicsManager.Singleton.VSync.OnChanged.Subscribe(OnVSyncChange, this);

            InitRangeFPS(_fpsSlider, _fpsInput, GraphicsManager.Singleton.FpsLimit);
            InitOptions(_taaOptions, GraphicsManager.Singleton.Taa, null);
            InitOptions(_msaaOptions, GraphicsManager.Singleton.Msaa, null);
            InitOptions(_screenSpaceAAOptions, GraphicsManager.Singleton.ScreenSpaceAa, null);
            InitOptions(_shadowSizeOptionButton, GraphicsManager.Singleton.ShadowResolution, null);
            InitOptions(_shadowFilterOptionButton, GraphicsManager.Singleton.ShadowFiltering, null);
            InitOptions(_modelQualityOptions, GraphicsManager.Singleton.ModelQuality, null);
            InitOptions(_giOptions, GraphicsManager.Singleton.GlobalIllumination, null);
            InitOptions(_bloomOptions, GraphicsManager.Singleton.Bloom, null);
            InitOptions(_aoOptions, GraphicsManager.Singleton.Ao, null);
            InitOptions(_ssrOptions, GraphicsManager.Singleton.Ssr, null);
            InitOptions(_sslOptions, GraphicsManager.Singleton.Ssl, null);
            InitOptions(_volumetricFogOptions, GraphicsManager.Singleton.VolumetricFog, null);
            InitRange(_brightnessSlider, _brightnessInput, GraphicsManager.Singleton.Brightness, 100.0f);
            InitRange(_contrastSlider, _contrastInput, GraphicsManager.Singleton.Contrast, 100.0f);
            InitRange(_saturationSlider, _saturationInput, GraphicsManager.Singleton.Saturation, 100.0f);
        }

        private bool OnWindowScaleChange(ref Viewport.Scaling3DModeEnum value)
        {
            if (value == Viewport.Scaling3DModeEnum.Bilinear)
            {
                _renderScaleSlider.MaxValue = GraphicsManager.Singleton.RenderScale.Options[1];
                _fsrGroup.Visible = false;
            }
            else if (value == Viewport.Scaling3DModeEnum.Fsr ||
                value == Viewport.Scaling3DModeEnum.Fsr2)
            {
                _renderScaleSlider.MaxValue = 1.0f;
                _fsrGroup.Visible = true;
            }

            return true;
        }

        private bool OnVSyncChange(ref DisplayServer.VSyncMode value)
        {
            if (value == DisplayServer.VSyncMode.Disabled)
            {
                _fpsGroup.Visible = true;
            }
            else
            {
                _fpsGroup.Visible = false;
            }

            return true;
        }

        public override void Deinitialize()
        {
            GraphicsManager.Singleton.WindowScalingMode.OnChanged.Unsubscribe(this);
            GraphicsManager.Singleton.VSync.OnChanged.Unsubscribe(this);

            _veryLowPreset.Pressed -= OnVeryLowPreset;
            _lowPreset.Pressed -= OnLowPreset;
            _mediumPreset.Pressed -= OnMediumPreset;
            _highPreset.Pressed -= OnHighPreset;
            _ultraPreset.Pressed -= OnUltraPreset;
        }

        private Color _backgroundColor;
        private static Color _empty = new(1.0f, 1.0f, 1.0f, 0.0f);

        public override void Open()
        {
            _backgroundColor = UiSettingsState.Singleton.BackgroundRect.Color;
            UiSettingsState.Singleton.BackgroundRect.Color = _empty;

            if (GameStateManager.Singleton.State == GameState.Game)
            {
                PostProcessManager.Singleton.ClearLayer(PlayerCamera3D.PostProcessSlot.GeneralBlur);
            }
        }

        public override void Close()
        {
            UiSettingsState.Singleton.BackgroundRect.Color = _backgroundColor;

            if (GameStateManager.Singleton.State == GameState.Game)
            {
                PostProcessManager.Singleton.SetLayer(PlayerCamera3D.PostProcessSlot.GeneralBlur, _blurMaterial.Cache);
            }
        }
    }
}
