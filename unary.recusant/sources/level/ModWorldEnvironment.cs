using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class ModWorldEnvironment : WorldEnvironment
    {
        [Export]
        bool UseVolumetricFog = false;

        public override void _Ready()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }
#endif

            if (Environment == null)
            {
                this.Error($"WorldEnvironment at path \"{GetPath()}\" is missing its Environment field");
                return;
            }

            GraphicsManager.Singleton.Ssr.OnChanged.Subscribe(UpdateScreenSpaceReflections, this);
            GraphicsManager.Singleton.Ssr.Republish();

            GraphicsManager.Singleton.Ao.OnChanged.Subscribe(UpdateScreenSpaceAmbientOcclusion, this);
            GraphicsManager.Singleton.Ao.Republish();

            GraphicsManager.Singleton.Ssl.OnChanged.Subscribe(UpdateScreenSpaceLighting, this);
            GraphicsManager.Singleton.Ssl.Republish();

            GraphicsManager.Singleton.GlobalIllumination.OnChanged.Subscribe(UpdateGlobalIllumination, this);
            GraphicsManager.Singleton.GlobalIllumination.Republish();

            GraphicsManager.Singleton.Bloom.OnChanged.Subscribe(UpdateBloom, this);
            GraphicsManager.Singleton.Bloom.Republish();

            GraphicsManager.Singleton.VolumetricFog.OnChanged.Subscribe(UpdateVolumetricFog, this);
            GraphicsManager.Singleton.VolumetricFog.Republish();

            GraphicsManager.Singleton.Brightness.OnChanged.Subscribe(UpdateBrightness, this);
            GraphicsManager.Singleton.Brightness.Republish();

            GraphicsManager.Singleton.Contrast.OnChanged.Subscribe(UpdateContrast, this);
            GraphicsManager.Singleton.Contrast.Republish();

            GraphicsManager.Singleton.Saturation.OnChanged.Subscribe(UpdateSaturation, this);
            GraphicsManager.Singleton.Saturation.Republish();
        }

        public override void _ExitTree()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }
