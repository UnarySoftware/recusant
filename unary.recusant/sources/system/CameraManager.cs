using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class CameraManager : Node, IModSystem
    {
        public PlayerCamera3D Current { get; private set; }

        public EventAction OnChanged = new();

        private void MigratePostProcess(PlayerCamera3D original, PlayerCamera3D target)
        {
            if (!_playerCameras.TryGetValue(original, out var originalEntries))
            {
                this.Error($"Tried migrating a post process slot for an unregistered camera");
                return;
            }

            if (!_playerCameras.TryGetValue(target, out var targetEntries))
            {
                this.Error($"Tried migrating a post process slot for an unregistered camera");
                return;
            }

            foreach (var entry in targetEntries)
            {
                entry.Visible = false;
                entry.SetSurfaceOverrideMaterial(0, null);
            }

            for (int i = 0; i < originalEntries.Count; i++)
            {
                targetEntries[i].Visible = originalEntries[i].Visible;
                targetEntries[i].SetSurfaceOverrideMaterial(0, originalEntries[i].GetSurfaceOverrideMaterial(0));
            }

            foreach (var entry in originalEntries)
            {
                entry.Visible = false;
                entry.SetSurfaceOverrideMaterial(0, null);
            }
        }

        public void MakeCurrent(PlayerCamera3D camera, bool migratePostProcess)
        {
            if (Current != null)
            {
                Current.Current = false;
                if (migratePostProcess)
                {
                    MigratePostProcess(Current, camera);
                }
            }
            Current = camera;
            Current.Current = true;
            OnChanged.Publish();
        }

        bool ISystem.Initialize()
        {
            return true;
        }

        void ISystem.Deinitialize()
        {

        }

        private Dictionary<PlayerCamera3D, List<MeshInstance3D>> _playerCameras = [];

        public void Add(PlayerCamera3D camera)
        {
            List<MeshInstance3D> slots = [];

            int childCount = camera.GetChildCount();

            if (childCount > 1)
            {
                for (int i = 0; i < childCount; i++)
                {
                    MeshInstance3D instance = (MeshInstance3D)camera.GetChild(i);
                    slots.Add(instance);
                    instance.Visible = false;
                }
            }
            else
            {
                slots.Add(camera.PostProcessMesh);
                camera.PostProcessMesh.Name = PlayerCamera3D.PostProcessSlot.GeneralBlur.ToString();

                while (childCount < (int)PlayerCamera3D.PostProcessSlot.Max)
                {
                    PlayerCamera3D.PostProcessSlot slotNamed = (PlayerCamera3D.PostProcessSlot)childCount;

                    MeshInstance3D newInstance = new()
                    {
                        Mesh = camera.PostProcessMesh.Mesh,
                        Rotation = camera.PostProcessMesh.Rotation,
                        Name = slotNamed.ToString()
                    };

                    Vector3 position = camera.PostProcessMesh.Position;
                    position.Z += camera.Offset;
                    newInstance.Position = position;

                    camera.AddChild(newInstance);
                    slots.Add(newInstance);
                    camera.Offset -= 0.001f;
                    childCount++;
                }

                foreach (var entry in slots)
                {
                    entry.Visible = false;
                }
            }

            _playerCameras.Add(camera, slots);
        }

        public void Remove(PlayerCamera3D camera)
        {
            _playerCameras.Remove(camera);
        }

        public MeshInstance3D GetPostProcessSlot(PlayerCamera3D camera, PlayerCamera3D.PostProcessSlot targetSlot)
        {
            int index = (int)targetSlot;

            if (!_playerCameras.TryGetValue(camera, out var entries))
            {
                this.Error($"Tried getting a post process slot for an unregistered camera");
                return null;
            }

            if (index < 0 || index >= (int)PlayerCamera3D.PostProcessSlot.Max)
            {
                this.Error($"Tried getting a post process slot within an invalid range of {targetSlot}");
                return null;
            }

            return entries[index];
        }
    }
}
