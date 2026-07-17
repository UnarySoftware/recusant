// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

using Godot;

namespace FuncGodot
{
    /// <summary>
    /// A <see cref="FuncGodotFGDPointClass"/> that exports a simplified GLB of its scene for TrenchBroom to
    /// display in the viewport.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotFGDModelPointClass : FuncGodotFGDPointClass
    {
        /// Display model export folder, relative to <see cref="FuncGodotConfig.ModelPointClassSavePath"/>.
        [Export]
        public string ModelsSubFolder = string.Empty;

        /// <summary>
        /// TrenchBroom scale expression applied to the model. Falls back to
        /// <see cref="FuncGodotConfig.DefaultInverseScaleFactor"/> when empty.
        /// </summary>
        [Export]
        public string ScaleExpression = string.Empty;

        /// <summary>
        /// Overrides the <c>size</c> meta property with one generated from the scene's AABB. Requires
        /// <see cref="ScaleExpression"/> to be a float or vector. The generated size rarely lands on grid.
        /// </summary>
        [Export]
        public bool GenerateSizeProperty = false;

        /// Degrees to rotate the model before export, for editors that read GLTF transforms differently.
        [Export]
        public Vector3 RotationOffset = Vector3.Zero;

        /// Set by FuncGodotFGDFile during export, so models are only written when the FGD asks for them.
        public bool ModelGenerationEnabled = false;

        /// Writes a .gdignore into the model export folder so Godot does not import the display models.
        [ExportToolButton("Generate GD Ignore File", Icon = "FileAccess")]
        public Callable GenerateGdIgnoreFileButton => Callable.From(GenerateGdIgnoreFile);

        private void GenerateGdIgnoreFile()
        {
            if (!Engine.Singleton.IsEditorHint())
            {
                return;
            }

            string path = GetGamePath().PathJoin(GetModelFolder());
            Error error = DirAccess.MakeDirRecursiveAbsolute(path);

            if (error != Error.Ok)
            {
                GD.PushError("Failed creating dir for GDIgnore file: ", error);
                return;
            }

            path = path.PathJoin(".gdignore");

            if (FileAccess.FileExists(path))
            {
                return;
            }

            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            file?.StoreString(string.Empty);
        }

        public override string BuildDefText()
        {
            if (ModelGenerationEnabled)
            {
                GenerateModel();
                ModelGenerationEnabled = false;
            }

            return base.BuildDefText();
        }

        private void GenerateModel()
        {
            if (SceneFile == null)
            {
                return;
            }

            if (SceneFile.Instantiate() is not Node3D node)
            {
                GD.PushError("Scene is not of type 'Node3D'");
                return;
            }

            GltfState state = new();

            if (!CreateGltfFile(state, GetExportPath(), node))
            {
                GD.PushError("Could not create gltf file");
                return;
            }

            node.QueueFree();

            MetaProperties["model"] = string.IsNullOrEmpty(ScaleExpression)
                ? $"{{\"path\": \"{GetLocalPath()}\", \"scale\": {GetDefaultInverseScaleFactor()} }}"
                : $"{{\"path\": \"{GetLocalPath()}\", \"scale\": {ScaleExpression} }}";

            if (GenerateSizeProperty && SceneFile.Instantiate() is Node3D sizeNode)
            {
                MetaProperties["size"] = GenerateSizeFromAabb(sizeNode);
                sizeNode.QueueFree();
            }
        }

        private static float GetDefaultInverseScaleFactor()
        {
            return FuncGodotConfig.Load()?.DefaultInverseScaleFactor ?? 32.0f;
        }

        private string GetExportPath()
        {
            return GetGamePath().PathJoin(GetModelFolder()).PathJoin($"{Classname}.glb");
        }

        private string GetLocalPath()
        {
            return GetModelFolder().PathJoin($"{Classname}.glb");
        }

        private string GetModelFolder()
        {
            string modelDir = FuncGodotConfig.Load()?.ModelPointClassSavePath ?? string.Empty;

            if (!string.IsNullOrEmpty(ModelsSubFolder))
            {
                modelDir = modelDir.PathJoin(ModelsSubFolder);
            }

            return modelDir;
        }

        private static string GetGamePath()
        {
            return FuncGodotLocalConfig.GetSetting(FuncGodotLocalConfig.Property.MapEditorGamePath);
        }

        private bool CreateGltfFile(GltfState state, string path, Node3D node)
        {
            GltfDocument document = new();
            state.CreateAnimations = false;

            node.RotateX(Mathf.DegToRad(RotationOffset.X));
            node.RotateY(Mathf.DegToRad(RotationOffset.Y));
            node.RotateZ(Mathf.DegToRad(RotationOffset.Z));

            // TrenchBroom scales display models itself via the scale expression, so the GLB stays unscaled.
            Error error = document.AppendFromScene(node, state);

            if (error != Error.Ok)
            {
                GD.PushError("Failed appending to gltf document: ", error);
                return false;
            }

            Callable.From(() => SaveToFileSystem(document, state, path)).CallDeferred();
            return true;
        }

        private static void SaveToFileSystem(GltfDocument document, GltfState state, string path)
        {
            Error error = DirAccess.MakeDirRecursiveAbsolute(path.GetBaseDir());

            if (error != Error.Ok)
            {
                GD.PushError("Failed creating dir: ", error);
                return;
            }

            error = document.WriteToFilesystem(state, path);

            if (error != Error.Ok)
            {
                GD.PushError("Failed writing to file system: ", error);
                return;
            }

            GD.Print("Exported model to ", path);
        }

        private static Aabb GetNodeAabb(Node3D node, Transform3D parentGlobalTransform)
        {
            Aabb aabb = new();
            Transform3D globalTransform = parentGlobalTransform * node.Transform;

            if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
            {
                aabb = globalTransform * meshInstance.Mesh.GetAabb();
            }

            foreach (Node child in node.GetChildren())
            {
                if (child is Node3D child3D)
                {
                    aabb = aabb.Merge(GetNodeAabb(child3D, globalTransform));
                }
            }

            return aabb;
        }

        private Aabb GenerateSizeFromAabb(Node3D scene)
        {
            Aabb aabb = GetNodeAabb(scene, Transform3D.Identity);

            // Reorient into TrenchBroom's coordinate system.
            Aabb size = new(
                new Vector3(aabb.Position.Z, aabb.Position.X, aabb.Position.Y),
                new Vector3(aabb.Size.Z, aabb.Size.X, aabb.Size.Y));

            Vector3 scaleFactor = Vector3.One;

            if (string.IsNullOrEmpty(ScaleExpression))
            {
                scaleFactor *= GetDefaultInverseScaleFactor();
            }
            else if (ScaleExpression.StartsWith('\''))
            {
                // A quoted scale expression may hold a full vector, e.g. '32 32 32'.
                string[] components = ScaleExpression
                    .Trim('\'')
                    .Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

                if (components.Length == 3)
                {
                    scaleFactor *= new Vector3(
                        components[0].ToFloat(),
                        components[1].ToFloat(),
                        components[2].ToFloat());
                }
            }
            else if (ScaleExpression.ToFloat() > 0.0f)
            {
                scaleFactor *= ScaleExpression.ToFloat();
            }

            size.Position *= scaleFactor;
            size.Size *= scaleFactor;
            size.Size += size.Position;

            // Round so the bounds can at least stay on grid level 1.
            size.Position = size.Position.Round();
            size.Size = size.Size.Round();

            return size;
        }
    }
}
