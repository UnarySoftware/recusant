using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace Unary.Core
{
    public class SystemCollector<T> where T : ISystem
    {
        private readonly Dictionary<Type, (T instance, bool initialized)> _systemsDictionary = [];
        private readonly List<T> _systemsList = [];
        private Node _root;

        public IReadOnlyList<T> Systems
        {
            get
            {
                return _systemsList;
            }
        }

        public bool Initialized { get; private set; } = true;

        public T GetSystem()
        {
            return GetSystem(typeof(T));
        }

        public T GetSystem(Type type)
        {
            if (!_systemsDictionary.TryGetValue(type, out var system))
            {
                return default;
            }

            if (!system.initialized)
            {
                system = new(system.instance, true);
                _systemsDictionary[type] = system;

                if (system.instance.Initialize())
                {
                    _systemsList.Add(system.instance);
                }
                else
                {
                    Initialized = false;
                    system.instance.Critical($"Failed to initialize {system.instance.GetType().FullName}");
                    return default;
                }
            }

            return system.instance;
        }

        public bool Initialize(HashSet<string> enabledNamespaces, Node root)
        {
            _root = root;

            var types = Types.GetTypesOfBase(typeof(T));

            Dictionary<string, Node> namespaceRoots = [];

            if (_root != null)
            {
                foreach (var enabledNamespace in enabledNamespaces)
                {
                    string name = enabledNamespace.FilterTreeName();

                    Node resolvedRoot;

                    if (_root.HasNode(name))
                    {
                        resolvedRoot = _root.GetNode(name);
                    }
                    else
                    {
                        resolvedRoot = new()
                        {
                            Name = name
                        };
                        _root.AddChild(resolvedRoot);
                    }

                    namespaceRoots.Add(enabledNamespace, resolvedRoot);
                }
            }

            Dictionary<Type, HashSet<Type>> explicitDependencies = [];

            foreach (var type in types)
            {
                if (type == typeof(T) || type.IsInterface)
                {
                    continue;
                }

                if (!enabledNamespaces.Contains(type.Namespace.ToLower()))
                {
                    continue;
                }

                if (!explicitDependencies.TryGetValue(type, out var dependencies))
                {
                    dependencies = [];
                    explicitDependencies[type] = dependencies;
                }

                MethodInfo method = null;

                var interfaceMap = type.GetInterfaceMap(typeof(ISystem));
                for (int i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
                {
                    if (interfaceMap.InterfaceMethods[i].Name == nameof(RuntimeLogger.Initialize))
                    {
                        method = interfaceMap.TargetMethods[i];
                        break;
                    }
                }

                if (method != null)
                {
                    foreach (var attribute in method.GetCustomAttributes())
                    {
                        if (attribute is InitializeExplicitAttribute target)
                        {
                            foreach (var entry in target.Dependencies)
                            {
                                dependencies.Add(entry);
                            }
                        }
                    }
                }

                T newSystem = (T)Activator.CreateInstance(type);

                if (_root != null && newSystem is Node nodeSystem)
                {
                    nodeSystem.Name = type.Name.FilterTreeName();

                    string targetName = type.Namespace.ToLower();

                    if (namespaceRoots.TryGetValue(targetName, out var targetRoot))
                    {
                        targetRoot.AddChild(nodeSystem);
                    }
                }

                _systemsDictionary.Add(type, new(newSystem, false));
            }

            List<TopoSortItem<Type>> preSortedTypes = [];

            foreach (var dependencySet in explicitDependencies)
            {
                preSortedTypes.Add(new TopoSortItem<Type>(dependencySet.Key, [.. dependencySet.Value]));
            }

            List<TopoSortItem<Type>> sortedTypes = [.. preSortedTypes.TopoSort(x => x.Target, x => x.Dependencies)];

            foreach (var targetType in sortedTypes)
            {
                GetSystem(targetType.Target);

                if (!Initialized)
                {
                    break;
                }
            }

            return Initialized;
        }

        public bool PostInitialize()
        {
            if (Initialized)
            {
                foreach (var system in _systemsList)
                {
                    if (!system.PostInitialize())
                    {
                        Initialized = false;
                        break;
                    }
                }
            }

            return Initialized;
        }

        public void Process(float delta)
        {
            foreach (var system in _systemsList)
            {
                system.Process(delta);
            }
        }

        public void PhysicsProcess(float delta)
        {
            foreach (var system in _systemsList)
            {
                system.PhysicsProcess(delta);
            }
        }

        public void Deinitialize()
        {
            for (int i = _systemsList.Count - 1; i >= 0; i--)
            {
                T target = _systemsList[i];
                target.Deinitialize();

                if (target is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
