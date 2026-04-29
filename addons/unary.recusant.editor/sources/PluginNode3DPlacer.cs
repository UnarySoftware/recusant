#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using Unary.Core;
using Unary.Core.Editor;

namespace Unary.Recusant.Editor
{
    [Tool]
    public partial class PluginNode3DPlacer : Node, IPluginSystem
    {
        private HashSet<Type> _targetAncorTypes = [];
        private HashSet<Type> _targetPlaceableTypes = [];

        public static EditorSettingVariable<string[]> AncorTypes = new()
        {
            EditorDefault = [],
            RuntimeDefault = [],
            PropertyHint = PropertyHint.ArrayType,
            HintText = "String"
        };

        public static EditorSettingVariable<string[]> PlaceableTypes = new()
        {
            EditorDefault = [nameof(Decal), nameof(MeshInstance3D)],
            RuntimeDefault = [],
            PropertyHint = PropertyHint.ArrayType,
            HintText = "String"
        };

        [EditorSettingAction]
        private static void RefreshTypes()
        {
            var instance = Singleton;

            instance._targetAncorTypes.Clear();
            instance._targetPlaceableTypes.Clear();

            instance._targetAncorTypes = Types.GetTypesWithAttribute(typeof(SceneAncorAttribute)) ?? [];
            instance._targetPlaceableTypes = Types.GetTypesWithAttribute(typeof(ScenePlaceableAttribute)) ?? [];

            var ancorTypes = AncorTypes.Value;

            foreach (var type in ancorTypes)
            {
                Type resolved = Types.GetTypeOfName(type);

                if (resolved == null)
                {
                    PluginLogger.Warning(Singleton, $"Skipping invalid type \"{type}\"");
                    continue;
                }

                instance._targetAncorTypes.Add(resolved);
            }

            var placeableTypes = PlaceableTypes.Value;

            foreach (var type in placeableTypes)
            {
                Type resolved = Types.GetTypeOfName(type);

                if (resolved == null)
                {
                    PluginLogger.Warning(Singleton, $"Skipping invalid type \"{type}\"");
                    continue;
                }

                instance._targetPlaceableTypes.Add(resolved);
            }
        }

        private Camera3D _camera;

        bool ISystem.Initialize()
        {
            EditorInterface.Singleton.GetSelection().SelectionChanged += OnSelectionChanged;

            _camera = EditorInterface.Singleton.GetEditorViewport3D().GetCamera3D();

            if (_camera == null)
            {
                this.Error("Failed to get a viewport 3D camera");
                return false;
            }

            RefreshTypes();

            return true;
        }

        void ISystem.Deinitialize()
        {
            EditorInterface.Singleton.GetSelection().SelectionChanged -= OnSelectionChanged;
        }

        private Node3D _candidate;

        private void OnSelectionChanged()
        {
            var nodes = EditorInterface.Singleton.GetSelection().GetTopSelectedNodes();

            if (nodes.Count != 1)
            {
                return;
            }

            var editedRoot = EditorInterface.Singleton.GetEditedSceneRoot();

            Type rootType = editedRoot.GetType();

            if (!_targetAncorTypes.Contains(rootType))
            {
                return;
            }

            _candidate = null;

            Node node = nodes[0];

            if (node == editedRoot)
            {
                return;
            }

            Node parent = node.GetParent();

            if (parent == null || parent != editedRoot)
            {
                return;
            }

            if (node is Node3D node3D)
            {
                if (node3D.Position != Vector3.Zero)
                {
                    return;
                }

                Type type = node3D.GetType();

                if (_targetPlaceableTypes.Contains(type))
                {
                    _candidate = node3D;
                }
            }
        }

        public void PhysicsProcess(float delta)
        {
            if (_candidate == null)
            {
                return;
            }

            Vector3 from = _camera.GlobalPosition;
            Vector3 dir = -_camera.GlobalTransform.Basis.Z;
            Vector3 to = from + dir * 250.0f;

            Vector3 position;

            World3D world = _candidate.GetWorld3D();

            if (world != null)
            {
                PhysicsDirectSpaceState3D state = world.DirectSpaceState;

                if (state != null)
                {
                    PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(from, to);

                    var result = state.IntersectRay(query);

                    if (result.Count == 0)
                    {
                        position = from + dir * 10.0f;
                    }
                    else
                    {
                        position = result["position"].AsVector3();
                    }
                }
                else
                {
                    position = from + dir * 10.0f;
                }
            }
            else
            {
                position = from + dir * 10.0f;
            }

            EditorUndoRedoManager undoRedo = PluginBootstrap.Singleton.GetUndoRedo();

            undoRedo.CreateAction("Move 3D Node to View");
            undoRedo.AddDoProperty(_candidate, "position", position);
            undoRedo.AddUndoProperty(_candidate, "position", _candidate.Position);
            undoRedo.CommitAction();

            _candidate = null;
        }
    }
}

#endif
