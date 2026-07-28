using Godot;
using Godot.Collections;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UnaryCollisionShape3D : CollisionShape3D
    {
        public static StringName color { get; } = new("color");
        public static StringName bleach { get; } = new("bleach");

#if TOOLS
        [Export]
        public Material VisualizerMaterial;

        [Export]
        public Color VisualizerColor = Colors.White;

        [Export]
        public Color BleachColor = Colors.White;

        public override void _ValidateProperty(Dictionary property)
        {
            property.MakeHidden(PropertyName.VisualizerMaterial, PropertyName.VisualizerColor, PropertyName.BleachColor);
            base._ValidateProperty(property);
        }
#endif

        [Export]
        public UnaryStandartMaterial3D.SurfaceType Type;

        public override void _Ready()
        {
#if !TOOLS
            return;
#endif

            if (!Engine.Singleton.IsEditorHint())
            {
                return;
            }

            if (VisualizerMaterial == null || Shape == null)
            {
                return;
            }

            CallDeferred(MethodName.CreateVisualizer);
        }

        private void CreateVisualizer()
        {
            DebugFill = true;
            ArrayMesh visualizer = BuildVisualizerMesh(Shape.GetDebugMesh());
            DebugFill = false;

            if (visualizer == null)
            {
                return;
            }

            MeshInstance3D mesh = new()
            {
                Mesh = visualizer,
                MaterialOverride = VisualizerMaterial
            };

            mesh.SetInstanceShaderParameter(color, VisualizerColor);
            mesh.SetInstanceShaderParameter(bleach, BleachColor);

            AddChild(mesh);
        }

        private static ArrayMesh BuildVisualizerMesh(ArrayMesh debugMesh)
        {
            ArrayMesh visualizer = null;

            for (int surface = 0; surface < debugMesh.GetSurfaceCount(); surface++)
            {
                if (debugMesh.SurfaceGetPrimitiveType(surface) != Mesh.PrimitiveType.Triangles)
                {
                    continue;
                }

                Array arrays = debugMesh.SurfaceGetArrays(surface);

                if (arrays[(int)Mesh.ArrayType.Normal].VariantType == Variant.Type.Nil)
                {
                    arrays = FlattenNormals(arrays);
                }

                visualizer ??= new ArrayMesh();
                visualizer.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            }

            return visualizer;
        }

        private static Array FlattenNormals(Array arrays)
        {
            Vector3[] vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            Variant indices = arrays[(int)Mesh.ArrayType.Index];

            if (indices.VariantType != Variant.Type.Nil)
            {
                int[] indexArray = indices.AsInt32Array();
                Vector3[] expanded = new Vector3[indexArray.Length];

                for (int i = 0; i < indexArray.Length; i++)
                {
                    expanded[i] = vertices[indexArray[i]];
                }

                vertices = expanded;
            }

            Vector3[] normals = new Vector3[vertices.Length];

            for (int i = 0; i + 2 < vertices.Length; i += 3)
            {
                Vector3 normal = (vertices[i + 2] - vertices[i]).Cross(vertices[i + 1] - vertices[i]).Normalized();

                normals[i] = normal;
                normals[i + 1] = normal;
                normals[i + 2] = normal;
            }

            Array result = [];
            result.Resize((int)Mesh.ArrayType.Max);
            result[(int)Mesh.ArrayType.Vertex] = vertices;
            result[(int)Mesh.ArrayType.Normal] = normals;

            return result;
        }
    }
}
