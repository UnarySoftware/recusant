using Godot;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class ModDirectionalLight3D : DirectionalLight3D
    {
        public override void _Ready()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }
#endif

            GraphicsManager.Singleton.ShadowResolution.OnChanged.Subscribe(UpdateShadowResolution, this);
            GraphicsManager.Singleton.ShadowResolution.Republish();
        }

        public override void _ExitTree()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }
#endif

            GraphicsManager.Singleton.ShadowResolution.OnChanged.Unsubscribe(this);
        }

        private bool UpdateShadowResolution(ref int value)
        {
            if (value == 0)
            {
                ShadowEnabled = false;
            }
            else if (value == 1)
            {
                ShadowEnabled = true;
                RenderingServer.Singleton.DirectionalShadowAtlasSetSize(1024, true);
                ShadowBias = 0.04f;
                GetViewport().PositionalShadowAtlasSize = 1024;
            }
            else if (value == 2)
            {
                ShadowEnabled = true;
                RenderingServer.Singleton.DirectionalShadowAtlasSetSize(2048, true);
                ShadowBias = 0.03f;
                GetViewport().PositionalShadowAtlasSize = 2048;
            }
            else if (value == 3)
            {
                ShadowEnabled = true;
                RenderingServer.Singleton.DirectionalShadowAtlasSetSize(4096, true);
                ShadowBias = 0.02f;
                GetViewport().PositionalShadowAtlasSize = 4096;
            }
            else if (value == 4)
            {
                ShadowEnabled = true;
                RenderingServer.Singleton.DirectionalShadowAtlasSetSize(8192, true);
                ShadowBias = 0.01f;
                GetViewport().PositionalShadowAtlasSize = 8192;
            }
            else if (value == 5)
            {
                ShadowEnabled = true;
                RenderingServer.Singleton.DirectionalShadowAtlasSetSize(16384, true);
                ShadowBias = 0.005f;
                GetViewport().PositionalShadowAtlasSize = 16384;
            }
            return true;
        }
    }
}
