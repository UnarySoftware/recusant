using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class PostProcessManager : Node, IModSystem
    {
        bool ISystem.Initialize()
        {
            return true;
        }

        void ISystem.Deinitialize()
        {

        }

        public void SetLayer(PlayerCamera3D.PostProcessSlot slot, Material material)
        {
            MeshInstance3D mesh = CameraManager.Singleton.GetPostProcessSlot(CameraManager.Singleton.Current, slot);
            mesh.Visible = true;
            mesh.SetSurfaceOverrideMaterial(0, material);
        }

        public void ClearLayer(PlayerCamera3D.PostProcessSlot slot)
        {
            MeshInstance3D mesh = CameraManager.Singleton.GetPostProcessSlot(CameraManager.Singleton.Current, slot);
            mesh.Visible = false;
            mesh.SetSurfaceOverrideMaterial(0, null);
        }
    }
}
