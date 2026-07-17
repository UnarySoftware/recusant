using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class CursorManager : Node, IModSystem
    {
        private List<CursorPackDefinition> _cursorPacks = [];

        private void TrySetType(LazyResource entry, Input.CursorShape shape, Vector2 size, bool centered)
        {
            if (!BaseSelectorResource.IsValid(entry))
            {
                return;
            }

            Texture2D targetTexture = entry.Load<Texture2D>();

            if (targetTexture == null)
            {
                return;
            }

            if (centered)
            {
                size /= 2.0f;
                Input.Singleton.SetCustomMouseCursor(targetTexture, shape, size);
            }
            else
            {
                Input.Singleton.SetCustomMouseCursor(targetTexture, shape);
            }
        }

        bool ISystem.PostInitialize()
        {
            _cursorPacks = ResourceTypesManager.Singleton.LoadResources<CursorPackDefinition>();

            if (_cursorPacks.Count == 0)
            {
                return false;
            }

            CursorPackDefinition selected = _cursorPacks[0];

            Vector2 size = new(selected.TextureSize, selected.TextureSize);

            TrySetType(selected.ArrowTexture, Input.CursorShape.Arrow, size, false);
            TrySetType(selected.IBeamTexture, Input.CursorShape.Ibeam, size, true);
            TrySetType(selected.PointingHandTexture, Input.CursorShape.PointingHand, size, false);
            TrySetType(selected.CrossTexture, Input.CursorShape.Cross, size, true);
            TrySetType(selected.WaitTexture, Input.CursorShape.Wait, size, true);
            TrySetType(selected.BusyTexture, Input.CursorShape.Busy, size, true);
            TrySetType(selected.DragTexture, Input.CursorShape.Drag, size, true);
            TrySetType(selected.CanDropTexture, Input.CursorShape.CanDrop, size, true);
            TrySetType(selected.ForbiddenTexture, Input.CursorShape.Forbidden, size, true);
            TrySetType(selected.VSizeTexture, Input.CursorShape.Vsplit, size, true);
            TrySetType(selected.HSizeTexture, Input.CursorShape.Hsize, size, true);
            TrySetType(selected.BDiagSizeTexture, Input.CursorShape.Bdiagsize, size, true);
            TrySetType(selected.FDiagSizeTexture, Input.CursorShape.Fdiagsize, size, true);
            TrySetType(selected.DragTexture, Input.CursorShape.Move, size, true);
            TrySetType(selected.VSizeTexture, Input.CursorShape.Vsplit, size, true);
            TrySetType(selected.HSizeTexture, Input.CursorShape.Hsplit, size, true);
            TrySetType(selected.HelpTexture, Input.CursorShape.Help, size, true);

            return true;
        }

        void ISystem.Deinitialize()
        {

        }
    }
}
