// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/func_godot_csharp/LICENSE.

using Godot;
using System;
using System.Collections.Generic;
using Unary.Core;

namespace FuncGodot
{
    /// <summary>
    /// Turns parsed brush planes into meshes and collision shapes. Each entity is processed independently, so
    /// the per-entity stages run in parallel.
    /// </summary>
    public sealed class FuncGodotGeometryGenerator
    {
        public const string Signature = "[GEO]";

        private const float VertexEpsilon = FuncGodotUtil.VertexEpsilon;

        /// Rounding applied to plane lookup keys when matching opposing faces, to absorb float error.
        private const float OcclusionPrecision = 100.0f;

        private readonly FuncGodotMapSettings _mapSettings;
        private readonly float _hyperplaneSize;

        private List<FuncGodotData.EntityData> _entityData = [];
        private Dictionary<string, Material> _textureMaterials = [];
        private Dictionary<string, Vector2> _textureSizes = [];

        public event Action<string> DeclareStep;

        private void Step(string step)
        {
            DeclareStep?.Invoke(step);
        }

        public FuncGodotGeometryGenerator(FuncGodotMapSettings mapSettings, float hyperplaneSize = 512.0f)
        {
            _mapSettings = mapSettings;
            _hyperplaneSize = hyperplaneSize;
        }

        #region BUILD

        /// Runs every generation stage over all entities.
        public Error Build(FuncGodotMap.BuildFlagBits buildFlags, List<FuncGodotData.EntityData> entities)
        {
            _entityData = entities;

            int entityCount = entities.Count;

            Step($"Preparing {entityCount} {(entityCount == 1 ? "entity" : "entities")}");

            Step("Gathering materials");
            FuncGodotUtil.BuildTextureMap(_entityData, _mapSettings, out _textureMaterials, out _textureSizes);

            Step("Generating brush vertices");
            Multi.Thread(0, entityCount, (index, state) => GenerateEntityVertices(index));

            Step("Determining solid entity origins");
            Multi.Thread(0, entityCount, (index, state) => DetermineEntityOrigin(index));

            Step("Winding faces");
            Multi.Thread(0, entityCount, (index, state) => WindEntityFaces(index));

            Step("Generating surfaces");
            Multi.Thread(0, entityCount, (index, state) => GenerateEntitySurfaces(index));

            if (buildFlags.HasFlag(FuncGodotMap.BuildFlagBits.UnwrapUv2))
            {
                Step("Unwrapping UV2s");

                float texelSize = _mapSettings.UvUnwrapTexelSize * _mapSettings.ScaleFactor;

                // lightmap_unwrap is not thread safe, so this stage stays serial.
                for (int index = 0; index < entityCount; index++)
                {
                    UnwrapUv2(index, texelSize);
                }
            }

            Step("Geometry generation complete");

            return Error.Ok;
        }

        #endregion

        #region TOOL TEXTURES

        private bool IsSkip(FuncGodotData.FaceData face)
        {
            return FuncGodotUtil.IsSkip(face.Texture, _mapSettings);
        }

        private bool IsClip(FuncGodotData.FaceData face)
        {
            return FuncGodotUtil.IsClip(face.Texture, _mapSettings);
        }

        private bool IsOrigin(FuncGodotData.FaceData face)
        {
            return FuncGodotUtil.IsOrigin(face.Texture, _mapSettings);
        }

        private bool IsShadow(FuncGodotData.FaceData face)
        {
            return FuncGodotUtil.IsShadow(face.Texture, _mapSettings);
        }

        private Material GetMaterial(string texture)
        {
            return _textureMaterials.GetValueOrDefault(texture);
        }

        private Vector2 GetTextureSize(string texture)
        {
            return _textureSizes.TryGetValue(texture, out Vector2 size)
                ? size
                : Vector2.One * _mapSettings.InverseScaleFactor;
        }

        /// <summary>
        /// The surface type of a whole brush, taken from the first of its faces whose material declares one.
        /// Used to tag the brush's convex collision shape.
        /// </summary>
        private UnaryStandartMaterial3D.SurfaceType? GetBrushSurfaceType(FuncGodotData.BrushData brush)
        {
            foreach (FuncGodotData.FaceData face in brush.Faces)
            {
                UnaryStandartMaterial3D.SurfaceType? surfaceType =
                    FuncGodotUtil.GetCollisionSurfaceType(GetMaterial(face.Texture));

                if (surfaceType.HasValue)
                {
                    return surfaceType;
                }
            }

            return null;
        }

