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
    /// Containers that hold the data passed between the parsing, geometry generation, and entity assembly
    /// stages of a <see cref="FuncGodotMap"/> build. Every struct here is a reference type, so the stages
    /// mutate a single shared graph rather than copying it between steps.
    /// </summary>
    public static class FuncGodotData
    {
        /// <summary>
        /// A single map plane and the mesh face generated from it. Vertices only exist once the face has been
        /// clipped against the other planes of its brush during geometry generation.
        /// </summary>
        public sealed class FaceData
        {
            public Vector3[] Vertices = [];
            public int[] Indices = [];

            /// Defaults to the planar normal, which yields flat shading. Smoothing rewrites these per vertex.
            public Vector3[] Normals = [];
            public float[] Tangents = [];

            /// Texture path without extension, relative to the map settings' base texture directory.
            public string Texture = string.Empty;

            /// UV offset in the origin, UV scale in the basis. Valve 220 stores rotation in the axes instead.
            public Transform2D Uv = Transform2D.Identity;

            /// The Valve 220 texture axes, parsed straight from the map file.
            public List<Vector3> UvAxes = [];

            /// Raw plane data in the id Tech coordinate system.
            public Plane Plane;

            /// Average of all vertices. Only valid once the face has been clipped.
            public Vector3 GetCentroid()
            {
                return FuncGodotUtil.Vec3Average(Vertices);
            }

            /// An arbitrary coplanar direction to wind the face around.
            public Vector3 GetBasis()
            {
                if (Vertices.Length < 2)
                {
                    GD.PushError("Cannot get winding basis without at least 2 vertices!");
                    return Vector3.Zero;
                }

                return (Vertices[1] - Vertices[0]).Normalized();
            }

            /// Sorts the vertices by angle around the centroid, giving OpenGL triangle winding order.
            public void Wind()
            {
                if (Vertices.Length < 3)
                {
                    return;
                }

                Vector3 centroid = GetCentroid();
                Vector3 uAxis = GetBasis();
                Vector3 vAxis = uAxis.Cross(Plane.Normal).Normalized();

                Array.Sort(Vertices, (a, b) =>
                {
                    Vector3 dirA = a - centroid;
                    Vector3 dirB = b - centroid;
                    float angleA = Mathf.Atan2(dirA.Dot(vAxis), dirA.Dot(uAxis));
                    float angleB = Mathf.Atan2(dirB.Dot(vAxis), dirB.Dot(uAxis));
                    return angleA.CompareTo(angleB);
                });
            }

            /// Builds a triangle fan over the wound vertices.
            public void IndexVertices()
            {
                int triangleCount = Mathf.Max(Vertices.Length - 2, 0);
                Indices = new int[triangleCount * 3];

                int index = 0;

                for (int i = 0; i < triangleCount; i++)
                {
                    Indices[index] = 0;
                    Indices[index + 1] = i + 1;
                    Indices[index + 2] = i + 2;
                    index += 3;
                }
            }
        }

        /// A single map brush, primarily a container for its faces.
        public sealed class BrushData
        {
            public List<Plane> Planes = [];
            public List<FaceData> Faces = [];

            /// True when every face of the brush uses the origin texture from the map settings.
            public bool Origin = false;
        }

        /// A TrenchBroom Group or Layer.
        public sealed class GroupData
        {
            public enum GroupType
            {
                Group,
                Layer
            }

            public GroupType Type = GroupType.Group;

            /// Id from the map file, used to resolve both group membership and group parenting.
            public int Id;

            /// Generated as type_id_name, e.g. group_2_Arkham.
            public string Name = string.Empty;

            public int ParentId = -1;
            public GroupData Parent = null;
            public Node3D Node = null;

            /// Set by TrenchBroom's "omit layer from export" option. Omitted groups and their entities are
            /// dropped at the end of parsing, before anything is built.
            public bool Omit = false;
        }

        /// A single map entity, and everything the build stages hang off it.
        public sealed class EntityData
        {
            /// Key/value pairs straight from the map file. Values start as strings and are converted to the
            /// types declared by the entity definition at the end of parsing.
            public Dictionary<string, Variant> Properties = [];

            public List<BrushData> Brushes = [];
            public GroupData Group = null;

            /// Resolved from the FGD by matching classname. Always a solid or point class once parsing ends.
            public FuncGodotFGDEntityClass Definition = null;

            public ArrayMesh Mesh = null;
            public MeshInstance3D MeshInstance = null;

            /// Faces textured with the shadow texture, split off into a shadow-only mesh.
            public ArrayMesh ShadowMesh = null;
            public MeshInstance3D ShadowMeshInstance = null;

            public Godot.Collections.Dictionary MeshMetadata = [];

            public List<Shape3D> Shapes = [];

            /// Parallel to <see cref="Shapes"/>. Holds the surface type each shape was tagged with, or null
            /// for shapes whose faces declared no surface type.
            public List<UnaryStandartMaterial3D.SurfaceType?> ShapeSurfaceTypes = [];

            public List<CollisionShape3D> CollisionShapes = [];
            public OccluderInstance3D OccluderInstance = null;

            /// Global position of the generated node. Mesh vertices are offset by this during generation.
            public Vector3 Origin = Vector3.Zero;

            public bool IsVisual()
            {
                return Definition is FuncGodotFGDSolidClass solid && solid.BuildVisuals;
            }

            public bool IsGiEnabled()
            {
                return Definition is FuncGodotFGDSolidClass solid
                    && solid.GlobalIlluminationMode != GeometryInstance3D.GIModeEnum.Disabled;
            }

            public bool IsCollisionConvex()
            {
                return Definition is FuncGodotFGDSolidClass solid
                    && solid.CollisionShapeType == FuncGodotFGDSolidClass.CollisionShapeTypes.Convex;
            }

            public bool IsCollisionConcave()
            {
                return Definition is FuncGodotFGDSolidClass solid
                    && solid.CollisionShapeType == FuncGodotFGDSolidClass.CollisionShapeTypes.Concave;
            }

            public bool IsSmoothShaded(string smoothingProperty = "_phong")
            {
                if (!Properties.TryGetValue(smoothingProperty, out Variant value))
                {
                    return false;
                }

                return value.AsInt32() != 0;
            }

            public float GetSmoothingAngle(string smoothingAngleProperty = "_phong_angle")
            {
                if (!Properties.TryGetValue(smoothingAngleProperty, out Variant value))
                {
                    return 89.0f;
                }

                return value.AsSingle();
            }
        }

        /// Every face a single welded vertex position appears in, used when smoothing normals.
        public sealed class VertexGroupData
        {
            public List<FaceData> Faces = [];
            public List<int> FaceIndices = [];
        }

        public sealed class ParseData
        {
            public List<EntityData> Entities = [];
            public List<GroupData> Groups = [];
        }
    }
}
