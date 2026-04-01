using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    public class PoolGroup
    {
        private readonly string _poolId = string.Empty;

        private readonly Queue<Node3D> _entries = [];

        private readonly Node _root;

        public PoolGroup(int count, string poolId, Node root, PackedScene prefab)
        {
            _poolId = poolId;
            _root = root;

            for (int i = 0; i < count; i++)
            {
                Node3D newObject = (Node3D)prefab.Instantiate();

                if (newObject is IPoolable poolable)
                {
                    poolable.Release();
                }

                newObject.Visible = false;

                _root.AddChild(newObject);
                _entries.Enqueue(newObject);
            }
        }

        public int Available
        {
            get { return _entries.Count; }
        }

        public void ResetAll()
        {
            foreach (var item in _entries)
            {
                if (item is IPoolable poolable)
                {
                    poolable.Release();
                }

                item.Visible = false;
            }
        }

        public T Aquire<T>(bool release) where T : Node3D
        {
            if (!release)
            {
                Node3D oldest = _entries.Dequeue();
                _entries.Enqueue(oldest);
                return (T)oldest;
            }
            else
            {
                if (_entries.Count == 0)
                {
                    RuntimeLogger.Error(this, $"Pool \"{_poolId}\" requested more objects than was previously allocated!");
                    return null;
                }

                Node3D released = _entries.Dequeue();

                if (released is IPoolable poolable)
                {
                    poolable.Aquire();
                }

                released.Visible = true;

                return (T)released;
            }
        }

        public void Release(Node3D node)
        {
            if (node is IPoolable poolable)
            {
                poolable.Release();
            }

            node.Visible = false;

            _entries.Enqueue(node);
        }
    }
}
