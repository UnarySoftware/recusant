using Godot;

namespace Unary.Recusant
{
    public partial class Rope : MeshInstance3D
    {
        private static Godot.Collections.Array _sufraceArrayCache = [];

        private Vector3[] _verticesCache;
        private Vector3[] _normalsCache;
        private Vector2[] _uvCache;
        private int[] _indicesCache;
        private Vector3[] _spineCache;

        private static void TryBuildingCache()
        {
            if (_sufraceArrayCache.Count != 0)
            {
                return;
            }

            _sufraceArrayCache = new Godot.Collections.Array();
            _sufraceArrayCache.Resize((int)Mesh.ArrayType.Max);
        }

        private void PassArraysToSurface()
        {
            _sufraceArrayCache[(int)Mesh.ArrayType.Vertex] = _verticesCache;
            _sufraceArrayCache[(int)Mesh.ArrayType.Normal] = _normalsCache;
            _sufraceArrayCache[(int)Mesh.ArrayType.TexUV] = _uvCache;
            _sufraceArrayCache[(int)Mesh.ArrayType.Index] = _indicesCache;
        }

        private void SetupVerticesCache(int size)
        {
            if (_verticesCache == null || _verticesCache.Length != size)
            {
                _verticesCache = new Vector3[size];
            }
        }

        private void SetupNormalsCache(int size)
        {
            if (_normalsCache == null || _normalsCache.Length != size)
            {
                _normalsCache = new Vector3[size];
            }
        }

        private void SetupUvCache(int size)
        {
            if (_uvCache == null || _uvCache.Length != size)
            {
                _uvCache = new Vector2[size];
            }
        }

        private void SetupIndicesCache(int size)
        {
            if (_indicesCache == null || _indicesCache.Length != size)
            {
                _indicesCache = new int[size];
            }
        }

        private void SetupSpineCache(int size)
        {
            if (_spineCache == null || _spineCache.Length != size)
            {
                _spineCache = new Vector3[size];
            }
        }
    }
}