        #endregion

        #region BRUSH VERTICES

        /// <summary>
        /// An oversized quad lying on the plane, large enough to cover the map, which the brush's other planes
        /// are then clipped against.
        /// </summary>
        private Vector3[] GenerateBaseWinding(Plane plane)
        {
            Vector3 up = Mathf.Abs(plane.Normal.Dot(Vector3.Up)) > 0.9f ? Vector3.Right : Vector3.Up;

            Vector3 right = plane.Normal.Cross(up).Normalized();
            Vector3 forward = right.Cross(plane.Normal).Normalized();
            Vector3 centroid = plane.GetCenter();

            float size = _hyperplaneSize;

            return
            [
                centroid + (right * size) + (forward * size),
                centroid + (right * -size) + (forward * size),
                centroid + (right * -size) + (forward * -size),
                centroid + (right * size) + (forward * -size),
            ];
        }

        /// Clips a face's base winding against every other plane in its brush, leaving the real polygon.
        private Vector3[] GenerateFaceVertices(FuncGodotData.BrushData brush, int faceIndex, float vertexMergeDistance)
        {
            Vector3[] winding = GenerateBaseWinding(brush.Faces[faceIndex].Plane);

            for (int otherIndex = 0; otherIndex < brush.Faces.Count; otherIndex++)
            {
                if (otherIndex == faceIndex)
                {
                    continue;
                }

                winding = Geometry3D.ClipPolygon(winding, brush.Faces[otherIndex].Plane);

                if (winding.Length == 0)
                {
                    return winding;
                }
            }

            if (vertexMergeDistance <= 0.0f || winding.Length == 0)
            {
                return winding;
            }

            // Snap and drop vertices that collapse onto their predecessor, which closes hairline seams.
            List<Vector3> merged = [];
            Vector3 previous = winding[0].Snapped(Vector3.One * vertexMergeDistance);
            merged.Add(previous);

            for (int i = 1; i < winding.Length; i++)
            {
                Vector3 current = winding[i].Snapped(Vector3.One * vertexMergeDistance);

                if (current != previous)
                {
                    merged.Add(current);
                }

                previous = current;
            }

            return [.. merged];
        }

        private void GenerateEntityVertices(int entityIndex)
        {
            FuncGodotData.EntityData entity = _entityData[entityIndex];

            // Configured on the entity definition, overridable per entity by the map property.
            float vertexMergeDistance = entity.Definition is FuncGodotFGDSolidClass solid ? solid.VertexMergeDistance : 0.0f;

            if (entity.Properties.TryGetValue(_mapSettings.VertexMergeDistanceProperty, out Variant mergeDistance))
            {
                vertexMergeDistance = mergeDistance.AsSingle();
            }

            foreach (FuncGodotData.BrushData brush in entity.Brushes)
            {
                for (int faceIndex = 0; faceIndex < brush.Faces.Count; faceIndex++)
                {
                    FuncGodotData.FaceData face = brush.Faces[faceIndex];

                    face.Vertices = GenerateFaceVertices(brush, faceIndex, vertexMergeDistance);

                    face.Normals = new Vector3[face.Vertices.Length];
                    Array.Fill(face.Normals, face.Plane.Normal);

                    float[] tangent = FuncGodotUtil.GetFaceTangent(face);
                    face.Tangents = new float[face.Vertices.Length * 4];

                    // Tangents are stored in Godot's coordinate system, unlike the vertices.
                    for (int i = 0; i < face.Vertices.Length; i++)
                    {
                        face.Tangents[(i * 4) + 0] = tangent[1];
                        face.Tangents[(i * 4) + 1] = tangent[2];
                        face.Tangents[(i * 4) + 2] = tangent[0];
                        face.Tangents[(i * 4) + 3] = tangent[3];
                    }
                }
            }
        }

        private void WindEntityFaces(int entityIndex)
        {
            foreach (FuncGodotData.BrushData brush in _entityData[entityIndex].Brushes)
            {
                foreach (FuncGodotData.FaceData face in brush.Faces)
                {
                    face.Wind();
                    face.IndexVertices();
                }
            }
        }

