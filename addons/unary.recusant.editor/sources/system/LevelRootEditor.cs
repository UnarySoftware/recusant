#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
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

            EditorInterface.Singleton.MarkSceneAsUnsaved();
        }

        public void AddGroup()
        {
            if (!IsInGroup(LevelRootGroup))
            {
                AddToGroup(LevelRootGroup, true);
            }
        }

        public void InitializeNodes()
        {
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

        public void BuildNavigation()
        {
            Task.Run(StartBuildNavigation);
        }

        public void ResetNavigation()
        {
            Task.Run(StartResetNavigation);
        }

        private async Task StartResetNavigation()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            AddGroup();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            _region = GetNodeOrNull<NavigationRegion3D>("Navigation");

            if (_region == null)
            {
                PluginLogger.Critical(this, $"Failed to aquire a {nameof(NavigationRegion3D)} node to reset navigation");
                return;
            }

            VisualPaths.Value = null;
            FromStartToFinish.Value = null;

            VertexDistance = null;
            Polys = null;
            PolyFlags = null;
            Bounds = null;
            BoundsCount = null;
            BoundsPolys = null;

            _region.NavigationMesh.Clear();
            ResourceSaver.Singleton.Save(_region.NavigationMesh);

            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            _region.UpdateGizmos();
            UpdateGizmos();

            EditorInterface.Singleton.MarkSceneAsUnsaved();
        }

        private async Task StartBuildNavigation()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            AddGroup();

            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            _region = null;
            VisualPaths.Value = null;
            FromStartToFinish.Value = null;

            _region = GetNodeOrNull<NavigationRegion3D>("Navigation");

            if (_region == null)
            {
                PluginLogger.Critical(this, $"Failed to aquire a {nameof(NavigationRegion3D)} node to build a navigation");
                return;
            }

            _region.BakeFinished += OnBakeFinished;
            _region.BakeNavigationMesh(true);
        }

        private void Failed(string why)
        {
            VisualPaths.Value = null;
            _region?.UpdateGizmos();
            PluginLogger.Critical(this, $"Failed to build navigation data.\nReason: {why}");
            EditorInterface.Singleton.MarkSceneAsUnsaved();
        }

        private void OnBakeFinished()
        {
            _region.BakeFinished -= OnBakeFinished;
            Task.Run(OnBakeFinishedAsync);
        }

        private int OriginalPolyCount = 0;
        private int ResultPolyCount = 0;
        private int MaxPolyPerBound = 0;
        private int InvalidatedGroups = 0;
        private int Groups = 0;

        private readonly Lock ValidGroupLock = new();
        private readonly Lock InvalidGroupLock = new();

        private struct PolyData : IComparable<PolyData>
        {
            public Vector3I Vertexes;
            public Aabb Box;
            public int Flag;

            public float AverageFlow;

            public readonly int CompareTo(PolyData other)
            {
                return -AverageFlow.CompareTo(other.AverageFlow);
            }
        }

        private async Task OnBakeFinishedAsync()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            Rid map = _region.GetNavigationMap();
            NavigationMesh navMesh = _region.NavigationMesh;

            Godot.Collections.Array<Node> markerNodes = GetTree().GetNodesInGroup(PlayerMarker.PlayerMarkerGroup);

            List<PlayerMarker> markers = [];
            List<PlayerMarker> startMarkers = [];
            List<PlayerMarker> endMarkers = [];

            foreach (var markerNode in markerNodes)
            {
                if (markerNode is PlayerMarker marker)
                {
                    markers.Add(marker);

                    if (marker.Type == PlayerMarker.MarkerType.Start)
                    {
                        startMarkers.Add(marker);
                    }
                    else if (marker.Type == PlayerMarker.MarkerType.End)
                    {
                        endMarkers.Add(marker);
                    }
                }
            }

            if (markers.Count == 0)
            {
                Failed("Missing PlayerMarkers on a level");
                return;
            }

            if (startMarkers.Count == 0)
            {
                Failed("Missing PlayerMarker with a Start type");
                return;
            }

            if (endMarkers.Count == 0)
            {
                Failed("Missing PlayerMarker with an End type");
                return;
            }

            Godot.Collections.Array<Node> navBrushNodes = GetTree().GetNodesInGroup(NavBrush.NavBrushGroup);

            List<NavBrush> brushes = [];
            List<NavBrush> startBrushes = [];
            List<NavBrush> endBrushes = [];

            foreach (var navBrushNode in navBrushNodes)
            {
                if (navBrushNode is NavBrush brush)
                {
                    brushes.Add(brush);

                    if (brush.Flags.HasFlag(NavBrush.Flag.Start))
                    {
                        startBrushes.Add(brush);
                    }

                    if (brush.Flags.HasFlag(NavBrush.Flag.End))
                    {
                        endBrushes.Add(brush);
                    }
                }
            }

            if (brushes.Count == 0)
            {
                Failed("Missing NavBrushes on a level");
                return;
            }

            if (startBrushes.Count == 0)
            {
                Failed("Missing NavBrush with a Start type");
                return;
            }

            if (endBrushes.Count == 0)
            {
                Failed("Missing NavBrush with an End type");
                return;
            }

            Vector3[] vertices = navMesh.GetVertices();

            int polyCount = navMesh.GetPolygonCount();
            OriginalPolyCount = polyCount;
            InvalidatedGroups = 0;
            Groups = 0;

            if (OriginalPolyCount == 0)
            {
                Failed("Failed to get polygons out of the NavMesh");
                return;
            }

            Vector3I[] polygons = new Vector3I[polyCount];

            for (int i = 0; i < polyCount; i++)
            {
                var polygon = navMesh.GetPolygon(i);
                polygons[i] = new(polygon[0], polygon[1], polygon[2]);
            }

            GD.Seed((ulong)startMarkers.Count);
            PlayerMarker startIndex = startMarkers[GD.RandRange(0, startMarkers.Count - 1)];

            GD.Seed((ulong)endMarkers.Count);
            PlayerMarker endIndex = endMarkers[GD.RandRange(0, endMarkers.Count - 1)];

            Vector3 startPosition = startIndex.GlobalPosition;
            Vector3 startPositionResolved = NavigationServer3D.Singleton.MapGetClosestPoint(map, startPosition);

            if (startPosition.DistanceTo(startPositionResolved) > PlayerConstants.PlayerRadius * 2.0f)
            {
                FromStartToFinish.Value =
                [
                    startPosition,
                    startPositionResolved
                ];

                Failed($"PlayerMarker \"{startIndex.Name}\" with a Start type is too far away from a NavMesh");
                UpdateGizmos();
                _region.UpdateGizmos();
                return;
            }

            Vector3 endPosition = endIndex.GlobalPosition;
            Vector3 endPositionResolved = NavigationServer3D.Singleton.MapGetClosestPoint(map, endPosition);

            if (endPosition.DistanceTo(endPositionResolved) > PlayerConstants.PlayerRadius * 2.0f)
            {
                FromStartToFinish.Value =
                [
                    endPosition,
                    endPositionResolved
                ];

                Failed($"PlayerMarker \"{endIndex.Name}\" with an End type is too far away from a NavMesh");
                UpdateGizmos();
                _region.UpdateGizmos();
                return;
            }

            NavigationPathQueryParameters3D pathParams = new()
            {
                Map = map,
                PathSearchMaxPolygons = 0,
                StartPosition = startPositionResolved,
                TargetPosition = endPositionResolved,
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

            Vector3[] points = result.Path;

            for (int i = 0; i < points.Length; i++)
            {
                points[i].Y += 0.01f;
            }

            FromStartToFinish.Value = points;

            if (result.Path[0].DistanceTo(startPositionResolved) > PathMargin ||
                result.Path[^1].DistanceTo(endPositionResolved) > PathMargin)
            {
                Failed("Failed to find a path from a start to the end");
                UpdateGizmos();
                _region.UpdateGizmos();
                return;
            }

            var filteredPolygons = new List<PolyData>();
            Vector3[] newVerticesArray;

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

                List<VisualPath> ValidPaths = new();

                Multi.Thread(0, polygonGroups.Count, (i, state) =>
                {
                    List<int> entries = polygonGroups[i];

                    if (entries.Count == 0)
                    {
                        return;
                    }

                    Vector3I poly = polygons[entries[0]];

                    Vector3 realStart = (vertices[poly.X] + vertices[poly.Y] + vertices[poly.Z]) / 3.0f;
                    Vector3 resolvedStart = NavigationServer3D.Singleton.MapGetClosestPoint(map, realStart);

                    if (realStart.DistanceTo(resolvedStart) > PathMargin)
                    {
                        foreach (var entry in entries)
                        {
                            invalidatedIndexes[entry] = true;
                        }

                        lock (InvalidGroupLock)
                        {
                            InvalidatedGroups++;
                        }

                        return;
                    }

                    NavigationPathQueryParameters3D input = new()
                    {
                        Map = map,
                        PathSearchMaxPolygons = 0,
                        StartPosition = resolvedStart,
                        TargetPosition = endPositionResolved,
                        SimplifyPath = true,
                        PathPostprocessing = NavigationPathQueryParameters3D.PathPostProcessing.Corridorfunnel
                    };

                    NavigationPathQueryResult3D output = new();

                    NavigationServer3D.Singleton.QueryPath(input, output);

                    if (output.Path == null ||
                    output.Path.Length == 0 ||
                    output.Path[0].DistanceTo(resolvedStart) > PathMargin ||
                    output.Path[^1].DistanceTo(endPositionResolved) > PathMargin)
                    {
                        foreach (var entry in entries)
                        {
                            invalidatedIndexes[entry] = true;
                        }

                        lock (InvalidGroupLock)
                        {
                            InvalidatedGroups++;
                        }
                    }
                    else
                    {
                        resolvedStart.Y += 0.01f;

                        VisualPath newPath = new()
                        {
                            Points = output.Path,
                            RealStart = realStart,
                            ResolvedStart = resolvedStart
                        };

                        lock (ValidGroupLock)
                        {
                            ValidPaths.Add(newPath);
                            Groups++;
                        }
                    }
                });

                VisualPaths.Value = ValidPaths;

                // Step 3: Collect referenced vertices
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
                newVerticesArray = new Vector3[referencedIndices.Count];

                for (int i = 0; i < refIndices.Length; i++)
                {
                    int oldIdx = refIndices[i];
                    newVerticesArray[i] = vertices[oldIdx];
                }

                for (int i = 0; i < polyCount; i++)
                {
                    if (!invalidatedIndexes[i])
                    {
                        var poly = polygons[i];

                        PolyData data = new()
                        {
                            AverageFlow = -1.0f,
                            Vertexes =
                            new(oldToNewIndexMap[poly.X],
                            oldToNewIndexMap[poly.Y],
                            oldToNewIndexMap[poly.Z]),
                        };

                        Aabb polyAabb = new(newVerticesArray[data.Vertexes.X], Vector3.Zero);
                        polyAabb = polyAabb.Expand(newVerticesArray[data.Vertexes.Y]);
                        polyAabb = polyAabb.Expand(newVerticesArray[data.Vertexes.Z]);

                        data.Box = polyAabb;

                        filteredPolygons.Add(data);
                    }
                }

                VertexDistance = new float[newVerticesArray.Length];

                Multi.Thread(0, newVerticesArray.Length, (i, state) =>
                {
                    Vector3 vertex = newVerticesArray[i];
                    Vector3 resolvedStart = NavigationServer3D.Singleton.MapGetClosestPoint(map, vertex);

                    if (vertex.DistanceTo(resolvedStart) > PathMargin)
                    {
                        VertexDistance[i] = -1.0f;
                        return;
                    }

                    NavigationPathQueryParameters3D input = new()
                    {
                        Map = map,
                        PathSearchMaxPolygons = 0,
                        StartPosition = resolvedStart,
                        TargetPosition = endPositionResolved,
                        SimplifyPath = true,
                        PathPostprocessing = NavigationPathQueryParameters3D.PathPostProcessing.Corridorfunnel
                    };

                    NavigationPathQueryResult3D output = new();

                    NavigationServer3D.Singleton.QueryPath(input, output);

                    if (output.Path == null ||
                    output.Path.Length == 0 ||
                    output.Path[0].DistanceTo(resolvedStart) > PathMargin ||
                    output.Path[^1].DistanceTo(endPositionResolved) > PathMargin)
                    {
                        VertexDistance[i] = -1.0f;
                    }
                    else
                    {
                        VertexDistance[i] = output.PathLength;
                    }
                });

                for (int i = 0; i < filteredPolygons.Count; i++)
                {
                    PolyData data = filteredPolygons[i];

                    float vertexFlow1 = VertexDistance[data.Vertexes.X];

                    if (vertexFlow1 == -1.0f)
                    {
                        return;
                    }

                    float vertexFlow2 = VertexDistance[data.Vertexes.Y];

                    if (vertexFlow2 == -1.0f)
                    {
                        return;
                    }

                    float vertexFlow3 = VertexDistance[data.Vertexes.Z];

                    if (vertexFlow3 == -1.0f)
                    {
                        return;
                    }

                    data.AverageFlow = (vertexFlow1 + vertexFlow2 + vertexFlow3) / 3.0f;

                    filteredPolygons[i] = data;
                }

                filteredPolygons.Sort();

                navMesh.Clear();

                navMesh.SetVertices(newVerticesArray);

                ResultPolyCount = filteredPolygons.Count;

                int[] passed = new int[3];

                for (int i = 0; i < filteredPolygons.Count; i++)
                {
                    var poly = filteredPolygons[i];
                    passed[0] = poly.Vertexes.X;
                    passed[1] = poly.Vertexes.Y;
                    passed[2] = poly.Vertexes.Z;

                    navMesh.AddPolygon(passed);
                }
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

                        Aabb probeBox = new(new()
                        {
                            X = position.X - BoundsSize / 2.0f,
                            Y = position.Y - BoundsSize / 2.0f,
                            Z = position.Z - BoundsSize / 2.0f,
                        }, sizeBox);

                        for (int i = 0; i < filteredPolygons.Count; i++)
                        {
                            if (probeBox.Intersects(filteredPolygons[i].Box))
                            {
                                if (!boundToPoly.TryGetValue(position, out var entries))
                                {
                                    entries = [];
                                    boundToPoly[position] = entries;
                                }

                                entries.Add(i);
                            }
                        }

                        // This bound has no triangles associated with it, skipping
                        if (!boundToPoly.TryGetValue(position, out var addedEntries))
                        {
                            continue;
                        }

                        foreach (var brush in brushes)
                        {
                            // Current brush does not intersect with this probe, skip it
                            if (!probeBox.Intersects(brush.GetAabb()))
                            {
                                continue;
                            }

                            foreach (var polyEntry in addedEntries)
                            {
                                var poly = filteredPolygons[polyEntry];

                                // This poly does not intersect with this brush, skip it
                                if (!Triangle.IntersectsBounds(newVerticesArray[poly.Vertexes.X],
                                newVerticesArray[poly.Vertexes.Y],
                                newVerticesArray[poly.Vertexes.Z],
                                brush.GetAabb()))
                                {
                                    continue;
                                }

                                poly.Flag |= (int)brush.Flags;

                                filteredPolygons[polyEntry] = poly;
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

            Polys = new int[filteredPolygons.Count * 3];
            PolyFlags = new int[filteredPolygons.Count];

            int polyWriteIndex = 0;

            for (int i = 0; i < filteredPolygons.Count; i++)
            {
                var target = filteredPolygons[i];

                Polys[polyWriteIndex] = target.Vertexes.X;
                Polys[polyWriteIndex + 1] = target.Vertexes.Y;
                Polys[polyWriteIndex + 2] = target.Vertexes.Z;

                PolyFlags[i] = target.Flag;

                polyWriteIndex += 3;
            }

            ResourceSaver.Singleton.Save(_region.NavigationMesh);

            UpdateGizmos();
            _region.UpdateGizmos();

            int percentage = ResultPolyCount * 100 / OriginalPolyCount;

            string additionalMessage = string.Empty;

            if (OriginalPolyCount == ResultPolyCount)
            {
                additionalMessage = "\n\nIt seems that the post processing failed to decimate any unnecessary polys from the NavMesh indicating a timing issue" +
                    " with the algoritm yet again. Please report this on GitHub.";
            }

            PluginLogger.Critical(this, $"Successfully build navigation!\n" +
                $"Poly count reduction result: {OriginalPolyCount} - {OriginalPolyCount - ResultPolyCount} = {ResultPolyCount} (-{100 - percentage}%)\n" +
                $"Remaining groups: {Groups} Invalidated groups: {InvalidatedGroups}\n" +
                $"Bound count: {Bounds.Length} Max polygons per bound: {MaxPolyPerBound}" +
                additionalMessage);

            EditorInterface.Singleton.MarkSceneAsUnsaved();
        }
    }
}

#endif
