// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace FuncGodot
{
    /// <summary>
    /// Stateless helpers shared by the parsing, geometry generation, and entity assembly stages.
    /// </summary>
    public static class FuncGodotUtil
    {
        public const float VertexEpsilon = 0.008f;

        public const string DefaultTexturePath = "res://addons/func_godot_csharp/textures/default_texture.png";

        public static void PrintProfileInfo(string message, string signature)
        {
            GD.Print(signature + " " + message);
        }

        public static string Newline()
        {
            return OS.GetName() == "Windows" ? "\r\n" : "\n";
        }

        #region MATH

        public static Vector3 Vec3Average(IReadOnlyList<Vector3> vectors)
        {
            if (vectors.Count == 0)
            {
                GD.PushError("Cannot average empty Vector3 array!");
                return Vector3.Zero;
            }

            Vector3 sum = Vector3.Zero;

            foreach (Vector3 vector in vectors)
            {
                sum += vector;
            }

            return sum / vectors.Count;
        }

        /// Conversion from the id Tech coordinate system to Godot's.
        public static Vector3 IdToOpenGl(Vector3 vector)
        {
            return new Vector3(vector.Y, vector.Z, vector.X);
        }

        public static bool IsPointInConvexHull(IReadOnlyList<Plane> planes, Vector3 vertex)
        {
            foreach (Plane plane in planes)
            {
                if (plane.Normal.Dot(vertex) - plane.D > VertexEpsilon)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region TEXTURES

        private static readonly BaseMaterial3D.TextureParam[] _pbrTextures =
        [
            BaseMaterial3D.TextureParam.Albedo,
            BaseMaterial3D.TextureParam.Normal,
            BaseMaterial3D.TextureParam.Metallic,
            BaseMaterial3D.TextureParam.Roughness,
            BaseMaterial3D.TextureParam.Emission,
            BaseMaterial3D.TextureParam.AmbientOcclusion,
            BaseMaterial3D.TextureParam.Heightmap,
            BaseMaterial3D.TextureParam.Orm,
        ];

        // Parallel to _pbrTextures. Null means the feature is always on and needs no explicit enable.
        private static readonly BaseMaterial3D.Feature?[] _pbrFeatures =
        [
            null,
            BaseMaterial3D.Feature.NormalMapping,
            null,
            null,
            BaseMaterial3D.Feature.Emission,
            BaseMaterial3D.Feature.AmbientOcclusion,
            BaseMaterial3D.Feature.HeightMapping,
            null,
        ];

        /// Searches the base texture directory for a texture, falling back to the default texture.
        public static Texture2D LoadTexture(string textureName, FuncGodotMapSettings mapSettings)
        {
            foreach (string extension in mapSettings.TextureFileExtensions)
            {
                string texturePath = mapSettings.BaseTextureDir.PathJoin(textureName + "." + extension);

                if (!ResourceLoader.Exists(texturePath))
                {
                    continue;
                }

                if (ResourceLoader.Load(texturePath) is Texture2D texture)
                {
                    return texture;
                }

                GD.PushError($"Error: Texture load failed! ({texturePath}) not a valid Texture2D resource");
            }

            return ResourceLoader.Load<Texture2D>(DefaultTexturePath);
        }

        public static bool IsSkip(string texture, FuncGodotMapSettings mapSettings)
        {
            return mapSettings != null && texture.ToLower() == mapSettings.SkipTexture;
        }

        public static bool IsClip(string texture, FuncGodotMapSettings mapSettings)
        {
            return mapSettings != null && texture.ToLower() == mapSettings.ClipTexture;
        }

        public static bool IsOrigin(string texture, FuncGodotMapSettings mapSettings)
        {
            return mapSettings != null && texture.ToLower() == mapSettings.OriginTexture;
        }

        /// Shadow faces are moved into a separate shadow-only mesh rather than the main visual mesh.
        public static bool IsShadow(string texture, FuncGodotMapSettings mapSettings)
        {
            return mapSettings != null
                && !string.IsNullOrEmpty(mapSettings.ShadowTexture)
                && texture.ToLower() == mapSettings.ShadowTexture;
        }

        /// True for any tool texture, none of which contribute to the visual mesh.
        public static bool FilterFace(string texture, FuncGodotMapSettings mapSettings)
        {
            if (mapSettings == null)
            {
                return false;
            }

            return IsSkip(texture, mapSettings)
                || IsClip(texture, mapSettings)
                || IsOrigin(texture, mapSettings)
                || IsShadow(texture, mapSettings);
        }

        /// <summary>
        /// The surface type a face's material declares, used to split collision into separately tagged
        /// <see cref="StaticCollisionShape3D"/> nodes. Null for any material that is not a
        /// <see cref="UnaryStandartMaterial3D"/>, which lands the face in the untyped default pool.
        /// </summary>
        public static UnaryStandartMaterial3D.SurfaceType? GetCollisionSurfaceType(Material material)
        {
            if (material is UnaryStandartMaterial3D unaryMaterial)
            {
                return unaryMaterial.Type;
            }

            return null;
        }

        /// Adds PBR textures to an existing BaseMaterial3D by matching the map settings' name patterns.
        public static void BuildBaseMaterial(FuncGodotMapSettings mapSettings, BaseMaterial3D material, string texture)
        {
            string path = mapSettings.BaseTextureDir.PathJoin(texture);

            // A texture may live in a subfolder named after itself, alongside its PBR maps.
            if (DirAccess.DirExistsAbsolute(path))
            {
                path = path.PathJoin(texture);
            }

            string[] patterns =
            [
                mapSettings.AlbedoMapPattern,
                mapSettings.NormalMapPattern,
                mapSettings.MetallicMapPattern,
                mapSettings.RoughnessMapPattern,
                mapSettings.EmissionMapPattern,
                mapSettings.AoMapPattern,
                mapSettings.HeightMapPattern,
                mapSettings.OrmMapPattern,
            ];

            for (int i = 0; i < patterns.Length; i++)
            {
                string pattern = patterns[i];

                if (string.IsNullOrEmpty(pattern))
                {
                    continue;
                }

                if (!pattern.Contains("{0}"))
                {
                    GD.PushError($"No string replacement tokens found in auto-PBR pattern '{pattern}'! Must have at least one instance of '{{0}}' per pattern.");
                    continue;
                }

                foreach (string extension in mapSettings.TextureFileExtensions)
                {
                    string pbrPath = pattern.Contains("{1}")
                        ? string.Format(pattern, path, extension)
                        : string.Format(pattern, path) + "." + extension;

                    if (!ResourceLoader.Exists(pbrPath))
                    {
                        continue;
                    }

                    if (_pbrFeatures[i].HasValue)
                    {
                        material.SetFeature(_pbrFeatures[i].Value, true);
                    }

                    material.SetTexture(_pbrTextures[i], ResourceLoader.Load<Texture2D>(pbrPath));
                    break;
                }
            }
        }

        /// <summary>
        /// Resolves every texture referenced by the visual brushes to a material and an albedo size. The
        /// sizes feed UV mapping, which needs texel dimensions to normalize the Valve 220 texture axes.
        /// </summary>
        public static void BuildTextureMap(
            List<FuncGodotData.EntityData> entityData,
            FuncGodotMapSettings mapSettings,
            out Dictionary<string, Material> textureMaterials,
            out Dictionary<string, Vector2> textureSizes)
        {
            textureMaterials = [];
            textureSizes = [];

            foreach (FuncGodotData.EntityData entity in entityData)
            {
                if (!entity.IsVisual())
                {
                    continue;
                }

                foreach (FuncGodotData.BrushData brush in entity.Brushes)
                {
                    foreach (FuncGodotData.FaceData face in brush.Faces)
                    {
                        string textureName = face.Texture;

                        if (FilterFace(textureName, mapSettings) || textureMaterials.ContainsKey(textureName))
                        {
                            continue;
                        }

                        string materialDir = string.IsNullOrEmpty(mapSettings.BaseMaterialDir)
                            ? mapSettings.BaseTextureDir
                            : mapSettings.BaseMaterialDir;

                        string materialPath = materialDir.PathJoin(textureName)
                            + "." + mapSettings.MaterialFileExtension;

                        materialPath = materialPath.Replace("*", "");

                        if (ResourceLoader.Exists(materialPath))
                        {
                            Material material = ResourceLoader.Load<Material>(materialPath);
                            textureMaterials[textureName] = material;

                            Texture2D albedo = null;

                            if (material is BaseMaterial3D baseMaterial)
                            {
                                albedo = baseMaterial.AlbedoTexture;
                            }
                            else if (material is ShaderMaterial shaderMaterial
                                && !string.IsNullOrEmpty(mapSettings.DefaultMaterialAlbedoUniform))
                            {
                                albedo = shaderMaterial
                                    .GetShaderParameter(mapSettings.DefaultMaterialAlbedoUniform)
                                    .As<Texture2D>();
                            }

                            if (albedo != null)
                            {
                                textureSizes[textureName] = albedo.GetSize();
                            }
                            else
                            {
                                Texture2D texture = LoadTexture(textureName, mapSettings);

                                textureSizes[textureName] = texture != null
                                    ? texture.GetSize()
                                    : Vector2.One * mapSettings.InverseScaleFactor;
                            }

                            continue;
                        }

                        if (mapSettings.DefaultMaterial == null)
                        {
                            continue;
                        }

                        // Material generation
                        Material generated = (Material)mapSettings.DefaultMaterial.Duplicate(false);
                        Texture2D generatedTexture = LoadTexture(textureName, mapSettings);
                        textureSizes[textureName] = generatedTexture.GetSize();

                        if (generated is BaseMaterial3D generatedBase)
                        {
                            generatedBase.AlbedoTexture = generatedTexture;
                            BuildBaseMaterial(mapSettings, generatedBase, textureName);
                        }
                        else if (generated is ShaderMaterial generatedShader)
                        {
                            generatedShader.SetShaderParameter(mapSettings.DefaultMaterialAlbedoUniform, generatedTexture);

                            foreach (string uniform in mapSettings.ShaderMaterialUniformMapPatterns.Keys)
                            {
                                string pattern = mapSettings.ShaderMaterialUniformMapPatterns[uniform];

                                if (!pattern.Contains("{0}"))
                                {
                                    GD.PushError($"No string replacement tokens found in ShaderMaterial uniform map pattern '{pattern}'! Must have one instance of '{{0}}' per pattern.");
                                    continue;
                                }

                                foreach (string extension in mapSettings.TextureFileExtensions)
                                {
                                    string uniformTexturePath = mapSettings.BaseTextureDir.PathJoin(
                                        string.Format(pattern, textureName) + "." + extension);

                                    if (!ResourceLoader.Exists(uniformTexturePath))
                                    {
                                        continue;
                                    }

                                    generatedShader.SetShaderParameter(uniform, ResourceLoader.Load<Texture2D>(uniformTexturePath));
                                    break;
                                }
                            }
                        }

                        if (mapSettings.SaveGeneratedMaterials
                            && !FilterFace(textureName, mapSettings)
                            && generatedTexture.ResourcePath != DefaultTexturePath)
                        {
                            string baseDir = materialPath.GetBaseDir();

                            if (!DirAccess.DirExistsAbsolute(baseDir))
                            {
                                DirAccess.MakeDirRecursiveAbsolute(baseDir);
                            }

                            ResourceSaver.Save(generated, materialPath);
                        }

                        textureMaterials[textureName] = generated;
                    }
                }
            }
        }

        #endregion

        #region UV MAPPING

        /// <summary>
        /// UV coordinate for a vertex in the Valve 220 format, where each face carries its own texture axes.
        /// </summary>
        public static Vector2 GetValveUv(Vector3 vertex, Vector3 uAxis, Vector3 vAxis, Transform2D uvBasis, Vector2 textureSize)
        {
            Vector2 uv = new(uAxis.Dot(vertex), vAxis.Dot(vertex));
            Vector2 scale = new(uvBasis.X.X, uvBasis.Y.Y);

            uv += uvBasis.Origin * scale;
            uv /= scale;
            uv.X /= textureSize.X;
            uv.Y /= textureSize.Y;

            return uv;
        }

        public static Vector2 GetFaceVertexUv(Vector3 vertex, FuncGodotData.FaceData face, Vector2 textureSize)
        {
            return GetValveUv(vertex, face.UvAxes[0], face.UvAxes[1], face.Uv, textureSize);
        }

        /// Tangent for the Valve 220 format, taken directly from the face's texture axes.
        public static float[] GetValveTangent(Vector3 u, Vector3 v, Vector3 normal)
        {
            Vector3 uAxis = u.Normalized();
            Vector3 vAxis = v.Normalized();
            float vSign = -Mathf.Sign(normal.Cross(uAxis).Dot(vAxis));

            return [uAxis.X, uAxis.Y, uAxis.Z, vSign];
        }

        public static float[] GetFaceTangent(FuncGodotData.FaceData face)
        {
            return GetValveTangent(face.UvAxes[0], face.UvAxes[1], face.Plane.Normal);
        }

        #endregion

        #region MESH

        /// <summary>
        /// Averages the normals of every vertex shared between faces whose normals lie within
        /// <paramref name="angleDegrees"/> of one another, producing a smooth-shaded copy of the mesh.
        /// </summary>
        public static ArrayMesh SmoothMeshByAngle(ArrayMesh mesh, float angleDegrees = 89.0f)
        {
            if (mesh == null)
            {
                GD.PushError("Need a source mesh to smooth");
                return null;
            }

            float angle = Mathf.DegToRad(Mathf.Clamp(angleDegrees, 0.0f, 360.0f));

            List<Vector3> meshVertices = [];
            List<Vector3> meshNormals = [];
            List<(MeshDataTool Tool, int Offset, Material Material)> surfaceData = [];

            for (int surfaceIndex = 0; surfaceIndex < mesh.GetSurfaceCount(); surfaceIndex++)
            {
                MeshDataTool tool = new();

                if (tool.CreateFromSurface(mesh, surfaceIndex) != Error.Ok)
                {
                    continue;
                }

                surfaceData.Add((tool, meshVertices.Count, mesh.SurfaceGetMaterial(surfaceIndex)));

                for (int i = 0; i < tool.GetVertexCount(); i++)
                {
                    meshVertices.Add(tool.GetVertex(i));
                    meshNormals.Add(tool.GetVertexNormal(i));
                }
            }

            // Group vertex indices by welded position. Positions were snapped during generation, so an exact
            // key comparison is safe here.
            Dictionary<Vector3, List<int>> groups = [];

            for (int i = 0; i < meshVertices.Count; i++)
            {
                Vector3 key = meshVertices[i].Snapped(Vector3.One * VertexEpsilon);

                if (!groups.TryGetValue(key, out List<int> group))
                {
                    group = [];
                    groups[key] = group;
                }

                group.Add(i);
            }

            Vector3[] smoothedNormals = [.. meshNormals];

            foreach (List<int> group in groups.Values)
            {
                foreach (int i in group)
                {
                    Vector3 current = meshNormals[i];
                    Vector3 sum = Vector3.Zero;

                    foreach (int j in group)
                    {
                        Vector3 other = meshNormals[j];

                        if (current.AngleTo(other) <= angle)
                        {
                            sum += other;
                        }
                    }

                    smoothedNormals[i] = sum.Normalized();
                }
            }

            ArrayMesh smoothedMesh = new();

            foreach ((MeshDataTool tool, int offset, Material material) in surfaceData)
            {
                for (int i = 0; i < tool.GetVertexCount(); i++)
                {
                    tool.SetVertexNormal(i, smoothedNormals[offset + i]);
                }

                SurfaceTool surfaceTool = new();
                surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
                surfaceTool.SetMaterial(material);

                for (int i = 0; i < tool.GetFaceCount(); i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int index = tool.GetFaceVertex(i, j);
                        surfaceTool.SetNormal(tool.GetVertexNormal(index));
                        surfaceTool.SetUV(tool.GetVertexUV(index));
                        surfaceTool.SetTangent(tool.GetVertexTangent(index));
                        surfaceTool.AddVertex(tool.GetVertex(index));
                    }
                }

                smoothedMesh = surfaceTool.Commit(smoothedMesh);
            }

            return smoothedMesh;
        }

        #endregion
    }
}