        #endregion

        #region ORIGINS

        /// <summary>
        /// Resolves the entity's origin, which the mesh vertices are then made relative to. Worldspawn always
        /// sits at the map node's own origin.
        /// </summary>
        private void DetermineEntityOrigin(int entityIndex)
        {
            FuncGodotData.EntityData entity = _entityData[entityIndex];

            FuncGodotFGDSolidClass solidClass = entity.Definition as FuncGodotFGDSolidClass;

            // A non-solid entity only has an origin worth computing if it somehow carries brushes.
            if (solidClass == null && entity.Brushes.Count == 0)
            {
                return;
            }

            if (entityIndex == 0)
            {
                entity.Origin = Vector3.Zero;
                return;
            }

            bool hasEntityBounds = false;
            bool hasOriginBounds = false;

            Vector3 entityMins = Vector3.Zero;
            Vector3 entityMaxs = Vector3.Zero;
            Vector3 originMins = Vector3.Zero;
            Vector3 originMaxs = Vector3.Zero;

            foreach (FuncGodotData.BrushData brush in entity.Brushes)
            {
                foreach (FuncGodotData.FaceData face in brush.Faces)
                {
                    foreach (Vector3 vertex in face.Vertices)
                    {
                        entityMins = hasEntityBounds ? entityMins.Min(vertex) : vertex;
                        entityMaxs = hasEntityBounds ? entityMaxs.Max(vertex) : vertex;
                        hasEntityBounds = true;

                        if (!brush.Origin)
                        {
                            continue;
                        }

                        originMins = hasOriginBounds ? originMins.Min(vertex) : vertex;
                        originMaxs = hasOriginBounds ? originMaxs.Max(vertex) : vertex;
                        hasOriginBounds = true;
                    }
                }
            }

            if (!hasEntityBounds)
            {
                return;
            }

            // Everything falls back to the bounds center.
            entity.Origin = entityMaxs - ((entityMaxs - entityMins) * 0.5f);

            FuncGodotFGDSolidClass.OriginTypes originType = solidClass?.OriginType
                ?? FuncGodotFGDSolidClass.OriginTypes.Brush;

            if (originType == FuncGodotFGDSolidClass.OriginTypes.BoundsCenter || entity.Brushes.Count == 0)
            {
                return;
            }

            switch (originType)
            {
                case FuncGodotFGDSolidClass.OriginTypes.Absolute:
                case FuncGodotFGDSolidClass.OriginTypes.Relative:
                    {
                        if (!entity.Properties.TryGetValue("origin", out Variant originProperty))
                        {
                            break;
                        }

                        if (!TryReadVector3(originProperty, out Vector3 origin))
                        {
                            break;
                        }

                        origin *= _mapSettings.ScaleFactor;

                        entity.Origin = originType == FuncGodotFGDSolidClass.OriginTypes.Absolute
                            ? origin
                            : entity.Origin + origin;

                        break;
                    }
                case FuncGodotFGDSolidClass.OriginTypes.Brush:
                    {
                        if (hasOriginBounds)
                        {
                            entity.Origin = originMaxs - ((originMaxs - originMins) * 0.5f);
                        }

                        break;
                    }
                case FuncGodotFGDSolidClass.OriginTypes.BoundsMins:
                    {
                        entity.Origin = entityMins;
                        break;
                    }
                case FuncGodotFGDSolidClass.OriginTypes.BoundsMaxs:
                    {
                        entity.Origin = entityMaxs;
                        break;
                    }
                case FuncGodotFGDSolidClass.OriginTypes.Averaged:
                    {
                        List<Vector3> vertices = [];

                        foreach (FuncGodotData.BrushData brush in entity.Brushes)
                        {
                            foreach (FuncGodotData.FaceData face in brush.Faces)
                            {
                                vertices.AddRange(face.Vertices);
                            }
                        }

                        entity.Origin = vertices.Count > 0 ? FuncGodotUtil.Vec3Average(vertices) : Vector3.Zero;
                        break;
                    }
            }
        }

        private static bool TryReadVector3(Variant value, out Vector3 vector)
        {
            if (value.VariantType == Variant.Type.Vector3)
            {
                vector = value.AsVector3();
                return true;
            }

            string[] components = value.AsString().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (components.Length > 2)
            {
                vector = new Vector3(components[0].ToFloat(), components[1].ToFloat(), components[2].ToFloat());
                return true;
            }

            vector = Vector3.Zero;
            return false;
        }

