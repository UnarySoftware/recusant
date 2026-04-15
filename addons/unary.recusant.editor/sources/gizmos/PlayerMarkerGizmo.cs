using Godot;
using Unary.Core;
using Unary.Core.Editor;

namespace Unary.Recusant.Editor
{
    [Tool]
    [GlobalClass]
    public partial class PlayerMarkerGizmo : EditorNode3DGizmoPlugin, IPluginSystem
    {
        bool ISystem.Initialize()
        {
            PluginBootstrap.Singleton.AddNode3DGizmoPlugin(this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            PluginBootstrap.Singleton.RemoveNode3DGizmoPlugin(this);
        }

        public override string _GetGizmoName()
        {
            return nameof(PlayerMarkerGizmo);
        }

        public override bool _HasGizmo(Node3D node)
        {
            return node is PlayerMarker;
        }

        private const string material = "material";
        private const string arrow = "arrow";
        private const string arrowAdditional = "arrowAdditional";
        private static CylinderMesh mesh;
        private static TriangleMesh triangleMesh;
        private static Vector3[] arrowLinesMain;
        private static Vector3[] arrowLinesAdditional;
        private static Transform3D transform;

        private static Color red = new(1.0f, 0.0f, 0.0f, 1.0f);
        private static Color green = new(0.0f, 1.0f, 0.0f, 1.0f);
        private static Color arrowColorMain = new(1.0f, 1.0f, 0.0f, 1.0f);
        private static Color arrowColorAdditional = new(1.0f, 0.0f, 1.0f, 1.0f);

        public PlayerMarkerGizmo()
        {
            CreateMaterial(material, new Color());
            CreateMaterial(arrow, arrowColorMain);
            CreateMaterial(arrowAdditional, arrowColorAdditional);

            mesh ??= new()
            {
                TopRadius = PlayerConstants.PlayerRadius,
                BottomRadius = PlayerConstants.PlayerRadius,
                Height = PlayerConstants.PlayerHeight
            };

            triangleMesh ??= new();

            Vector3[] faces = mesh.GetFaces();

            // This has to be done since Player has its origin at the bottom instead of middle
            for (int i = 0; i < faces.Length; i++)
            {
                faces[i].Y += PlayerConstants.PlayerHeight / 2.0f;
            }

            triangleMesh.CreateFromFaces(faces);

            arrowLinesMain ??=
            [
                new(0.0f, 0.0f, 0.0f),
                new(0.0f, 0.0f, -1.0f),
                new(0.0f, 0.0f, -1.0f),
                new(0.25f, 0.0f, -0.75f),
                new(0.0f, 0.0f, -1.0f),
                new(-0.25f, 0.0f, -0.75f)
            ];

            for (int i = 0; i < arrowLinesMain.Length; i++)
            {
                Vector3 newVector = arrowLinesMain[i];
                newVector.Y = PlayerConstants.PlayerHeight / 2.0f;
                arrowLinesMain[i] = newVector;
            }

            arrowLinesAdditional ??=
            [
                new(-0.1f, 0.0f, 0.1f),
                new(0.1f, 0.0f, 0.1f),
                new(0.1f, 0.0f, 0.1f),
                new(0.1f, 0.0f, -0.65f),
                new(0.1f, 0.0f, -0.65f),
                new(0.45f, 0.0f, -0.65f),
                new(0.45f, 0.0f, -0.65f),
                new(0.0f, 0.0f, -1.15f),
                new(0.0f, 0.0f, -1.15f),
                new(-0.45f, 0.0f, -0.65f),
                new(-0.45f, 0.0f, -0.65f),
                new(-0.1f, 0.0f, -0.65f),
                new(-0.1f, 0.0f, -0.65f),
                new(-0.1f, 0.0f, 0.1f),
            ];

            for (int i = 0; i < arrowLinesAdditional.Length; i++)
            {
                Vector3 newVector = arrowLinesAdditional[i];
                newVector.Y = PlayerConstants.PlayerHeight / 2.0f;
                arrowLinesAdditional[i] = newVector;
            }

            transform = new()
            {
                Basis = Basis.Identity,
                Origin = new Vector3(0.0f, PlayerConstants.PlayerHeight / 2.0f, 0.0f)
            };
        }

        public override void _Redraw(EditorNode3DGizmo gizmo)
        {
            gizmo.Clear();

            PlayerMarker marker = (PlayerMarker)gizmo.GetNode3D();

            StandardMaterial3D targetMaterial = GetMaterial(material);
            StandardMaterial3D arrowMaterial = GetMaterial(arrow);
            StandardMaterial3D arrowMaterialAdditional = GetMaterial(arrowAdditional);

            arrowMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            arrowMaterial.AlbedoColor = arrowColorMain;
            arrowMaterial.DisableFog = true;
            arrowMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;

            arrowMaterialAdditional.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
            arrowMaterialAdditional.AlbedoColor = arrowColorAdditional;
            arrowMaterialAdditional.DisableFog = true;
            arrowMaterialAdditional.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;

            switch (marker.Type)
            {
                case PlayerMarker.MarkerType.Start:
                    {
                        targetMaterial.AlbedoColor = green;
                        break;
                    }
            }

            gizmo.AddMesh(mesh, targetMaterial, transform);
            gizmo.AddCollisionTriangles(triangleMesh);
            gizmo.AddLines(arrowLinesMain, arrowMaterial);
            gizmo.AddLines(arrowLinesAdditional, arrowMaterialAdditional);

            //gizmo.AddMesh
        }
    }
}
