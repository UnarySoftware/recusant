#if TOOLS

using Godot;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unary.Core;
using Unary.Core.Editor;

namespace Unary.Recusant
{
    public partial class LevelRoot : Node3D
    {
        private static T CreateResource<T>(string path, T resource) where T : Resource, new()
        {
            ResourceSaver.Singleton.Save(resource, path);
            return (T)ResourceLoader.Singleton.Load(path);
        }

        private void CreateResources(string targetDirectory)
        {
            Node owner = GetTree().EditedSceneRoot;

            LightmapGIData lightmapData = CreateResource<LightmapGIData>(targetDirectory + "/lightmap.lmbake", new());

            LightmapGI lightmapGi = new()
            {
                Name = "Lightmap",
                LightData = lightmapData,
                Supersampling = true
            };

            AddChild(lightmapGi);
            lightmapGi.Owner = owner;

            ArrayOccluder3D occluderData = CreateResource<ArrayOccluder3D>(targetDirectory + "/occlusion.occ", new());

            OccluderInstance3D occlusion = new()
            {
                Name = "Occlusion",
                Occluder = occluderData
            };

            AddChild(occlusion);
            occlusion.Owner = owner;

            NavigationMesh navigationData = CreateResource<NavigationMesh>(targetDirectory + "/navigation.tres", new()
            {
                CellSize = PlayerConstants.NavCellSize,
                CellHeight = PlayerConstants.NavCellHeight,
                AgentHeight = PlayerConstants.NavAgentHeight,
                AgentRadius = PlayerConstants.NavAgentRadius,
                AgentMaxClimb = PlayerConstants.NavAgentMaxClimb,
                VerticesPerPolygon = PlayerConstants.NavMaxVerticesPerPolygon,
                GeometrySourceGeometryMode = NavigationMesh.SourceGeometryMode.GroupsWithChildren,
                GeometrySourceGroupName = LevelRootGroup,
                GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders
            });

            NavigationRegion3D navigation = new()
            {
                Name = "Navigation",
                NavigationMesh = navigationData
            };

            AddChild(navigation);
            navigation.Owner = owner;

            DirectionalLight3D light = new()
            {
                Name = "GlobalLight"
            };

            AddChild(light);
            light.Owner = owner;
        }

        public void InitializeNodes()
        {
            if (!IsInGroup(LevelRootGroup))
            {
                AddToGroup(LevelRootGroup, true);
            }

            if (GetChildCount() > 0)
            {
                return;
            }

            string targetPath = EditorInterface.Singleton.GetEditedSceneRoot().SceneFilePath.ToLower().Replace("res://", "").Replace('\\', '/');

            if (string.IsNullOrEmpty(targetPath))
            {
                PluginLogger.Critical(this, "Failed to aquire path of the current scene");
                return;
            }

            string directory = Path.GetDirectoryName(targetPath).Replace('\\', '/');
            string file = Path.GetFileNameWithoutExtension(targetPath);

            if (!directory.EndsWith("levels"))
            {
                PluginLogger.Critical(this, $"Created {nameof(LevelRoot)} is outside of a proper levels folder, skipping node creation");
                return;
            }

            string targetDirectory = directory + '/' + file;

            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, true);
            }

            Directory.CreateDirectory(targetDirectory);

