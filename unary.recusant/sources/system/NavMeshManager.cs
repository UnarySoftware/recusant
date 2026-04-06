using Godot;
using System.Collections.Generic;
using System.Linq;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class NavMeshManager : Node, IModSystem
    {
        private LevelRoot _levelRoot;
        private Vector3[] _vertices;
        private int _polyCount;
        private Vector3I[] _polyIndices;
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
            _polyIndices = new Vector3I[_polyCount];

            for (int i = 0; i < _polyCount; i++)
            {
                var indices = _levelRoot.NavigationMesh.GetPolygon(i);

                _polyIndices[i] = new()
                {
                    X = indices[0],
                    Y = indices[1],
                    Z = indices[2],
                };
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

        public float GetFlow(Vector3 position)
        {
            if (_levelRoot == null)
            {
                this.Error("Requested flow while we failed to aquire a level root");
                return -1.0f;
            }

            Vector3 key = new()
            {
                X = Mathf.Round(position.X / _levelRoot.BoundsSize) * _levelRoot.BoundsSize,
                Y = Mathf.Round(position.Y / _levelRoot.BoundsSize) * _levelRoot.BoundsSize,
                Z = Mathf.Round(position.Z / _levelRoot.BoundsSize) * _levelRoot.BoundsSize
            };

            if (!_boundToPolys.TryGetValue(key, out var entries))
            {
                return -1.0f;
            }

            foreach (var entry in entries)
            {
                var indices = _polyIndices[entry];

                Vector3 x = _vertices[indices.X];
                Vector3 y = _vertices[indices.Y];
                Vector3 z = _vertices[indices.Z];

                float distance = Triangle.GetPointDistance(x, y, z, position);

                // Mathf.IsEqualApprox is giving off false positives here... I know, crazy right?
                if (distance == 0.0f)
                {
                    Vector3 barycentric = Triangle.GetPointInside(x, y, z, position);

                    if (barycentric != default)
                    {
                        return barycentric.X * _levelRoot.VertexDistance[indices.X] +
                            barycentric.Y * _levelRoot.VertexDistance[indices.Y] +
                            barycentric.Z * _levelRoot.VertexDistance[indices.Z];
                    }
                }
            }

            return -1.0f;
        }

        private bool OnUnloaded(ref LevelManager.LevelInfo data)
        {
            _levelRoot = null;
            _vertices = null;
            _polyCount = 0;
            _polyIndices = null;
            _boundToPolys.Clear();
            return true;
        }
    }
}
