using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class GraphicsManager : Node, IModSystem
    {
        private struct GraphicsManagerData
        {
            // Ui Settings
            public float UiScale { get; set; }
            // Video Settings
            public Vector2I Resolution { get; set; }
            public float RenderScale { get; set; }
            public Viewport.Scaling3DModeEnum WindowScalingMode { get; set; }
            public float FsrSharpness { get; set; }
            public Window.ModeEnum DisplayMode { get; set; }
            public DisplayServer.VSyncMode VSync { get; set; }
            public int FpsLimit { get; set; }
            public bool Taa { get; set; }
            public Viewport.Msaa Msaa { get; set; }
            public Viewport.ScreenSpaceAAEnum ScreenSpaceAa { get; set; }
            // Quality Settings
            public int ShadowResolution { get; set; }
            public int ShadowFiltering { get; set; }
            public int ModelQuality { get; set; }
            // Effects Settings
            public int GlobalIllumination { get; set; }
            public int Bloom { get; set; }
            public int Ao { get; set; }
            public int Ssr { get; set; }
            public int Ssl { get; set; }
            public int VolumetricFog { get; set; }
            // Adjustments
            public float Brightness { get; set; }
            public float Contrast { get; set; }
            public float Saturation { get; set; }
        }

        public static T Default<T>(T input, List<T> options, T defaultValue) where T : struct
        {
            if (!options.Contains(input))
            {
                return defaultValue;
            }
            return input;
        }

        public static T NoValidation<T>(T input, List<T> options, T defaultValue) where T : struct
        {
            return input;
        }

        public static T Clamp<T>(T input, List<T> options, T defaultValue) where T : struct, IComparable<T>
        {
            if (options.Count != 2)
            {
                return defaultValue;
            }

            if (input.CompareTo(options[0]) < 0)
            {
                return options[0];
            }

            if (input.CompareTo(options[1]) > 0)
            {
                return options[1];
            }

            return input;
        }

        public GraphicsSetting<float> UiScale = new()
        {
            DefaultValue = 1.0f,
            Options = [1.5f, 1.25f, 1.0f, 0.75f, 0.5f],
            OptionsLabels = ["Smaller (66%)", "Small (80%)", "Medium (100%) (default)", "Large (133%)", "Larger (200%)"],
            Validator = NoValidation
        };

        public GraphicsSetting<Vector2I> Resolution = new()
        {
            DefaultCalculator = (options) =>
            {
                Vector2I size = DisplayServer.Singleton.ScreenGetSize();
                Vector2I selected = new();

                for (int i = 0; i < options.Count; i++)
                {
                    Vector2I current = options[i];

                    if (current.X >= size.X)
                    {
                        break;
                    }

                    selected = current;
                }

                return selected;
            },
            Options = [new(800, 1280), new(1280, 720), new(1280, 1024), new(1280, 800), new(1360, 768),
                new(1366, 768), new(1440, 900), new(1470, 956), new(1512, 982), new(1600, 900), new(1680, 1050),
                new(1920, 1080), new(1920, 1200), new(2560, 1600), new(2560, 1080), new(2560, 1440), new(2880, 1800),
                new(3440, 1440), new(3840, 2160), new(5120, 1440)],
            Validator = NoValidation
        };

        public GraphicsSetting<float> RenderScale = new()
        {
            DefaultValue = 1.0f,
            Options = [0.1f, 2.0f],
            Validator = Clamp
        };

        public GraphicsSetting<Viewport.Scaling3DModeEnum> WindowScalingMode = new()
        {
            DefaultValue = Viewport.Scaling3DModeEnum.Bilinear,
            Options = [Viewport.Scaling3DModeEnum.Bilinear, Viewport.Scaling3DModeEnum.Fsr, Viewport.Scaling3DModeEnum.Fsr2],
            OptionsLabels = ["Native", "FSR1", "FSR2"],
            Validator = Default
        };

        public GraphicsSetting<float> FsrSharpness = new()
        {
            DefaultValue = 0.2f,
            Options = [0.0f, 2.0f],
            Validator = Clamp
        };

        public GraphicsSetting<Window.ModeEnum> DisplayMode = new()
        {
            DefaultValue = Window.ModeEnum.Windowed,
            Options = [Window.ModeEnum.Windowed, Window.ModeEnum.Fullscreen, Window.ModeEnum.ExclusiveFullscreen],
            OptionsLabels = ["Windowed", "Borderless", "Fullscreen"],
            Validator = Default
        };

        public GraphicsSetting<DisplayServer.VSyncMode> VSync = new()
        {
            DefaultValue = DisplayServer.VSyncMode.Disabled,
            Options = [DisplayServer.VSyncMode.Disabled, DisplayServer.VSyncMode.Adaptive, DisplayServer.VSyncMode.Enabled],
            OptionsLabels = ["Disabled", "Adaptive", "Enabled"],
            Validator = Default
        };

        public GraphicsSetting<int> FpsLimit = new()
        {
            DefaultCalculator = (options) =>
            {
                float target = (float)DisplayServer.Singleton.ScreenGetRefreshRate();
                return (int)Mathf.Clamp(target, options[0], options[1]);
            },
            Options = [0, 1000],
            Validator = Clamp
        };

        public GraphicsSetting<bool> Taa = new()
        {
            DefaultValue = true,
            Options = [false, true],
            OptionsLabels = ["Disabled", "Enabled"],
            Validator = Default
        };

        public GraphicsSetting<Viewport.Msaa> Msaa = new()
        {
            DefaultValue = Viewport.Msaa.Disabled,
            Options = [Viewport.Msaa.Disabled, Viewport.Msaa.Msaa2X, Viewport.Msaa.Msaa4X, Viewport.Msaa.Msaa8X],
            OptionsLabels = ["Disabled", "x2", "x4", "x8"],
            Validator = Default
        };

        public GraphicsSetting<Viewport.ScreenSpaceAAEnum> ScreenSpaceAa = new()
        {
            DefaultValue = Viewport.ScreenSpaceAAEnum.Smaa,
            Options = [Viewport.ScreenSpaceAAEnum.Disabled, Viewport.ScreenSpaceAAEnum.Fxaa, Viewport.ScreenSpaceAAEnum.Smaa],
            OptionsLabels = ["Disabled", "FXAA", "SMAA"],
            Validator = Default
        };

        public GraphicsSetting<int> ShadowResolution = new()
        {
            DefaultValue = 2,
            Options = [0, 1, 2, 3, 4, 5],
            OptionsLabels = ["Very Low", "Low", "Medium", "High", "Very High", "Ultra"],
            Validator = Default
        };

        public GraphicsSetting<int> ShadowFiltering = new()
        {
            DefaultValue = 2,
            Options = [0, 1, 2, 3, 4, 5],
            OptionsLabels = ["Very Low", "Low", "Medium", "High", "Very High", "Ultra"],
            Validator = Default
        };

        public GraphicsSetting<int> ModelQuality = new()
        {
            DefaultValue = 1,
            Options = [0, 1, 2, 3, 4],
            OptionsLabels = ["Very Low", "Low", "Medium", "High", "Very High"],
            Validator = Default
        };

        public GraphicsSetting<int> GlobalIllumination = new()
        {
            DefaultValue = 1,
            Options = [0, 1, 2],
            OptionsLabels = ["Disabled", "Low", "High"],
            Validator = Default
        };

        public GraphicsSetting<int> Bloom = new()
        {
            DefaultValue = 1,
            Options = [0, 1, 2],
            OptionsLabels = ["Disabled", "Low", "High"],
            Validator = Default
        };

        public GraphicsSetting<int> Ao = new()
        {
            DefaultValue = 1,
            Options = [0, 1, 2, 3, 4, 5],
            OptionsLabels = ["Disabled", "Very Low", "Low", "Medium", "High", "Ultra"],
            Validator = Default
        };

        public GraphicsSetting<int> Ssr = new()
        {
            DefaultValue = 1,
            Options = [0, 1, 2, 3],
            OptionsLabels = ["Disabled", "Low", "Medium", "High"],
            Validator = Default
        };

        public GraphicsSetting<int> Ssl = new()
        {
            DefaultValue = 0,
            Options = [0, 1, 2, 3, 4, 5],
            OptionsLabels = ["Disabled", "Very Low", "Low", "Medium", "High", "Ultra"],
            Validator = Default
        };

        public GraphicsSetting<int> VolumetricFog = new()
        {
            DefaultValue = 1,
            Options = [0, 1, 2],
            OptionsLabels = ["Disabled", "Low", "High"],
            Validator = Default
        };

        public GraphicsSetting<float> Brightness = new()
        {
            DefaultValue = 1.0f,
            Options = [0.5f, 4.0f],
            Validator = Clamp
        };

        public GraphicsSetting<float> Contrast = new()
        {
            DefaultValue = 1.0f,
            Options = [0.5f, 4.0f],
            Validator = Clamp
        };

        public GraphicsSetting<float> Saturation = new()
        {
            DefaultValue = 1.0f,
            Options = [0.5f, 10.0f],
            Validator = Clamp
        };

        private GraphicsManagerData Serialize()
        {
            return new()
            {
                UiScale = UiScale.Value,
                Resolution = Resolution.Value,
                RenderScale = RenderScale.Value,
                WindowScalingMode = WindowScalingMode.Value,
                FsrSharpness = FsrSharpness.Value,
                DisplayMode = DisplayMode.Value,
                VSync = VSync.Value,
                FpsLimit = FpsLimit.Value,
                Taa = Taa.Value,
                Msaa = Msaa.Value,
                ScreenSpaceAa = ScreenSpaceAa.Value,
                ShadowResolution = ShadowResolution.Value,
                ShadowFiltering = ShadowFiltering.Value,
                ModelQuality = ModelQuality.Value,
                GlobalIllumination = GlobalIllumination.Value,
                Bloom = Bloom.Value,
                Ao = Ao.Value,
                Ssr = Ssr.Value,
                Ssl = Ssl.Value,
                VolumetricFog = VolumetricFog.Value,
                Brightness = Brightness.Value,
                Contrast = Contrast.Value,
                Saturation = Saturation.Value,
            };
        }

        private void Deserialize(GraphicsManagerData data)
        {
            UiScale.Value = data.UiScale;
            Resolution.Value = data.Resolution;
            RenderScale.Value = data.RenderScale;
            WindowScalingMode.Value = data.WindowScalingMode;
            FsrSharpness.Value = data.FsrSharpness;
            DisplayMode.Value = data.DisplayMode;
            VSync.Value = data.VSync;
            FpsLimit.Value = data.FpsLimit;
            Taa.Value = data.Taa;
            Msaa.Value = data.Msaa;
            ScreenSpaceAa.Value = data.ScreenSpaceAa;
            ShadowResolution.Value = data.ShadowResolution;
            ShadowFiltering.Value = data.ShadowFiltering;
            ModelQuality.Value = data.ModelQuality;
            GlobalIllumination.Value = data.GlobalIllumination;
            Bloom.Value = data.Bloom;
            Ao.Value = data.Ao;
            Ssr.Value = data.Ssr;
            Ssl.Value = data.Ssl;
            VolumetricFog.Value = data.VolumetricFog;
            Brightness.Value = data.Brightness;
            Contrast.Value = data.Contrast;
            Saturation.Value = data.Saturation;
        }

        bool ISystem.Initialize()
        {
            UiScale.OnChanged.Subscribe(UpdateUiScale, this);
            Resolution.OnChanged.Subscribe(UpdateResolution, this);
            RenderScale.OnChanged.Subscribe(UpdateResolutionScale, this);
            WindowScalingMode.OnChanged.Subscribe(UpdateViewportScale, this);
            FsrSharpness.OnChanged.Subscribe(UpdateFsrSharpness, this);
            DisplayMode.OnChanged.Subscribe(UpdateWindowMode, this);
            VSync.OnChanged.Subscribe(UpdateVSync, this);
            FpsLimit.OnChanged.Subscribe(UpdateFpsLimit, this);
            Taa.OnChanged.Subscribe(UpdateTaa, this);
            Msaa.OnChanged.Subscribe(UpdateMsaa, this);
            ScreenSpaceAa.OnChanged.Subscribe(UpdateScreenSpaceAa, this);
            ShadowFiltering.OnChanged.Subscribe(UpdateShadowFiltering, this);
            ModelQuality.OnChanged.Subscribe(UpdateModelQuality, this);

            if (!TryLoad())
            {
                Save();
                NotificationManager.Singleton.SendNotification("Alert!",
                    "It seems that this is your first time running the game on this hardware." +
                    "Graphical settings were reset to defaults.");
            }

            return true;
        }

        void ISystem.Deinitialize()
        {
            Save();

            UiScale.OnChanged.Unsubscribe(this);
            Resolution.OnChanged.Unsubscribe(this);
            RenderScale.OnChanged.Unsubscribe(this);
            WindowScalingMode.OnChanged.Unsubscribe(this);
            FsrSharpness.OnChanged.Unsubscribe(this);
            DisplayMode.OnChanged.Unsubscribe(this);
            VSync.OnChanged.Unsubscribe(this);
            FpsLimit.OnChanged.Unsubscribe(this);
            Taa.OnChanged.Unsubscribe(this);
            Msaa.OnChanged.Unsubscribe(this);
            ScreenSpaceAa.OnChanged.Unsubscribe(this);
            ShadowFiltering.OnChanged.Unsubscribe(this);
            ModelQuality.OnChanged.Unsubscribe(this);
        }

        private bool UpdateUiScale(ref float value)
        {
            Vector2 resolutionFloat = DisplayServer.Singleton.WindowGetSize();

            resolutionFloat *= value;

            GetTree().Root.ContentScaleSize = new((int)resolutionFloat.X, (int)resolutionFloat.Y);

            return true;
        }

        private bool UpdateResolution(ref Vector2I value)
        {
            GetWindow().Size = value;
            UiScale.Republish();
            return true;
        }

        private bool UpdateResolutionScale(ref float value)
        {
            GetViewport().Scaling3DScale = value;
            return true;
        }

        private bool UpdateViewportScale(ref Viewport.Scaling3DModeEnum value)
        {
            if (value == Viewport.Scaling3DModeEnum.Fsr ||
                value == Viewport.Scaling3DModeEnum.Fsr2)
            {
                RenderScale.Value = Mathf.Clamp(RenderScale.Value, RenderScale.Options[0], 1.0f);
            }

            GetViewport().Scaling3DMode = value;
            return true;
        }

        private bool UpdateFsrSharpness(ref float value)
        {
            GetViewport().FsrSharpness = 2.0f - value;
            return true;
        }

        private bool UpdateWindowMode(ref Window.ModeEnum value)
        {
            GetTree().Root.Mode = value;
            return true;
        }

        private bool UpdateVSync(ref DisplayServer.VSyncMode value)
        {
            DisplayServer.Singleton.WindowSetVsyncMode(value);

            if (value != DisplayServer.VSyncMode.Disabled)
            {
                FpsLimit.Value = 0;
            }

            return true;
        }

        private bool UpdateFpsLimit(ref int value)
        {
            Engine.Singleton.MaxFps = value;
            return true;
        }

        private bool UpdateTaa(ref bool value)
        {
            GetViewport().UseTaa = value;
            return true;
        }

        private bool UpdateMsaa(ref Viewport.Msaa value)
        {
            GetViewport().Msaa3D = value;
            return true;
        }

        private bool UpdateScreenSpaceAa(ref Viewport.ScreenSpaceAAEnum value)
        {
            GetViewport().ScreenSpaceAA = value;
            return true;
        }

        private bool UpdateShadowFiltering(ref int value)
        {
            if (value == 0)
            {
                RenderingServer.Singleton.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.Hard);
                RenderingServer.Singleton.PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.Hard);
            }
            else if (value == 1)
            {
                RenderingServer.Singleton.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftVeryLow);
                RenderingServer.Singleton.PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftVeryLow);
            }
            else if (value == 2)
            {
                RenderingServer.Singleton.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftLow);
                RenderingServer.Singleton.PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftLow);
            }
            else if (value == 3)
            {
                RenderingServer.Singleton.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftMedium);
                RenderingServer.Singleton.PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftMedium);
            }
            else if (value == 4)
            {
                RenderingServer.Singleton.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftHigh);
                RenderingServer.Singleton.PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftHigh);
            }
            else if (value == 5)
            {
                RenderingServer.Singleton.DirectionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftUltra);
                RenderingServer.Singleton.PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftUltra);
            }
            return true;
        }

        private bool UpdateModelQuality(ref int value)
        {
            if (value == 0)
            {
                GetViewport().MeshLodThreshold = 8.0f;
            }
            else if (value == 1)
            {
                GetViewport().MeshLodThreshold = 4.0f;
            }
            else if (value == 2)
            {
                GetViewport().MeshLodThreshold = 2.0f;
            }
            else if (value == 3)
            {
                GetViewport().MeshLodThreshold = 1.0f;
            }
            else if (value == 4)
            {
                GetViewport().MeshLodThreshold = 0.0f;
            }
            return true;
        }

        void Save()
        {
            GraphicsManagerData data = Serialize();

            StorageManager.Singleton.WriteEntryText(this.GetModId(), nameof(GraphicsManager), StorageManager.Singleton.FingerprintString, JsonSerializer.Serialize(data, JsonConverters.IndentedOptions));
        }

        bool TryLoad()
        {
            List<string> entries = StorageManager.Singleton.GetEntries(this.GetModId(), nameof(GraphicsManager));

            if (entries.Count == 0)
            {
                Save();
                return true;
            }

            string entryText = string.Empty;

            foreach (var entry in entries)
            {
                if (entry == StorageManager.Singleton.FingerprintString)
                {
                    entryText = StorageManager.Singleton.ReadEntryText(this.GetModId(), nameof(GraphicsManager), entry);
                    break;
                }
            }

            if (entryText != string.Empty)
            {
                GraphicsManagerData data = JsonSerializer.Deserialize<GraphicsManagerData>(entryText, JsonConverters.IndentedOptions);

                Deserialize(data);

                return true;
            }

            return false;
        }
    }
}
