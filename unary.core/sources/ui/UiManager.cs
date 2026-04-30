using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unary.Recusant;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UiManager : Node, ICoreSystem
    {
        private struct UiStateData
        {
            public UiStateManifest Manifest;
            public Control Root;
            public int Order;
            public UiState State;
            public List<UiUnitBase> Units;
        }

        private StringName ui_back = new(nameof(ui_back));
        private StringName ui_mouse_release = new(nameof(ui_mouse_release));

        private readonly Dictionary<Type, UiStateData> _states = [];
        private Type _currentState;

        // This type ALWAYS goes last
        private static readonly Type _coreType = typeof(UiCoreState);

        public T GetState<T>() where T : UiState
        {
            Type type = typeof(T);

            if (_states.TryGetValue(type, out var result))
            {
                return (T)result.State;
            }

            return null;
        }

        // I hate it that using two loops like this takes the least amount of code because FieldInfo and PropertyInfo dont have a base shared class
        private void Resolve(Node node, Node root)
        {
            Type type = node.GetType();

            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                foreach (var attribute in field.GetCustomAttributes())
                {
                    if (attribute is UiElementAttribute element)
                    {
                        Node target = root.GetNode(element.NodePath);

                        if (target == null)
                        {
                            this.Warning($"Failed to resolve node with path \"{element.NodePath}\" for \"{type.FullName}\"");
                            continue;
                        }

                        Type targetType = target.GetType();

                        if (targetType != field.FieldType)
                        {
                            this.Warning($"Failed to resolve node with path \"{element.NodePath}\" for \"{type.FullName}\" due to a wrong type \"{targetType.FullName}\" != \"{field.FieldType.FullName}\"");
                            continue;
                        }

                        field.SetValue(node, target);
                    }
                }
            }

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var property in properties)
            {
                foreach (var attribute in property.GetCustomAttributes())
                {
                    if (attribute is UiElementAttribute element)
                    {
                        Node target = root.GetNode(element.NodePath);

                        if (target == null)
                        {
                            this.Warning($"Failed to resolve node with path \"{element.NodePath}\" for \"{type.FullName}\"");
                            continue;
                        }

                        Type targetType = target.GetType();

                        if (targetType != property.PropertyType)
                        {
                            this.Warning($"Failed to resolve node with path \"{element.NodePath}\" for \"{type.FullName}\" due to a wrong type \"{targetType.FullName}\" != \"{property.PropertyType.FullName}\"");
                            continue;
                        }

                        property.SetValue(node, target);
                    }
                }
            }
        }

        [InitializeExplicit(typeof(ModSystems))]
        bool ISystem.Initialize()
        {
            List<UiStateManifest> states = ResourceTypesManager.Singleton.LoadResourcesOfType<UiStateManifest>();

            if (states.Count == 0)
            {
                return this.Critical("Could not find any UI states");
            }

            Dictionary<UiStateManifest, HashSet<UiStateManifest>> resolvedDependencies = [];

            foreach (var state in states)
            {
                Type targetType = state.Type.ResolveType();

                if (targetType == null)
                {
                    return this.Critical($"Failed to resolve a UI state type \"{state.Type.TargetValue}\"");
                }

                if (state.Underlaying != null)
                {
                    foreach (var underlaying in state.Underlaying)
                    {
                        UiStateManifest manifest = underlaying.Load<UiStateManifest>();

                        if (manifest == null)
                        {
                            return this.Critical($"Failed to load \"{underlaying.TargetValue}\" as an underlaying UI state of \"{targetType.FullName}\"");
                        }

                        if (!resolvedDependencies.TryGetValue(manifest, out var dependencies))
                        {
                            dependencies = [];
                            resolvedDependencies[manifest] = dependencies;
                        }

                        dependencies.Add(state);
                    }
                }

                if (state.Overlaying != null)
                {
                    foreach (var overlaying in state.Overlaying)
                    {
                        UiStateManifest manifest = overlaying.Load<UiStateManifest>();

                        if (manifest == null)
                        {
                            return this.Critical($"Failed to load \"{overlaying.TargetValue}\" as an overlaying UI state of \"{targetType.FullName}\"");
                        }

                        if (!resolvedDependencies.TryGetValue(state, out var dependencies))
                        {
                            dependencies = [];
                            resolvedDependencies[state] = dependencies;
                        }

                        dependencies.Add(manifest);
                    }
                }

                // Add empty dependencies set in order to be added as a valid topo sort entry down the execution
                if (!resolvedDependencies.TryGetValue(state, out var emptyDependencies))
                {
                    emptyDependencies = [];
                    resolvedDependencies[state] = emptyDependencies;
                }
            }

            // Check for self-reference
            foreach (var dependency in resolvedDependencies)
            {
                foreach (var test in dependency.Value)
                {
                    if (dependency.Key == test)
                    {
                        return this.Critical($"UI state {dependency.Key.GetType().FullName} tried self-referencing itself as a dependency");
                    }
                }
            }

            List<TopoSortItem<UiStateManifest>> presortedManifests = [];

            foreach (var manifest in resolvedDependencies)
            {
                presortedManifests.Add(new TopoSortItem<UiStateManifest>(manifest.Key, [.. manifest.Value]));
            }

            List<TopoSortItem<UiStateManifest>> postSortedManifests = [.. presortedManifests.TopoSort(x => x.Target, x => x.Dependencies)];

            List<UiStateManifest> sortedManifests = [];

            UiStateManifest coreState = null;

            foreach (var manifest in postSortedManifests)
            {
                UiStateManifest targetManifest = manifest.Target;

                // Already checked if null above
                Type type = targetManifest.Type.ResolveType();

                if (type == _coreType)
                {
                    coreState = targetManifest;
                    continue;
                }

                sortedManifests.Add(targetManifest);
            }

            if (coreState != null)
            {
                sortedManifests.Add(coreState);
            }

            int order = 0;
            int alwaysOrder = (int)RenderingServer.CanvasItemZMax;

            Dictionary<string, Node> namespaceNodes = [];

            Dictionary<Type, HashSet<Type>> unitTypes = [];

            Type uiUnitBaseType = typeof(UiUnitBase);

            var units = Types.GetTypesOfBase(uiUnitBaseType);

            Type uiStateType = typeof(UiState);
            Type nodeType = typeof(Node);

            foreach (var unit in units)
            {
                if (!nodeType.IsAssignableFrom(unit))
                {
                    this.Warning($"Tried registering UI unit type \"{unit.FullName}\" that does not inherit from Godot.Node");
                    continue;
                }

                if (unit == uiUnitBaseType || unit.BaseType == uiUnitBaseType)
                {
                    continue;
                }

                if (!unit.BaseType.IsGenericType)
                {
                    this.Warning($"Tried registering UI unit type \"{unit.FullName}\" that is not generic");
                    continue;
                }

                var generics = unit.BaseType.GenericTypeArguments;

                if (generics.Length != 1)
                {
                    continue;
                }

                var stateType = generics[0];

                if (!uiStateType.IsAssignableFrom(stateType))
                {
                    this.Warning($"Tried registering UI unit type \"{unit.FullName}\" that does not inherit from UiState");
                    continue;
                }

                if (!unitTypes.TryGetValue(stateType, out var entries))
                {
                    entries = [];
                    unitTypes[stateType] = entries;
                }

                entries.Add(unit);
            }

            foreach (var manifest in sortedManifests)
            {
                // Already checked if null above
                Type type = manifest.Type.ResolveType();

                if (_states.ContainsKey(type))
                {
                    return this.Critical($"Tried registering duplicate UI state {type.FullName}");
                }

                string targetNamespace = type.Namespace.ToLower().FilterTreeName();

                if (!namespaceNodes.TryGetValue(targetNamespace, out var namespaceNode))
                {
                    namespaceNode = new()
                    {
                        Name = targetNamespace
                    };
                    AddChild(namespaceNode);
                    namespaceNodes[targetNamespace] = namespaceNode;
                }

                UiState newUiState = (UiState)Activator.CreateInstance(type);
                newUiState.Name = type.Name.FilterTreeName();
                newUiState.SetAnchorsPreset(Control.LayoutPreset.FullRect);
                newUiState.MouseFilter = Control.MouseFilterEnum.Ignore;
                namespaceNode.AddChild(newUiState);

                if (manifest.AlwaysEnabled)
                {
                    newUiState.ZIndex = alwaysOrder;
                    alwaysOrder--;
                }
                else
                {
                    newUiState.ZIndex = order;
                    order++;
                }

                PackedScene scene = manifest.Scene.LoadWithoutCache<PackedScene>();

                if (scene == null)
                {
                    return this.Critical($"Failed to load a UI state scene for {type.FullName}");
                }

                Control control = (Control)scene.Instantiate();

                if (control == null)
                {
                    return this.Critical($"Failed to instantiate a Control-based scene from UI state scene for {type.FullName}");
                }

                newUiState.AddChild(control);
                Resolve(newUiState, control);



                List<UiUnitBase> unitsList = [];
                Dictionary<Type, UiUnitBase> unitsDictionary = [];

                if (unitTypes.TryGetValue(type, out var finalUnits))
                {
                    foreach (var unit in finalUnits)
                    {
                        UiUnitBase newUiUnit = (UiUnitBase)Activator.CreateInstance(unit);
                        newUiUnit.Name = unit.Name.FilterTreeName();
                        newUiState.AddChild(newUiUnit);
                        unitsList.Add(newUiUnit);
                        unitsDictionary[unit] = newUiUnit;
                        Resolve(newUiUnit, control);
                    }
                }

                UiStateData newData = new()
                {
                    Manifest = manifest,
                    Order = order,
                    Root = control,
                    State = newUiState,
                    Units = unitsList
                };

                newUiState.Units = unitsDictionary;

                _states[type] = newData;

                newUiState.Initialize();

                foreach (var unit in unitsList)
                {
                    unit.Initialize();
                }

                if (manifest.AlwaysEnabled)
                {
                    newUiState.Visible = true;

                    newUiState.Open();

                    foreach (var unit in unitsList)
                    {
                        unit.Open();
                    }
                }
                else
                {
                    newUiState.Visible = false;
                }
            }

            return true;
        }

        public void Open(Type stateType)
        {
            if (stateType == null)
            {
                return;
            }

            if (stateType == _currentState)
            {
                return;
            }

            if (!_states.TryGetValue(stateType, out var newData))
            {
                this.Error($"Tried opening unknown UI state with type \"{stateType.FullName}\"");
                return;
            }

            if (_currentState != null)
            {
                newData = _states[_currentState];

                newData.State.Visible = false;

                newData.State.Close();

                foreach (var unit in newData.Units)
                {
                    unit.Close();
                }
            }

            _currentState = stateType;

            newData = _states[_currentState];

            newData.State.Visible = true;

            newData.State.Open();

            foreach (var unit in newData.Units)
            {
                unit.Open();
            }
        }

        public void GoBack()
        {
            if (_currentState == null)
            {
                return;
            }

            UiStateData data = _states[_currentState];

            Open(data.State.GetBackState());
        }

        void ISystem.Process(float delta)
        {
            foreach (var state in _states)
            {
                UiStateData data = state.Value;
                data.State.Process(delta);

                foreach (var unit in data.Units)
                {
                    unit.Process(delta);
                }
            }

            if (InputManager.Singleton.IsActionJustReleased(ui_back, 0))
            {
                GoBack();
            }

            if (InputManager.Singleton.IsActionJustReleased(ui_mouse_release, 0))
            {
                InputManager.Singleton.InvertMouseMode();
            }
        }

        void ISystem.Deinitialize()
        {
            foreach (var state in _states)
            {
                UiStateData data = state.Value;
                data.State.Deinitialize();

                foreach (var unit in data.Units)
                {
                    unit.Deinitialize();
                }
            }
        }
    }
}
