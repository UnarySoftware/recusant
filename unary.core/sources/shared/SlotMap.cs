using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unary.Core
{
    public sealed class SlotMap<T>
    {
        private readonly List<T> _dense = [];
        private readonly List<int> _backMap = [];
        private readonly List<int> _sparse = [];
        private readonly Stack<int> _freeSlots = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => CollectionsMarshal.AsSpan(_dense);

        public int Add(T item)
        {
            int denseId = _dense.Count;
            _dense.Add(item);

            int slotId;

            if (_freeSlots.TryPop(out var free))
            {
                slotId = free;
                _sparse[slotId] = denseId;
            }
            else
            {
                slotId = _sparse.Count;
                _sparse.Add(denseId);
            }

            _backMap.Add(slotId);
            return slotId;
        }

        public void Remove(int slotId)
        {
            int denseId = _sparse[slotId];
            int lastDense = _dense.Count - 1;

            if (denseId != lastDense)
            {
                _dense[denseId] = _dense[lastDense];
                int movedSlot = _backMap[lastDense];
                _backMap[denseId] = movedSlot;
                _sparse[movedSlot] = denseId;
            }

            _dense.RemoveAt(lastDense);
            _backMap.RemoveAt(lastDense);
            _sparse[slotId] = -1;
            _freeSlots.Push(slotId);
        }
    }
}
