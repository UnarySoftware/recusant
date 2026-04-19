using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class NavMeshManager : Node, IModSystem
    {
        private struct PolyData
        {
            public Vector3I Vertex;
            public NavBrush.Flag Flags;
        }

        private LevelRoot _levelRoot;
        private Vector3[] _vertices;
        private int _polyCount;
        private PolyData[] _polyData;
        private readonly Dictionary<Vector3, int[]> _boundToPolys = [];

        bool ISystem.Initialize()
        {
            LevelManager.Singleton.OnLoaded.Subscribe(OnLoaded, this);
            LevelManager.Singleton.OnUnloaded.Subscribe(OnUnloaded, this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            LevelManager.Singleton.OnLoaded.Unsubscribe(this);
            LevelManager.Singleton.OnUnloaded.Unsubscribe(this);
        }

        private bool OnLoaded(ref LevelManager.LevelInfo data)
        {
            _levelRoot = data.Root;
            _vertices = _levelRoot.NavigationMesh.GetVertices();
            _polyCount = _levelRoot.NavigationMesh.GetPolygonCount();
            _polyData = new PolyData[_polyCount];

            int polyReader = 0;

            for (int i = 0; i < _polyCount; i++)
            {
                _polyData[i] = new()
                {
                    Vertex = new()
                    {
                        X = _levelRoot.Polys[polyReader],
                        Y = _levelRoot.Polys[polyReader + 1],
                        Z = _levelRoot.Polys[polyReader + 2],
                    },
                    Flags = (NavBrush.Flag)_levelRoot.PolyFlags[i],
                };

                polyReader += 3;
            }

            int entryCounter = 0;

            for (int i = 0; i < _levelRoot.Bounds.Length; i++)
            {
                Vector3 bound = _levelRoot.Bounds[i];
                int boundEntryCount = _levelRoot.BoundsCount[i];
                int[] entries = new int[boundEntryCount];

                for (int k = 0; k < boundEntryCount; k++)
                {
                    entries[k] = _levelRoot.BoundsPolys[entryCounter];
                    entryCounter++;
                }

                _boundToPolys[bound] = entries;
            }

            return true;
        }

        public (float flow, NavBrush.Flag flags, int triangle) GetFlow(Vector3 position)
        {
            if (_levelRoot == null)
            {
                this.Error("Requested flow while we failed to aquire a level root");
                return (-1.0f, 0, -1);
            }

            Vector3 key = new()
            {
                X = Mathf.Round(position.X / _levelRoot.BoundsSize) * _levelRoot.BoundsSize,
                Y = Mathf.Round(position.Y / _levelRoot.BoundsSize) * _levelRoot.BoundsSize,
                Z = Mathf.Round(position.Z / _levelRoot.BoundsSize) * _levelRoot.BoundsSize
            };

            if (!_boundToPolys.TryGetValue(key, out var entries))
            {
                return (-1.0f, 0, -1);
            }

            foreach (var entry in entries)
            {
                var data = _polyData[entry];

                Vector3 x = _vertices[data.Vertex.X];
                Vector3 y = _vertices[data.Vertex.Y];
                Vector3 z = _vertices[data.Vertex.Z];

                float distance = Triangle.GetPointDistance(x, y, z, position);

                // Mathf.IsEqualApprox is giving off false positives here... I know, crazy right?
                if (distance == 0.0f)
                {
                    Vector3 barycentric = Triangle.GetBarycentricCoords(x, y, z, position);

                    if (barycentric != default)
                    {
                        return (barycentric.X * _levelRoot.VertexDistance[data.Vertex.X] +
                            barycentric.Y * _levelRoot.VertexDistance[data.Vertex.Y] +
                            barycentric.Z * _levelRoot.VertexDistance[data.Vertex.Z],
                            data.Flags, entry);
                    }
                }
            }

            return (-1.0f, 0, -1);
        }

        private bool OnUnloaded(ref LevelManager.LevelInfo data)
        {
            _levelRoot = null;
            _vertices = null;
            _polyCount = 0;
            _polyData = null;
            _boundToPolys.Clear();
            return true;
        }
    }
}