        #endregion

        #region SURFACES

        /// Rounded plane key, so a face and its exact opposite hash to matching keys.
        private static Vector4I GetPlaneLookupKey(Plane plane)
        {
            return new Vector4I(
                Mathf.RoundToInt(plane.Normal.X * OcclusionPrecision),
                Mathf.RoundToInt(plane.Normal.Y * OcclusionPrecision),
                Mathf.RoundToInt(plane.Normal.Z * OcclusionPrecision),
                Mathf.RoundToInt(plane.D * OcclusionPrecision));
        }

        /// <summary>
        /// Builds the mesh surfaces and collision shapes for one entity. Faces are grouped by texture, one
        /// surface per texture, with tool-textured faces diverted or dropped along the way.
        /// </summary>
        private void GenerateEntitySurfaces(int entityIndex)
        {
            FuncGodotData.EntityData entity = _entityData[entityIndex];

            if (entity.Brushes.Count == 0)
            {
                return;
            }

            FuncGodotFGDSolidClass definition = entity.Definition as FuncGodotFGDSolidClass ?? new FuncGodotFGDSolidClass();

            Vector3 EntityTransform(Vector3 vertex)
            {
                return FuncGodotUtil.IdToOpenGl(vertex - entity.Origin);
            }

            // Faces grouped by texture. Shadow faces are held aside for a separate shadows-only mesh.
            Dictionary<string, List<FuncGodotData.FaceData>> surfaces = [];
            Dictionary<string, List<FuncGodotData.FaceData>> shadowSurfaces = [];

            foreach (FuncGodotData.BrushData brush in entity.Brushes)
            {
                foreach (FuncGodotData.FaceData face in brush.Faces)
                {
                    if (IsSkip(face) || IsOrigin(face))
                    {
                        continue;
                    }

                    Dictionary<string, List<FuncGodotData.FaceData>> target = IsShadow(face) ? shadowSurfaces : surfaces;

                    if (!target.TryGetValue(face.Texture, out List<FuncGodotData.FaceData> faces))
                    {
                        faces = [];
                        target[face.Texture] = faces;
                    }

                    faces.Add(face);
                }
            }

            // Configured on the entity definition, overridable per entity by the map property.
            bool cullInteriorFaces = definition.CullInteriorFaces;

            if (entity.Properties.TryGetValue(_mapSettings.CullInteriorFacesProperty, out Variant cull))
            {
                cullInteriorFaces = cull.AsBool();
            }

            bool buildConcave = entity.IsCollisionConcave();

            MeshMetadataBuilder metadata = new(definition);

            // Concave collision triangles, bucketed by surface type. Faces whose material declares no surface
            // type collect in the untyped default pool, kept separate because a Dictionary rejects null keys.
            Dictionary<UnaryStandartMaterial3D.SurfaceType, List<Vector3>> concavePools = [];
            Dictionary<UnaryStandartMaterial3D.SurfaceType, List<FuncGodotData.FaceData>> concavePoolFaces = [];
            List<Vector3> defaultConcaveVertices = [];
            List<FuncGodotData.FaceData> defaultConcaveFaces = [];

            List<string> surfaceTextures = [];
            List<Godot.Collections.Array> surfaceArrays = [];

            foreach (KeyValuePair<string, List<FuncGodotData.FaceData>> surface in surfaces)
            {
                string textureName = surface.Key;
                List<FuncGodotData.FaceData> faces = surface.Value;

                int textureIndex = metadata.AddTexture(textureName);

                Dictionary<Vector4I, List<FuncGodotData.FaceData>> interiorLookup =
                    cullInteriorFaces ? BuildInteriorFaceLookup(faces) : null;

                List<Vector3> vertices = [];
                List<Vector3> normals = [];
                List<float> tangents = [];
                List<Vector2> uvs = [];
                List<int> indices = [];

                int indexOffset = 0;

                foreach (FuncGodotData.FaceData face in faces)
                {
                    if (face.Vertices.Length < 3)
                    {
                        continue;
                    }

                    if (cullInteriorFaces && IsInteriorFace(face, interiorLookup))
                    {
                        continue;
                    }

                    // Collision is built from every face, including the clip faces the mesh skips.
                    if (buildConcave)
                    {
                        UnaryStandartMaterial3D.SurfaceType? pool =
                            FuncGodotUtil.GetCollisionSurfaceType(GetMaterial(face.Texture));

                        List<Vector3> poolVertices;
                        List<FuncGodotData.FaceData> poolFaces;

                        if (pool.HasValue)
                        {
                            if (!concavePools.TryGetValue(pool.Value, out poolVertices))
                            {
                                concavePools[pool.Value] = poolVertices = [];
                                concavePoolFaces[pool.Value] = poolFaces = [];
                            }
                            else
                            {
                                poolFaces = concavePoolFaces[pool.Value];
                            }
                        }
                        else
                        {
                            poolVertices = defaultConcaveVertices;
                            poolFaces = defaultConcaveFaces;
                        }

                        foreach (int index in face.Indices)
                        {
                            poolVertices.Add(EntityTransform(face.Vertices[index]));
                        }

                        poolFaces.Add(face);
                    }

                    if (IsClip(face))
                    {
                        continue;
                    }

                    metadata.AddFace(face, textureIndex, EntityTransform);

                    for (int i = 0; i < face.Vertices.Length; i++)
                    {
                        Vector3 vertex = face.Vertices[i];

                        vertices.Add(EntityTransform(vertex));
                        normals.Add(FuncGodotUtil.IdToOpenGl(face.Normals[i]));
                        uvs.Add(FuncGodotUtil.GetFaceVertexUv(vertex, face, GetTextureSize(face.Texture)));

                        for (int j = 0; j < 4; j++)
                        {
                            tangents.Add(face.Tangents[(i * 4) + j]);
                        }
                    }

                    foreach (int index in face.Indices)
                    {
                        indices.Add(index + indexOffset);
                    }

                    indexOffset += face.Vertices.Length;
                }

                // Tool textures never become a visual surface.
                if (FuncGodotUtil.FilterFace(textureName, _mapSettings) || vertices.Count == 0)
                {
                    continue;
                }

                surfaceTextures.Add(textureName);
                surfaceArrays.Add(BuildSurfaceArrays(vertices, normals, tangents, uvs, indices));
            }

            if (definition.BuildVisuals)
            {
                BuildVisualMesh(entity, definition, surfaceTextures, surfaceArrays, metadata);
                BuildShadowMesh(entity, shadowSurfaces, EntityTransform);
            }

            if (entity.IsCollisionConvex())
            {
                BuildConvexCollision(entity, definition, metadata, EntityTransform);
            }
            else if (buildConcave)
            {
                BuildConcaveCollision(
                    entity, definition, metadata,
                    concavePools, concavePoolFaces,
                    defaultConcaveVertices, defaultConcaveFaces);
            }

            metadata.Apply(entity);
        }

