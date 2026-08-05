using Godot;
using System;
using System.Collections.Generic;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class FgdManager : Node, ICoreSystem
    {
        private readonly Dictionary<Type, HashSet<BaseFgd>> _typeEntries = [];
        private readonly Dictionary<string, HashSet<BaseFgd>> _nameEntries = [];

        bool ISystem.Initialize()
        {
            return true;
        }

        public HashSet<T> OwnByName<T>(IFgdOwner owner, string name) where T : BaseFgd
        {
            if (!_nameEntries.TryGetValue(name, out var namedEntries))
            {
                this.Error($"Failed to find any FGD objects with name \"{name}\"");
                return [];
            }

            HashSet<T> result = [];

            foreach (var fgd in namedEntries)
            {
                fgd.Owners.Add(owner);
                result.Add((T)fgd);
            }

            if (result.Count == 0)
            {
                this.Error($"Failed to find any FGD objects with name \"{name}\"");
                return [];
            }

            return result;
        }

        public T OwnByNameSingle<T>(IFgdOwner owner, string name) where T : BaseFgd
        {
            if (!_nameEntries.TryGetValue(name, out var namedEntries))
            {
                this.Error($"Failed to find any FGD objects with name \"{name}\"");
                return null;
            }

            foreach (var fgd in namedEntries)
            {
                fgd.Owners.Add(owner);
                return (T)fgd;
            }

            this.Error($"Failed to find any FGD objects with name \"{name}\"");
            return null;
        }

        public HashSet<T> OwnByType<T>(IFgdOwner owner) where T : BaseFgd
        {
            Type type = typeof(T);

            if (!_typeEntries.TryGetValue(type, out var typedEntries))
            {
                return [];
            }

            HashSet<T> result = [];

            foreach (var fgd in typedEntries)
            {
                fgd.Owners.Add(owner);
                result.Add((T)fgd);
            }

            return result;
        }

        public T OwnByTypeSingle<T>(IFgdOwner owner) where T : BaseFgd
        {
            Type type = typeof(T);

            if (!_typeEntries.TryGetValue(type, out var typedEntries))
            {
                return null;
            }

            foreach (var fgd in typedEntries)
            {
                fgd.Owners.Add(owner);
                return (T)fgd;
            }

            return null;
        }

        public void Disown<T>(IFgdOwner owner, T fgd) where T : BaseFgd
        {
            fgd.Owners.Remove(owner);
        }

        public void Disown<T>(IFgdOwner owner, HashSet<T> fgds) where T : BaseFgd
        {
            foreach (var entry in fgds)
            {
                Disown(owner, entry);
            }

            fgds.Clear();
        }

        public void AddFgd(BaseFgd fgd)
        {
            Type type = fgd.GetType();

            if (!_typeEntries.TryGetValue(type, out var typeEntries))
            {
                typeEntries = [];
                _typeEntries[type] = typeEntries;
            }

            typeEntries.Add(fgd);

            if (string.IsNullOrEmpty(fgd.TargetName))
            {
                return;
            }

            if (!_nameEntries.TryGetValue(fgd.TargetName, out var nameEntries))
            {
                nameEntries = [];
                _nameEntries[fgd.TargetName] = nameEntries;
            }

            nameEntries.Add(fgd);
        }

        public void RemoveFgd(BaseFgd fgd)
        {
            Type type = fgd.GetType();

            if (_typeEntries.TryGetValue(type, out var typeEntries))
            {
                typeEntries.Remove(fgd);
            }

            if (_nameEntries.TryGetValue(fgd.TargetName, out var nameEntries))
            {
                nameEntries.Remove(fgd);
            }
        }
    }
}