            CreateResources(targetDirectory);
        }

        private NavigationRegion3D _region;

        public async void BuildNavigation()
        {
            _region = null;
            Points = null;

            _region = GetNodeOrNull<NavigationRegion3D>("Navigation");

            if (_region == null)
            {
                PluginLogger.Critical(this, $"Failed to aquire a {nameof(NavigationRegion3D)} node to build a navigation");
                return;
            }

            _region.NavigationMesh.Clear();

            ResourceSaver.Singleton.Save(_region.NavigationMesh);

            _region.BakeFinished += OnBakeFinished;
            // Setting this to true seems to be causing weird ordering issues
            // Even though we are using the callback on completion some data is still being changed afterwards
            // Probably an engine bug
            _region.BakeNavigationMesh(false);
        }

        private void Failed(string why)
        {
            Points = null;
            _region?.UpdateGizmos();
            PluginLogger.Critical(this, $"Failed to build navigation data.\nReason: {why}");
        }

        private void OnBakeFinished()
        {
            _region.BakeFinished -= OnBakeFinished;
            CallDeferred(MethodName.OnBakeFinishedAsync);
        }

        private int OriginalPolyCount = 0;
        private int ResultPolyCount = 0;
        private int MaxPolyPerBound = 0;
        private int InvalidatedGroups = 0;

        private async void OnBakeFinishedAsync()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            ResourceSaver.Singleton.Save(_region.NavigationMesh);

            Godot.Collections.Array<Node> markerNodes = GetTree().GetNodesInGroup(PlayerMarker.PlayerMarkerGroup);

            PlayerMarker start = null;
            PlayerMarker end = null;

            foreach (var node in markerNodes)
            {
                if (node is PlayerMarker marker)
                {
                    if (start == null && marker.Type == PlayerMarker.MarkerType.Start)
                    {
                        start = marker;
                    }
                    else if (end == null && marker.Type == PlayerMarker.MarkerType.End)
                    {
                        end = marker;
                    }
                }
            }

            if (start == null)
            {
                Failed("Missing start PlayerMarker");
                return;
            }

            if (end == null)
            {
                Failed("Missing end PlayerMarker");
                return;
            }

            Rid map = _region.GetNavigationMap();

            Vector3 startPosition = NavigationServer3D.Singleton.MapGetClosestPoint(map, start.Position);

            Vector3 endPosition = NavigationServer3D.Singleton.MapGetClosestPoint(map, end.Position);

            NavigationPathQueryParameters3D pathParams = new()
            {
                Map = map,
                PathSearchMaxPolygons = 0,
                StartPosition = startPosition,
                TargetPosition = endPosition,
                SimplifyPath = true,
                PathPostprocessing = NavigationPathQueryParameters3D.PathPostProcessing.Corridorfunnel
            };

            NavigationPathQueryResult3D result = new();

            NavigationServer3D.Singleton.QueryPath(pathParams, result);

            if (result.Path == null || result.Path.Length == 0)
            {
                Failed("Failed to find a path from a start to the end");
                return;
            }

            Points = result.Path;

            if (result.Path[^1].DistanceTo(endPosition) > (PlayerConstants.PlayerRadius * 2.0f))
            {
                Failed("Failed to find a path from a start to the end");
                UpdateGizmos();
                _region.UpdateGizmos();
                return;
            }

            var navMesh = _region.NavigationMesh;

            Vector3[] vertices = navMesh.GetVertices();

            int polyCount = navMesh.GetPolygonCount();
            OriginalPolyCount = polyCount;
            InvalidatedGroups = 0;

            Vector3I[] polygons = new Vector3I[polyCount];

            for (int i = 0; i < polyCount; i++)
            {
                var polygon = navMesh.GetPolygon(i);
                polygons[i] = new(polygon[0], polygon[1], polygon[2]);
            }

            List<Aabb> boundingBoxes = [];

            {
                // Step 1: Partition all triangles into connected groups in order to do a connectivity check

                Dictionary<int, List<int>> vertexToPolygons = [];

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                void AddVertex(int v, int i)
                {
                    if (!vertexToPolygons.TryGetValue(v, out var entries))
                    {
                        entries = [];
                        vertexToPolygons[v] = entries;
                    }
                    entries.Add(i);
                }

                for (int i = 0; i < polyCount; i++)
                {
                    var poly = polygons[i];
                    AddVertex(poly.X, i);
                    AddVertex(poly.Y, i);
                    AddVertex(poly.Z, i);
                }

                // Build adjacency list for polygons
                List<int>[] adjacency = new List<int>[polyCount];

                for (int i = 0; i < polyCount; i++)
                {
                    adjacency[i] = [];
                }

                // For each vertex, connect all polygons sharing that vertex
                foreach (var kvp in vertexToPolygons)
                {
                    var polyList = kvp.Value;
                    for (int i = 0; i < polyList.Count; i++)
                    {
                        for (int j = i + 1; j < polyList.Count; j++)
                        {
                            int polyA = polyList[i];
                            int polyB = polyList[j];

                            adjacency[polyA].Add(polyB);
                            adjacency[polyB].Add(polyA);
                        }
                    }
                }

                // Find connected groups of polygons
                bool[] visited = new bool[polyCount];

                List<List<int>> polygonGroups = [];

                for (int i = 0; i < polyCount; i++)
                {
                    if (!visited[i])
                    {
                        List<int> group = [];
                        Queue<int> queue = [];
                        queue.Enqueue(i);
                        visited[i] = true;

                        while (queue.Count > 0)
                        {
                            int current = queue.Dequeue();
                            group.Add(current);

                            foreach (var neighbor in adjacency[current])
                            {
                                if (!visited[neighbor])
                                {
                                    visited[neighbor] = true;
                                    queue.Enqueue(neighbor);
                                }
                            }
                        }

                        polygonGroups.Add(group);
                    }
                }

                // Step 2: Check each group by taking a random member and trying to path from it to the end, if no path - the entire group is invalid
                bool[] invalidatedIndexes = new bool[polyCount];

                for (int i = 0; i < polygonGroups.Count; i++)
                {
                    List<int> entries = polygonGroups[i];

                    if (entries.Count == 0)
                    {
                        continue;
                    }

                    Vector3I poly = polygons[entries[0]];

                    pathParams.StartPosition = (vertices[poly.X] + vertices[poly.Y] + vertices[poly.Z]) / 3.0f;

                    NavigationServer3D.Singleton.QueryPath(pathParams, result);

                    if (result.Path == null || result.Path.Length == 0 || result.Path[^1].DistanceTo(endPosition) > (PlayerConstants.PlayerRadius * 2.0f))
                    {
                        InvalidatedGroups++;
                        foreach (var entry in entries)
                        {
                            invalidatedIndexes[entry] = true;
                        }
                    }
                }

                // Step 3: Collect referenced vertices concurrently
                HashSet<int> referencedIndices = [];

                for (int i = 0; i < polyCount; i++)
                {
                    if (!invalidatedIndexes[i])
                    {
                        var poly = polygons[i];
                        referencedIndices.Add(poly.X);
                        referencedIndices.Add(poly.Y);
                        referencedIndices.Add(poly.Z);
                    }
                }

                // Step 4: Map old indices to new indices
                Dictionary<int, int> oldToNewIndexMap = [];
                List<Vector3> newVertices = [];
                int[] refIndices = [.. referencedIndices];

                // Build mapping in a single thread (since dictionary is shared)
                for (int i = 0; i < refIndices.Length; i++)
                {
                    oldToNewIndexMap[refIndices[i]] = i;
                }

                // Step 5: Filter vertices
                // Create a new array of vertices based on referenced indices
                Vector3[] newVerticesArray = new Vector3[referencedIndices.Count];

                for (int i = 0; i < refIndices.Length; i++)
                {
                    int oldIdx = refIndices[i];
                    newVerticesArray[i] = vertices[oldIdx];
                }

                // Step 6: Update polygon indices in parallel with order preservation
                // Use thread-local buffers
                Vector3I[] polygonResults = new Vector3I[polyCount];

                for (int i = 0; i < polyCount; i++)
                {
                    if (!invalidatedIndexes[i])
                    {
                        var poly = polygons[i];
                        var newPoly = new Vector3I(
                            oldToNewIndexMap[poly.X],
                            oldToNewIndexMap[poly.Y],
                            oldToNewIndexMap[poly.Z]
                        );
                        polygonResults[i] = newPoly;
                    }
                    else
                    {
                        // Mark invalid
                        polygonResults[i] = default;
                    }
                }

                // Filter only valid polygons (non-default)
                var filteredPolygons = new List<Vector3I>(polyCount);

                for (int i = 0; i < polyCount; i++)
                {
                    if (polygonResults[i] != default)
                    {
                        filteredPolygons.Add(polygonResults[i]);
                    }
                }

                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

                navMesh.Clear();

                navMesh.SetVertices(newVerticesArray);

                ResultPolyCount = filteredPolygons.Count;

                int[] passed = new int[3];

                for (int i = 0; i < filteredPolygons.Count; i++)
                {
                    var poly = filteredPolygons[i];
                    passed[0] = poly.X;
                    passed[1] = poly.Y;
                    passed[2] = poly.Z;

                    Aabb polyAabb = new(newVerticesArray[poly.X], Vector3.Zero);
                    polyAabb = polyAabb.Expand(newVerticesArray[poly.Y]);
                    polyAabb = polyAabb.Expand(newVerticesArray[poly.Z]);

                    boundingBoxes.Add(polyAabb);

                    navMesh.AddPolygon(passed);
                }

                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

                VertexDistance = new float[newVerticesArray.Length];

                Parallel.For(0, newVerticesArray.Length, i =>
                {
                    Vector3 vertex = newVerticesArray[i];

                    NavigationPathQueryParameters3D pathParams = new()
                    {
                        Map = map,
                        PathSearchMaxPolygons = 0,
                        StartPosition = vertex,
                        TargetPosition = endPosition,
                        SimplifyPath = true,
                        PathPostprocessing = NavigationPathQueryParameters3D.PathPostProcessing.Corridorfunnel
                    };

                    NavigationPathQueryResult3D result = new();

                    NavigationServer3D.Singleton.QueryPath(pathParams, result);

                    if (result.Path == null || result.Path.Length == 0 || result.Path[^1].DistanceTo(endPosition) > (PlayerConstants.PlayerRadius * 2.0f))
                    {
                        VertexDistance[i] = -1.0f;
                    }
                    else
                    {
                        VertexDistance[i] = result.PathLength;
                    }
                });
            }

            Aabb aabb = NavigationServer3D.Singleton.RegionGetBounds(_region.GetRid());

            Vector3 probePosition = aabb.Position;

            probePosition.X = (Mathf.Round(probePosition.X / BoundsSize) - 1.0f) * BoundsSize;
            probePosition.Y = (Mathf.Round(probePosition.Y / BoundsSize) - 1.0f) * BoundsSize;
            probePosition.Z = (Mathf.Round(probePosition.Z / BoundsSize) - 1.0f) * BoundsSize;

            Vector3 probeEnd = aabb.End;

            probeEnd.X = (Mathf.Round(probeEnd.X / BoundsSize) + 1.0f) * BoundsSize;
            probeEnd.Y = (Mathf.Round(probeEnd.Y / BoundsSize) + 1.0f) * BoundsSize;
            probeEnd.Z = (Mathf.Round(probeEnd.Z / BoundsSize) + 1.0f) * BoundsSize;

            Dictionary<Vector3, HashSet<int>> boundToPoly = [];

            Vector3 sizeBox = new(BoundsSize, BoundsSize, BoundsSize);

            for (float x = probePosition.X; x < probeEnd.X; x += BoundsSize)
            {
                for (float y = probePosition.Y; y < probeEnd.Y; y += BoundsSize)
                {
                    for (float z = probePosition.Z; z < probeEnd.Z; z += BoundsSize)
                    {
                        Vector3 position = new(x, y, z);

                        Vector3 aabbPosition = new()
                        {
                            X = position.X - BoundsSize / 2.0f,
                            Y = position.Y - BoundsSize / 2.0f,
                            Z = position.Z - BoundsSize / 2.0f,
                        };

                        Aabb probeBox = new(aabbPosition, sizeBox);

                        for (int i = 0; i < boundingBoxes.Count; i++)
                        {
                            if (probeBox.Intersects(boundingBoxes[i]))
                            {
                                if (!boundToPoly.TryGetValue(position, out var entries))
                                {
                                    entries = [];
                                    boundToPoly[position] = entries;
                                }

                                entries.Add(i);
                            }
                        }
                    }
                }
            }

            Bounds = new Vector3[boundToPoly.Count];
            BoundsCount = new int[boundToPoly.Count];

            int index = 0;
            List<int> polyIndexes = [];

            foreach (var entry in boundToPoly)
            {
                Bounds[index] = entry.Key;
                BoundsCount[index] = entry.Value.Count;

                foreach (var poly in entry.Value)
                {
                    polyIndexes.Add(poly);
                }

                if (entry.Value.Count > MaxPolyPerBound)
                {
                    MaxPolyPerBound = entry.Value.Count;
                }

                index++;
            }

            BoundsPolys = [.. polyIndexes];

            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            ResourceSaver.Singleton.Save(_region.NavigationMesh);

            UpdateGizmos();
            _region.UpdateGizmos();

            int percentage = ResultPolyCount * 100 / OriginalPolyCount;

            PluginLogger.Critical(this, $"Successfully build navigation!\n" +
                $"Poly count reduction result: {OriginalPolyCount} - {OriginalPolyCount - ResultPolyCount} = {ResultPolyCount} (-{100 - percentage}%)\n" +
                $"Invalidated island groups: {InvalidatedGroups}\n" +
                $"Bound count: {Bounds.Length} Max polygons per bound: {MaxPolyPerBound}");
        }
    }
}

#endif