        private static Godot.Collections.Array BuildSurfaceArrays(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<float> tangents,
            List<Vector2> uvs,
            List<int> indices)
        {
            Godot.Collections.Array arrays = [];
            arrays.Resize((int)Mesh.ArrayType.Max);

            arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
            arrays[(int)Mesh.ArrayType.Normal] = normals.ToArray();
            arrays[(int)Mesh.ArrayType.Tangent] = tangents.ToArray();
            arrays[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
            arrays[(int)Mesh.ArrayType.Index] = indices.ToArray();

            return arrays;
        }

        private void BuildVisualMesh(
            FuncGodotData.EntityData entity,
            FuncGodotFGDSolidClass definition,
            List<string> surfaceTextures,
            List<Godot.Collections.Array> surfaceArrays,
            MeshMetadataBuilder metadata)
        {
            if (surfaceArrays.Count == 0)
            {
                return;
            }

            ArrayMesh mesh = new();

            for (int index = 0; index < surfaceArrays.Count; index++)
            {
                mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArrays[index]);
                mesh.SurfaceSetName(index, surfaceTextures[index]);

                Material material = GetMaterial(surfaceTextures[index]);

                if (material != null)
                {
                    mesh.SurfaceSetMaterial(index, material);
                }
            }

            entity.Mesh = mesh;
        }

