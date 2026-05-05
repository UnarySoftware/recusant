#if TOOLS

using Godot;
using Unary.Recusant.Editor;

namespace Unary.Recusant
{
    public static class Node3DExtensions
    {
        public static void UpdateGizmo(this Node3D node3d)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                node3d.UpdateGizmos();
            }
            else
            {
                if (node3d is IGizmo gizmo)
                {
                    gizmo.DrawGizmo();
                }
            }
        }

        public static void AddCollision(this Node3D node3d, TriangleMesh mesh)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                PluginGizmoDrawer.Singleton.AddCollision(mesh);
            }
            // This is an editor only feature
        }

        public static void DrawBegin(this Node3D node3d)
        {
            if (!Engine.Singleton.IsEditorHint())
            {
                RuntimeGizmos.Singleton.DrawBegin(node3d);
            }
            // This is a runtime only feature
        }

        public static void DrawEnd(this Node3D node3d)
        {
            if (!Engine.Singleton.IsEditorHint())
            {
                RuntimeGizmos.Singleton.DrawEnd(node3d);
            }
            // This is a runtime only feature
        }

        public static void DrawBoxWireframe(this Node3D node3d, Vector3 origin, Vector3 size, Color color)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                PluginGizmoDrawer.Singleton.DrawBoxWireframe(origin, size, color);
            }
            else
            {
                RuntimeGizmos.Singleton.GetGizmo(node3d).IDrawBoxWireframe(origin, size, color);
            }
        }

        public static void DrawBoxWireframe(this Node3D node3d, Aabb box, Color color)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                PluginGizmoDrawer.Singleton.DrawBoxWireframe(box, color);
            }
            else
            {
                RuntimeGizmos.Singleton.GetGizmo(node3d).IDrawBoxWireframe(box, color);
            }
        }

        public static void DrawBox(this Node3D node3d, Vector3 origin, Vector3 size, Color color, bool shaded, float textureScale = 20.0f)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                PluginGizmoDrawer.Singleton.DrawBox(origin, size, color, shaded, textureScale);
            }
            else
            {
                RuntimeGizmos.Singleton.GetGizmo(node3d).IDrawBox(origin, size, color, shaded, textureScale);
            }
        }

        public static void DrawBox(this Node3D node3d, Aabb box, Color color, bool shaded, float textureScale = 20.0f)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                PluginGizmoDrawer.Singleton.DrawBox(box, color, shaded, textureScale);
            }
            else
            {
                RuntimeGizmos.Singleton.GetGizmo(node3d).IDrawBox(box, color, shaded, textureScale);
            }
        }

        public static void DrawPath(this Node3D node3d, Vector3[] points, Color color)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                PluginGizmoDrawer.Singleton.DrawPath(points, color);
            }
            else
            {
                RuntimeGizmos.Singleton.GetGizmo(node3d).IDrawPath(points, color);
            }
        }

        public static void DrawMesh(this Node3D node3d, Vector3 origin, Mesh mesh, Color color, bool shaded, float textureScale = 20.0f)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                PluginGizmoDrawer.Singleton.DrawMesh(mesh, origin, color, shaded, textureScale);
            }
            else
            {
                RuntimeGizmos.Singleton.GetGizmo(node3d).IDrawMesh(mesh, origin, color, shaded, textureScale);
            }
        }

        public static void DrawArrow(this Node3D node3d, Vector3 origin, Vector3 end, Color color, bool shaded, float textureScale = 20.0f)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                PluginGizmoDrawer.Singleton.DrawArrow(origin, end, color, shaded, textureScale);
            }
            else
            {
                RuntimeGizmos.Singleton.GetGizmo(node3d).IDrawArrow(origin, end, color, shaded, textureScale);
            }
        }
    }
}

#endif
