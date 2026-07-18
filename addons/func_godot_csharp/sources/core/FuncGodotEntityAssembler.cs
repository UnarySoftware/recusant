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
    /// Turns generated entity data into the actual SceneTree: one node per entity, plus the mesh, collision,
    /// and occluder children each definition asks for.
    /// </summary>
    public sealed class FuncGodotEntityAssembler
    {
        public const string Signature = "[ENT]";

        private readonly FuncGodotMapSettings _mapSettings;
        private FuncGodotMap.BuildFlagBits _buildFlags;

        public event Action<string> DeclareStep;

        private void Step(string step)
        {
            DeclareStep?.Invoke(step);
        }

        public FuncGodotEntityAssembler(FuncGodotMapSettings mapSettings)
        {
            _mapSettings = mapSettings;
        }

        /// <summary>
        /// Generates the group nodes, then every entity node, and parents them under the map node.
        /// </summary>
        public void Build(
            FuncGodotMap mapNode,
            List<FuncGodotData.EntityData> entities,
            List<FuncGodotData.GroupData> groups)
        {
            _buildFlags = mapNode.BuildFlags;

            // Everything the build creates has to be owned by the edited scene to be saved with it.
            Node sceneRoot = mapNode.IsInsideTree()
                ? mapNode.GetTree().EditedSceneRoot ?? mapNode
                : mapNode;

            if (_mapSettings.UseGroupsHierarchy)
            {
                Step($"Generating {groups.Count} groups");

                foreach (FuncGodotData.GroupData group in groups)
                {
                    group.Node = new Node3D
                    {
                        Name = group.Name,
                    };
                }

                foreach (FuncGodotData.GroupData group in groups)
                {
                    if (group.ParentId < 0)
                    {
                        mapNode.AddChild(group.Node);
                        group.Node.Owner = sceneRoot;
                        continue;
                    }

                    foreach (FuncGodotData.GroupData parent in groups)
                    {
                        if (group.ParentId != parent.Id)
                        {
                            continue;
                        }

                        parent.Node.AddChild(group.Node);
                        group.Node.Owner = sceneRoot;
                        break;
                    }
                }

                Step("Groups generation and sorting complete");
            }

            Step($"Assembling {entities.Count} entities");

            for (int entityIndex = 0; entityIndex < entities.Count; entityIndex++)
            {
                FuncGodotData.EntityData entityData = entities[entityIndex];
                Node entityNode = GenerateEntityNode(entityData, entityIndex);

                if (entityNode == null)
                {
                    continue;
                }

                if (!_mapSettings.UseGroupsHierarchy || entityData.Group == null)
                {
                    mapNode.AddChild(entityNode);

                    // Worldspawn stays the first child, so the map reads top down.
                    if (entityIndex == 0)
                    {
                        mapNode.MoveChild(entityNode, 0);
                    }
                }
                else
                {
                    entityData.Group.Node.AddChild(entityNode);
                }

                entityNode.Owner = sceneRoot;

                if (entityData.MeshInstance != null)
                {
                    entityData.MeshInstance.Owner = sceneRoot;
                }

                if (entityData.ShadowMeshInstance != null)
                {
                    entityData.ShadowMeshInstance.Owner = sceneRoot;
                }

                foreach (CollisionShape3D shape in entityData.CollisionShapes)
                {
                    shape.Owner = sceneRoot;
                }

                if (entityData.OccluderInstance != null)
                {
                    entityData.OccluderInstance.Owner = sceneRoot;
                }

                ApplyEntityProperties(entityNode, entityData);
            }

            Step("Entity assembly and property application complete");
        }

        /// Loads a Script by its global class name, so node_class can name a script class.
        private static Script GetScriptByClassName(string className)
        {
            if (ResourceLoader.Exists(className, "Script"))
            {
                return ResourceLoader.Load<Script>(className);
            }

            foreach (Godot.Collections.Dictionary globalClass in ProjectSettings.Singleton.GetGlobalClassList())
            {
                if (globalClass["class"].AsString() != className)
                {
                    continue;
                }

                return ResourceLoader.Load<Script>(globalClass["path"].AsString());
            }

            return null;
        }

        /// Instantiates the node named by an entity definition's NodeClass.
        private static Node InstantiateNodeClass(string nodeClass)
        {
            if (string.IsNullOrEmpty(nodeClass))
            {
                return null;
            }

            if (ClassDB.Singleton.ClassExists(nodeClass))
            {
                return ClassDB.Singleton.Instantiate(nodeClass).As<Node>();
            }

            Script script = GetScriptByClassName(nodeClass);

            return script switch
            {
                CSharpScript cSharpScript => cSharpScript.New().As<Node>(),
                _ => null,
            };
        }

        /// A name prefixed with "%" becomes a unique name in its owner, matching Godot's own syntax.
        private static void NameNode(Node node, string nodeName)
        {
            if (nodeName.StartsWith('%'))
            {
                node.Name = nodeName.TrimPrefix("%");
                node.UniqueNameInOwner = true;
                return;
            }

            node.Name = nodeName;
        }

        private Node GenerateEntityNode(FuncGodotData.EntityData entityData, int entityIndex)
        {
            // Suffix the index with the classname so the node communicates its type, e.g. "0_worldspawn".
            string nodeName = entityIndex.ToString();

            if (entityData.Properties.TryGetValue("classname", out Variant classname))
            {
                nodeName += "_" + classname.AsString();
            }

            FuncGodotFGDEntityClass definition = entityData.Definition;

            string nameProperty = string.Empty;

            if (!string.IsNullOrEmpty(definition.NameProperty)
                && entityData.Properties.TryGetValue(definition.NameProperty, out Variant definitionName))
            {
                nameProperty = definitionName.ToString();
            }
            else if (!string.IsNullOrEmpty(_mapSettings.EntityNameProperty)
                && entityData.Properties.TryGetValue(_mapSettings.EntityNameProperty, out Variant settingsName))
            {
                nameProperty = settingsName.ToString();
            }

            if (!string.IsNullOrEmpty(nameProperty))
            {
                nodeName = nameProperty;
            }

            Node node = definition switch
            {
                FuncGodotFGDSolidClass solidClass => GenerateSolidEntityNode(nodeName, entityData, solidClass),
                FuncGodotFGDPointClass pointClass => GeneratePointEntityNode(nodeName, entityData, pointClass),
                _ => null,
            };

            if (node == null)
            {
                return null;
            }

            Script scriptClass = definition switch
            {
                FuncGodotFGDSolidClass solidClass => solidClass.ScriptClass,
                FuncGodotFGDPointClass pointClass => pointClass.ScriptClass,
                _ => null,
            };

            // A scene file brings its own scripts, so only a bare node class gets one attached.
            if (scriptClass != null && (definition is not FuncGodotFGDPointClass point || point.SceneFile == null))
            {
                node.SetScript(scriptClass);
            }

            foreach (string nodeGroup in _mapSettings.EntityNodeGroups)
            {
                if (!string.IsNullOrEmpty(nodeGroup))
                {
                    node.AddToGroup(nodeGroup, true);
                }
            }

            foreach (string nodeGroup in definition.NodeGroups)
            {
                if (!string.IsNullOrEmpty(nodeGroup))
                {
                    node.AddToGroup(nodeGroup, true);
                }
            }

            return node;
        }

        #region SOLID ENTITIES

        private Node GenerateSolidEntityNode(
            string nodeName,
            FuncGodotData.EntityData data,
            FuncGodotFGDSolidClass definition)
        {
            // Merged entities donated their brushes to worldspawn during parsing and generate no node.
            if (definition.SpawnType == FuncGodotFGDSolidClass.SpawnTypes.MergeWorldspawn)
            {
                return null;
            }

            Node node = InstantiateNodeClass(definition.NodeClass) ?? new Node3D();

            NameNode(node, nodeName);

            if (data.Mesh != null)
            {
                MeshInstance3D meshInstance = new()
                {
                    Name = "mesh_instance",
                    Mesh = data.Mesh,
                    GIMode = definition.GlobalIlluminationMode,
                    CastShadow = definition.ShadowCastingSetting,
                    Layers = definition.RenderLayers,
                };

                node.AddChild(meshInstance);
                data.MeshInstance = meshInstance;

                if (definition.BuildOcclusion)
                {
                    BuildOccluder(node, data);
                }

                // Smoothing rebuilds the mesh, so it has to happen before the UV2 unwrap that follows it.
                if (!_buildFlags.HasFlag(FuncGodotMap.BuildFlagBits.DisableSmoothing)
                    && data.IsSmoothShaded(_mapSettings.EntitySmoothingProperty))
                {
                    meshInstance.Mesh = FuncGodotUtil.SmoothMeshByAngle(
                        data.Mesh,
                        data.GetSmoothingAngle(_mapSettings.EntitySmoothingAngleProperty));

                    if (data.IsGiEnabled() && _buildFlags.HasFlag(FuncGodotMap.BuildFlagBits.UnwrapUv2))
                    {
                        (meshInstance.Mesh as ArrayMesh)?.LightmapUnwrap(
                            Transform3D.Identity,
                            _mapSettings.UvUnwrapTexelSize * _mapSettings.ScaleFactor);
                    }
                }
            }

            if (data.ShadowMesh != null)
            {
                MeshInstance3D shadowMeshInstance = new()
                {
                    Name = "shadow_mesh_instance",
                    Mesh = data.ShadowMesh,
                    GIMode = GeometryInstance3D.GIModeEnum.Disabled,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly,
                    Layers = definition.RenderLayers,
                };

                node.AddChild(shadowMeshInstance);
                data.ShadowMeshInstance = shadowMeshInstance;
            }

            if (data.Shapes.Count > 0 && node is CollisionObject3D collisionObject)
            {
                BuildCollision(collisionObject, data, definition);
            }

            if (node is Node3D node3D)
            {
                node3D.Position = FuncGodotUtil.IdToOpenGl(data.Origin);
            }
            else if (node is Node2D node2D)
            {
                node2D.Position = new Vector2(data.Origin.Z, -data.Origin.Y) * _mapSettings.InverseScaleFactor;
            }

            if (data.MeshMetadata.Count > 0)
            {
                node.SetMeta("func_godot_mesh_data", data.MeshMetadata);
            }

            return node;
        }

        private static void BuildOccluder(Node node, FuncGodotData.EntityData data)
        {
            List<Vector3> vertices = [];
            List<int> indices = [];

            for (int surfaceIndex = 0; surfaceIndex < data.Mesh.GetSurfaceCount(); surfaceIndex++)
            {
                Godot.Collections.Array surface = data.Mesh.SurfaceGetArrays(surfaceIndex);

                int vertexOffset = vertices.Count;

                vertices.AddRange(surface[(int)Mesh.ArrayType.Vertex].AsVector3Array());

                foreach (int index in surface[(int)Mesh.ArrayType.Index].AsInt32Array())
                {
                    indices.Add(index + vertexOffset);
                }
            }

            ArrayOccluder3D occluder = new();
            occluder.SetArrays([.. vertices], [.. indices]);

            OccluderInstance3D occluderInstance = new()
            {
                Name = "occluder_instance",
                Occluder = occluder,
            };

            node.AddChild(occluderInstance);
            data.OccluderInstance = occluderInstance;
        }

        /// <summary>
        /// Creates a <see cref="StaticCollisionShape3D"/> per generated shape, carrying its surface type so
        /// gameplay code can tell what it hit. Shapes whose faces declared no surface type - tool textures like
        /// clip that build collision without a material - carry <see cref="UnaryStandartMaterial3D.SurfaceType.None"/>.
        /// </summary>
        private static void BuildCollision(
            CollisionObject3D node,
            FuncGodotData.EntityData data,
            FuncGodotFGDSolidClass definition)
        {
            node.CollisionLayer = definition.CollisionLayer;
            node.CollisionMask = definition.CollisionMask;
            node.CollisionPriority = definition.CollisionPriority;

            Godot.Collections.Array shapeToFaceArray = [];

            if (data.MeshMetadata.TryGetValue("shape_to_face_array", out Variant shapeToFace))
            {
                shapeToFaceArray = shapeToFace.AsGodotArray();
                data.MeshMetadata.Remove("shape_to_face_array");
            }

            Godot.Collections.Dictionary faceIndexMetadata = [];

            bool isConcave = definition.CollisionShapeType == FuncGodotFGDSolidClass.CollisionShapeTypes.Concave;

            for (int i = 0; i < data.Shapes.Count; i++)
            {
                // Absent or untyped shapes (tool textures like clip) collapse to None rather than an untyped shape.
                UnaryStandartMaterial3D.SurfaceType surfaceType = (i < data.ShapeSurfaceTypes.Count
                    ? data.ShapeSurfaceTypes[i]
                    : null) ?? UnaryStandartMaterial3D.SurfaceType.None;

                CollisionShape3D collisionShape = new StaticCollisionShape3D
                {
                    Type = surfaceType,
                };

                string prefix = surfaceType.ToString().ToLower() + "_";

                // Concave collision produces one shape per surface type, convex one shape per brush.
                collisionShape.Name = isConcave
                    ? prefix + "collision_shape"
                    : prefix + $"brush_{i}_collision_shape";

                data.Shapes[i].Margin = definition.CollisionShapeMargin;
                collisionShape.Shape = data.Shapes[i];

                node.AddChild(collisionShape);
                data.CollisionShapes.Add(collisionShape);

                if (shapeToFaceArray.Count > i)
                {
                    faceIndexMetadata[collisionShape.Name] = shapeToFaceArray[i];
                }
            }

            if (definition.AddCollisionShapeToFaceIndicesMetadata)
            {
                data.MeshMetadata["collision_shape_to_face_indices_map"] = faceIndexMetadata;
            }
        }

        #endregion

        #region POINT ENTITIES

        private Node GeneratePointEntityNode(
            string nodeName,
            FuncGodotData.EntityData data,
            FuncGodotFGDPointClass definition)
        {
            Node node;

            if (definition.SceneFile != null)
            {
                PackedScene.GenEditState editState = Engine.Singleton.IsEditorHint()
                    ? PackedScene.GenEditState.Instance
                    : PackedScene.GenEditState.Disabled;

                node = definition.SceneFile.Instantiate(editState);
            }
            else
            {
                node = InstantiateNodeClass(definition.NodeClass);
            }

            node ??= new Node3D();

            NameNode(node, nodeName);

            Dictionary<string, Variant> properties = data.Properties;

            if (node is Node3D node3D && definition.ApplyRotationOnMapBuild)
            {
                node3D.RotationDegrees = GetPointEntityRotation(properties, definition);
            }

            if (definition.ApplyScaleOnMapBuild)
            {
                ApplyPointEntityScale(node, properties);
            }

            ApplyPointEntityOrigin(node, properties, nodeName);

            return node;
        }

        /// <summary>
        /// Reads the rotation from <c>angles</c>, <c>mangle</c>, or <c>angle</c>, in that order. Lights and
        /// info_intermission read mangle in their own component order, a Quake quirk.
        /// </summary>
        private static Vector3 GetPointEntityRotation(
            Dictionary<string, Variant> properties,
            FuncGodotFGDPointClass definition)
        {
            Vector3 angles = Vector3.Zero;

            bool hasAngles = properties.ContainsKey("angles");
            bool hasMangle = properties.ContainsKey("mangle");

            if (hasAngles || hasMangle)
            {
                string key = hasAngles ? "angles" : "mangle";

                if (TryReadVector3(properties[key], out Vector3 raw))
                {
                    angles = new Vector3(-raw.X, raw.Y, -raw.Z);

                    if (key == "mangle")
                    {
                        if (definition.Classname.StartsWith("light"))
                        {
                            angles = new Vector3(raw.Y, raw.X, -raw.Z);
                        }
                        else if (definition.Classname == "info_intermission")
                        {
                            angles = new Vector3(raw.X, raw.Y, -raw.Z);
                        }
                    }
                }
                else
                {
                    GD.PushError($"Invalid vector format for \"{key}\" in entity \"{definition.Classname}\"");
                }
            }
            else if (properties.TryGetValue("angle", out Variant angleValue))
            {
                float angle = angleValue.AsSingle();

                // -1 and -2 are Quake's "straight up" and "straight down".
                if (Mathf.IsEqualApprox(angle, -1.0f))
                {
                    angles.X = 90.0f;
                }
                else if (Mathf.IsEqualApprox(angle, -2.0f))
                {
                    angles.X = -90.0f;
                }
                else
                {
                    angles.Y += angle;
                }
            }

            angles.Y += 180.0f;

            return angles;
        }

        private static void ApplyPointEntityScale(Node node, Dictionary<string, Variant> properties)
        {
            if (!properties.TryGetValue("scale", out Variant scaleProperty))
            {
                return;
            }

            // The scale property may be a single float, a Vector2, or a Vector3, in map coordinate order.
            if (scaleProperty.VariantType == Variant.Type.String)
            {
                string[] components = scaleProperty.AsString().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                scaleProperty = components.Length switch
                {
                    1 => components[0].ToFloat(),
                    2 => new Vector2(components[0].ToFloat(), components[0].ToFloat()),
                    3 => new Vector3(components[1].ToFloat(), components[2].ToFloat(), components[0].ToFloat()),
                    _ => scaleProperty,
                };
            }

            bool isScalar = scaleProperty.VariantType is Variant.Type.Float or Variant.Type.Int;

            if (node is Node3D node3D)
            {
                if (isScalar)
                {
                    node3D.Scale *= scaleProperty.AsSingle();
                }
                else if (scaleProperty.VariantType is Variant.Type.Vector3 or Variant.Type.Vector3I)
                {
                    node3D.Scale *= scaleProperty.AsVector3();
                }

                return;
            }

            if (node is Node2D node2D)
            {
                if (isScalar)
                {
                    node2D.Scale *= scaleProperty.AsSingle();
                }
                else if (scaleProperty.VariantType is Variant.Type.Vector2 or Variant.Type.Vector2I)
                {
                    node2D.Scale *= scaleProperty.AsVector2();
                }
            }
        }

        private void ApplyPointEntityOrigin(Node node, Dictionary<string, Variant> properties, string nodeName)
        {
            if (!properties.TryGetValue("origin", out Variant originProperty))
            {
                return;
            }

            if (!TryReadVector3(originProperty, out Vector3 raw))
            {
                GD.PushError($"Invalid vector format for \"origin\" in {nodeName}");
                return;
            }

            // Map coordinates are (x forward, y right, z up); Godot's are (y, z, x) of those.
            Vector3 origin = new(raw.Y, raw.Z, raw.X);

            if (node is Node3D node3D)
            {
                node3D.Position = origin * _mapSettings.ScaleFactor;
            }
            else if (node is Node2D node2D)
            {
                node2D.Position = new Vector2(origin.Z, -origin.Y);
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

        #region PROPERTIES

        /// <summary>
        /// Hands the entity's properties to the generated node: onto matching node properties when the
        /// definition opts in, into a <c>func_godot_properties</c> member, and through the
        /// <c>_func_godot_apply_properties</c> and <c>_func_godot_build_complete</c> callbacks.
        /// </summary>
        private static void ApplyEntityProperties(Node node, FuncGodotData.EntityData data)
        {
            Dictionary<string, Variant> properties = data.Properties;
            FuncGodotFGDEntityClass definition = data.Definition;

            Godot.Collections.Dictionary godotProperties = [];

            foreach (KeyValuePair<string, Variant> property in properties)
            {
                godotProperties[property.Key] = property.Value;
            }

            if (definition != null && definition.AutoApplyToMatchingNodeProperties)
            {
                bool appliesScale = definition is FuncGodotFGDPointClass pointClass && pointClass.ApplyScaleOnMapBuild;

                foreach (KeyValuePair<string, Variant> property in properties)
                {
                    // Scale was already applied to the node itself.
                    if (property.Key == "scale" && appliesScale)
                    {
                        continue;
                    }

                    ApplyMatchingNodeProperty(node, property.Key, property.Value);
                }
            }

            if (HasProperty(node, "func_godot_properties"))
            {
                node.Set("func_godot_properties", godotProperties);
            }

            if (node.HasMethod("_func_godot_apply_properties"))
            {
                node.Call("_func_godot_apply_properties", godotProperties);
            }

            if (node.HasMethod("_func_godot_build_complete"))
            {
                node.CallDeferred("_func_godot_build_complete");
            }
        }

        private static void ApplyMatchingNodeProperty(Node node, string property, Variant value)
        {
            if (!HasProperty(node, property))
            {
                return;
            }

            Variant current = node.Get(property);

            if (current.VariantType == value.VariantType)
            {
                node.Set(property, value);
                return;
            }

            if (value.VariantType != Variant.Type.String)
            {
                GD.PushError($"Entity {node.Name} property '{property}' type mismatch with matching generated node property.");
                return;
            }

            switch (current.VariantType)
            {
                case Variant.Type.String:
                case Variant.Type.StringName:
                    {
                        node.Set(property, value);
                        break;
                    }
                case Variant.Type.NodePath:
                    {
                        node.Set(property, new NodePath(value.AsString()));
                        break;
                    }
                default:
                    {
                        GD.PushError($"Entity {node.Name} property '{property}' type mismatch with matching generated node property.");
                        break;
                    }
            }
        }

        private static bool HasProperty(Node node, string property)
        {
            foreach (Godot.Collections.Dictionary entry in node.GetPropertyList())
            {
                if (entry["name"].AsString() == property)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