        /// A shadows-only mesh built from the faces textured with the shadow texture.
        private void BuildShadowMesh(
            FuncGodotData.EntityData entity,
            Dictionary<string, List<FuncGodotData.FaceData>> shadowSurfaces,
            Func<Vector3, Vector3> entityTransform)
        {
            if (shadowSurfaces.Count == 0)
            {
                return;
            }

            ArrayMesh shadowMesh = new();

            foreach (KeyValuePair<string, List<FuncGodotData.FaceData>> surface in shadowSurfaces)
            {
                List<Vector3> vertices = [];
                List<Vector3> normals = [];
                List<float> tangents = [];
                List<Vector2> uvs = [];
                List<int> indices = [];

                int indexOffset = 0;

                foreach (FuncGodotData.FaceData face in surface.Value)
                {
                    if (face.Vertices.Length < 3)
                    {
                        continue;
                    }

                    for (int i = 0; i < face.Vertices.Length; i++)
                    {
                        Vector3 vertex = face.Vertices[i];

                        vertices.Add(entityTransform(vertex));
                        normals.Add(FuncGodotUtil.IdToOpenGl(face.Normals[i]));
                        uvs.Add(FuncGodotUtil.GetFaceVertexUv(vertex, face, GetTextureSize(face.Texture)));

                        for (int j = 0; j < 4; j++)
                        {
                            tangents.Add(face.Tangents[(i * 4) + j]);
                        }
                    }

                    foreach (int index in face.Indices)
                    {
                        indices.Add(index + indexOffset);
                    }

                    indexOffset += face.Vertices.Length;
                }

                if (vertices.Count == 0)
                {
                    continue;
                }

                shadowMesh.AddSurfaceFromArrays(
                    Mesh.PrimitiveType.Triangles,
                    BuildSurfaceArrays(vertices, normals, tangents, uvs, indices));

                int surfaceIndex = shadowMesh.GetSurfaceCount() - 1;
                shadowMesh.SurfaceSetName(surfaceIndex, surface.Key);

                Material material = GetMaterial(surface.Key);

                if (material != null)
                {
                    shadowMesh.SurfaceSetMaterial(surfaceIndex, material);
                }
            }

            if (shadowMesh.GetSurfaceCount() > 0)
            {
                entity.ShadowMesh = shadowMesh;
            }
        }

        #endregion

        #region INTERIOR FACE CULLING

        private static Dictionary<Vector4I, List<FuncGodotData.FaceData>> BuildInteriorFaceLookup(
            List<FuncGodotData.FaceData> faces)
        {
            Dictionary<Vector4I, List<FuncGodotData.FaceData>> lookup = [];

            foreach (FuncGodotData.FaceData face in faces)
            {
                Vector4I key = GetPlaneLookupKey(face.Plane);

                if (!lookup.TryGetValue(key, out List<FuncGodotData.FaceData> coplanar))
                {
                    coplanar = [];
                    lookup[key] = coplanar;
                }

                coplanar.Add(face);
            }

            return lookup;
        }