#endif

            GraphicsManager.Singleton.Ssr.OnChanged.Unsubscribe(this);
            GraphicsManager.Singleton.Ao.OnChanged.Unsubscribe(this);
            GraphicsManager.Singleton.Ssl.OnChanged.Unsubscribe(this);
            GraphicsManager.Singleton.GlobalIllumination.OnChanged.Unsubscribe(this);
            GraphicsManager.Singleton.Bloom.OnChanged.Unsubscribe(this);
            GraphicsManager.Singleton.VolumetricFog.OnChanged.Unsubscribe(this);
            GraphicsManager.Singleton.Brightness.OnChanged.Unsubscribe(this);
            GraphicsManager.Singleton.Contrast.OnChanged.Unsubscribe(this);
            GraphicsManager.Singleton.Saturation.OnChanged.Unsubscribe(this);
        }

        private bool UpdateScreenSpaceReflections(ref int value)
        {
            if (value == 0)
            {
                Environment.SsrEnabled = false;
            }
            else if (value == 1)
            {
                Environment.SsrEnabled = true;
                Environment.SsrMaxSteps = 8;
            }
            else if (value == 2)
            {
                Environment.SsrEnabled = true;
                Environment.SsrMaxSteps = 32;
            }
            else if (value == 3)
            {
                Environment.SsrEnabled = true;
                Environment.SsrMaxSteps = 56;
            }

            return true;
        }

        private bool UpdateScreenSpaceAmbientOcclusion(ref int value)
        {
            if (value == 0)
            {
                Environment.SsaoEnabled = false;
            }
            else if (value == 1)
            {
                Environment.SsaoEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsaoQuality(RenderingServer.EnvironmentSsaoQuality.VeryLow, true, 0.5f, 2, 50.0f, 300.0f);
            }
            else if (value == 2)
            {
                Environment.SsaoEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsaoQuality(RenderingServer.EnvironmentSsaoQuality.Low, true, 0.5f, 2, 50.0f, 300.0f);
            }
            else if (value == 3)
            {
                Environment.SsaoEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsaoQuality(RenderingServer.EnvironmentSsaoQuality.Medium, true, 0.5f, 2, 50.0f, 300.0f);
            }
            else if (value == 4)
            {
                Environment.SsaoEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsaoQuality(RenderingServer.EnvironmentSsaoQuality.High, true, 0.5f, 2, 50.0f, 300.0f);
            }
            else if (value == 5)
            {
                Environment.SsaoEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsaoQuality(RenderingServer.EnvironmentSsaoQuality.Ultra, true, 0.5f, 2, 50.0f, 300.0f);
            }

            return true;
        }

        private bool UpdateScreenSpaceLighting(ref int value)
        {
            if (value == 0)
            {
                Environment.SsilEnabled = false;
            }
            else if (value == 1)
            {
                Environment.SsilEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsilQuality(RenderingServer.EnvironmentSsilQuality.VeryLow, true, 0.5f, 4, 50.0f, 300.0f);
            }
            else if (value == 2)
            {
                Environment.SsilEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsilQuality(RenderingServer.EnvironmentSsilQuality.Low, true, 0.5f, 4, 50.0f, 300.0f);
            }
            else if (value == 3)
            {
                Environment.SsilEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsilQuality(RenderingServer.EnvironmentSsilQuality.Medium, true, 0.5f, 4, 50.0f, 300.0f);
            }
            else if (value == 4)
            {
                Environment.SsilEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsilQuality(RenderingServer.EnvironmentSsilQuality.High, true, 0.5f, 4, 50.0f, 300.0f);
            }
            else if (value == 5)
            {
                Environment.SsilEnabled = true;
                RenderingServer.Singleton.EnvironmentSetSsilQuality(RenderingServer.EnvironmentSsilQuality.Ultra, true, 0.5f, 4, 50.0f, 300.0f);
            }
            return true;
        }

        private bool UpdateGlobalIllumination(ref int value)
        {
            if (value == 0)
            {
                Environment.SdfgiEnabled = false;
            }
            else if (value == 1)
            {
                Environment.SdfgiEnabled = true;
                RenderingServer.Singleton.GISetUseHalfResolution(true);
            }
            else if (value == 2)
            {
                Environment.SdfgiEnabled = true;
                RenderingServer.Singleton.GISetUseHalfResolution(false);
            }
            return true;
        }

        private bool UpdateBloom(ref int value)
        {
            if (value == 0)
            {
                Environment.GlowEnabled = false;
            }
            else if (value == 1)
            {
                Environment.GlowEnabled = true;
                RenderingServer.Singleton.EnvironmentGlowSetUseBicubicUpscale(false);
            }
            else if (value == 2)
            {
                Environment.GlowEnabled = true;
                RenderingServer.Singleton.EnvironmentGlowSetUseBicubicUpscale(true);
            }
            return true;
        }

        private bool UpdateVolumetricFog(ref int value)
        {
            if (!UseVolumetricFog || value == 0)
            {
                Environment.VolumetricFogEnabled = false;
            }
            else if (value == 1)
            {
                Environment.VolumetricFogEnabled = true;
                RenderingServer.Singleton.EnvironmentSetVolumetricFogFilterActive(false);
            }
            else if (value == 2)
            {
                Environment.VolumetricFogEnabled = true;
                RenderingServer.Singleton.EnvironmentSetVolumetricFogFilterActive(true);
            }
            return true;
        }

        private bool UpdateBrightness(ref float value)
        {
            Environment.AdjustmentEnabled = true;
            Environment.AdjustmentBrightness = value;
            return true;
        }

        private bool UpdateContrast(ref float value)
        {
            Environment.AdjustmentEnabled = true;
            Environment.AdjustmentContrast = value;
            return true;
        }

        private bool UpdateSaturation(ref float value)
        {
            Environment.AdjustmentEnabled = true;
            Environment.AdjustmentSaturation = value;
            return true;
        }
    }
}