        /// <summary>
        /// True when the face is completely covered by an opposing coplanar face, meaning it is sealed inside
        /// the entity and can never be seen.
        /// </summary>
        private static bool IsInteriorFace(
            FuncGodotData.FaceData face,
            Dictionary<Vector4I, List<FuncGodotData.FaceData>> lookup)
        {
            Plane opposite = new(-face.Plane.Normal, -face.Plane.D);

            if (!lookup.TryGetValue(GetPlaneLookupKey(opposite), out List<FuncGodotData.FaceData> candidates))
            {
                return false;
            }

            foreach (FuncGodotData.FaceData other in candidates)
            {
                if (ReferenceEquals(face, other))
                {
                    continue;
                }

                if (!other.Plane.HasPoint(face.Plane.GetCenter(), 0.0001f))
                {
                    continue;
                }

                if (!(-face.Plane.Normal).IsEqualApprox(other.Plane.Normal))
                {
                    continue;
                }

                // Identical vertices: the faces are back to back.
                bool allVerticesShared = true;

                foreach (Vector3 vertex in face.Vertices)
                {
                    if (Array.IndexOf(other.Vertices, vertex) < 0)
                    {
                        allVerticesShared = false;
                        break;
                    }
                }

                if (allVerticesShared)
                {
                    return true;
                }

                // Otherwise the face is interior only if every one of its vertices lands inside the other face.
                bool allVerticesCovered = true;

                foreach (Vector3 vertex in face.Vertices)
                {
                    if (!IsVertexOnFace(vertex, other))
                    {
                        allVerticesCovered = false;
                        break;
                    }
                }

                if (allVerticesCovered)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsVertexOnFace(Vector3 vertex, FuncGodotData.FaceData face)
        {
            Vector3 from = vertex - (face.Plane.Normal * 0.001f);
            Vector3 to = face.Plane.Normal * 0.001f;

            for (int i = 0; i < face.Indices.Length / 3; i++)
            {
                Variant intersection = Geometry3D.RayIntersectsTriangle(
                    from,
                    to,
                    face.Vertices[face.Indices[i * 3]],
                    face.Vertices[face.Indices[(i * 3) + 1]],
                    face.Vertices[face.Indices[(i * 3) + 2]]);

                if (intersection.VariantType != Variant.Type.Nil)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region COLLISION

        /// One convex shape per brush, tagged with the surface type its faces declare.
        private void BuildConvexCollision(
            FuncGodotData.EntityData entity,
            FuncGodotFGDSolidClass definition,
            MeshMetadataBuilder metadata,
            Func<Vector3, Vector3> entityTransform)
        {
            foreach (FuncGodotData.BrushData brush in entity.Brushes)
            {
                if (brush.Planes.Count == 0 || brush.Origin)
                {
                    continue;
                }

                Vector3[] points = Geometry3D.ComputeConvexMeshPoints([.. brush.Planes]);

                if (points.Length == 0)
                {
                    continue;
                }

                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = entityTransform(points[i]);
                }

                ConvexPolygonShape3D shape = new()
                {
                    Points = points,
                };

                entity.Shapes.Add(shape);
                entity.ShapeSurfaceTypes.Add(GetBrushSurfaceType(brush));

                metadata.AddShape(brush.Faces);
            }
        }

        /// <summary>
        /// One concave shape per surface type. Faces whose material declares no surface type collect into the
        /// untyped default pool.
        /// </summary>
        private static void BuildConcaveCollision(
            FuncGodotData.EntityData entity,
            FuncGodotFGDSolidClass definition,
            MeshMetadataBuilder metadata,
            Dictionary<UnaryStandartMaterial3D.SurfaceType, List<Vector3>> concavePools,
            Dictionary<UnaryStandartMaterial3D.SurfaceType, List<FuncGodotData.FaceData>> concavePoolFaces,
            List<Vector3> defaultPoolVertices,
            List<FuncGodotData.FaceData> defaultPoolFaces)
        {
            void AddShape(
                UnaryStandartMaterial3D.SurfaceType? surfaceType,
                List<Vector3> vertices,
                List<FuncGodotData.FaceData> faces)
            {
                if (vertices.Count == 0)
                {
                    return;
                }

                ConcavePolygonShape3D shape = new();
                shape.SetFaces([.. vertices]);

                entity.Shapes.Add(shape);
                entity.ShapeSurfaceTypes.Add(surfaceType);

                metadata.AddShape(faces);
            }

            foreach (KeyValuePair<UnaryStandartMaterial3D.SurfaceType, List<Vector3>> pool in concavePools)
            {
                AddShape(pool.Key, pool.Value, concavePoolFaces[pool.Key]);
            }

            // Faces with no surface type build a single untyped shape, recorded with a null surface type.
            AddShape(null, defaultPoolVertices, defaultPoolFaces);
        }

        #endregion

        #region UV2

        private void UnwrapUv2(int entityIndex, float texelSize)
        {
            FuncGodotData.EntityData entity = _entityData[entityIndex];

            // Smoothed meshes are re-created during entity assembly, so they are unwrapped there instead.
            if (entity.Mesh == null
                || !entity.IsGiEnabled()
                || entity.IsSmoothShaded(_mapSettings.EntitySmoothingProperty))
            {
                return;
            }

            entity.Mesh.LightmapUnwrap(Transform3D.Identity, texelSize);
        }

        #endregion

        /// <summary>
        /// Accumulates the optional per-face metadata a solid class can request. Face data is recorded per
        /// triangle so every array stays parallel with the mesh's triangles.
        /// </summary>
        private sealed class MeshMetadataBuilder
        {
            private readonly FuncGodotFGDSolidClass _definition;

            private readonly List<StringName> _textureNames = [];
            private readonly List<int> _textures = [];
            private readonly List<Vector3> _vertices = [];
            private readonly List<Vector3> _normals = [];
            private readonly List<Vector3> _positions = [];

            /// Triangle indices owned by each face, used to map collision shapes back onto mesh faces.
            private readonly Dictionary<FuncGodotData.FaceData, List<int>> _faceTriangles = [];
            private readonly List<int[]> _shapeToFace = [];

            private int _triangleIndex = 0;

            public MeshMetadataBuilder(FuncGodotFGDSolidClass definition)
            {
                _definition = definition;
            }

            public int AddTexture(string textureName)
            {
                if (!_definition.AddTexturesMetadata)
                {
                    return 0;
                }

                _textureNames.Add(textureName);
                return _textureNames.Count - 1;
            }

            public void AddFace(FuncGodotData.FaceData face, int textureIndex, Func<Vector3, Vector3> entityTransform)
            {
                int triangleCount = face.Indices.Length / 3;

                if (_definition.AddTexturesMetadata)
                {
                    for (int i = 0; i < triangleCount; i++)
                    {
                        _textures.Add(textureIndex);
                    }
                }

                if (_definition.AddFaceNormalMetadata)
                {
                    Vector3 normal = FuncGodotUtil.IdToOpenGl(face.Plane.Normal);

                    for (int i = 0; i < triangleCount; i++)
                    {
                        _normals.Add(normal);
                    }
                }

                if (_definition.AddFacePositionMetadata)
                {
                    for (int i = 0; i < triangleCount; i++)
                    {
                        Vector3[] triangle =
                        [
                            face.Vertices[face.Indices[i * 3]],
                            face.Vertices[face.Indices[(i * 3) + 1]],
                            face.Vertices[face.Indices[(i * 3) + 2]],
                        ];

                        _positions.Add(entityTransform(FuncGodotUtil.Vec3Average(triangle)));
                    }
                }

                if (_definition.AddVertexMetadata)
                {
                    foreach (int index in face.Indices)
                    {
                        _vertices.Add(entityTransform(face.Vertices[index]));
                    }
                }

                if (_definition.AddCollisionShapeToFaceIndicesMetadata)
                {
                    List<int> triangles = [];

                    for (int i = 0; i < triangleCount; i++)
                    {
                        triangles.Add(_triangleIndex + i);
                    }

                    _faceTriangles[face] = triangles;
                }

                _triangleIndex += triangleCount;
            }

            /// Records which mesh triangles the collision shape just built covers.
            public void AddShape(List<FuncGodotData.FaceData> faces)
            {
                if (!_definition.AddCollisionShapeToFaceIndicesMetadata)
                {
                    return;
                }

                List<int> triangles = [];

                foreach (FuncGodotData.FaceData face in faces)
                {
                    if (_faceTriangles.TryGetValue(face, out List<int> faceTriangles))
                    {
                        triangles.AddRange(faceTriangles);
                    }
                }

                _shapeToFace.Add([.. triangles]);
            }

            public void Apply(FuncGodotData.EntityData entity)
            {
                if (_definition.AddTexturesMetadata)
                {
                    Godot.Collections.Array textureNames = [];

                    foreach (StringName textureName in _textureNames)
                    {
                        textureNames.Add(textureName);
                    }

                    entity.MeshMetadata["texture_names"] = textureNames;
                    entity.MeshMetadata["textures"] = _textures.ToArray();
                }

                if (_definition.AddVertexMetadata)
                {
                    entity.MeshMetadata["vertices"] = _vertices.ToArray();
                }

                if (_definition.AddFaceNormalMetadata)
                {
                    entity.MeshMetadata["normals"] = _normals.ToArray();
                }

                if (_definition.AddFacePositionMetadata)
                {
                    entity.MeshMetadata["positions"] = _positions.ToArray();
                }

                if (_definition.AddCollisionShapeToFaceIndicesMetadata)
                {
                    // Entity assembly rewrites this into a map keyed by CollisionShape3D node name.
                    Godot.Collections.Array shapeToFace = [];

                    foreach (int[] triangles in _shapeToFace)
                    {
                        shapeToFace.Add(triangles);
                    }

                    entity.MeshMetadata["shape_to_face_array"] = shapeToFace;
                }
            }
        }
    }
}
