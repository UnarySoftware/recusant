using Godot;
using System.IO;
using Unary.Core;
using Unary.Core.Editor;
using Unary.Recusant.Editor;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class LevelRoot : Node
    {
        public static StringName LevelRootGroup { get; } = new(nameof(LevelRoot));

#if TOOLS
        [ExportToolButton("Build Navigation")]
        public Callable BuildNavigation => Callable.From(OnBuildNavigation);

        private void OnBuildNavigation()
        {
            LevelRootEditor.BuildNavigation(this);
        }

        public override void _Ready()
        {
            if (Engine.Singleton.IsEditorHint())
            {
                if (!IsInGroup(LevelRootGroup))
                {
                    AddToGroup(LevelRootGroup, true);
                }

                CallDeferred(MethodName.InitializeNodes);
            }
        }

        private void InitializeNodes()
        {
            LevelRootEditor.InitializeNodes(this);
        }
#endif

        private bool Initialized = false;

        public override void _Process(double delta)
        {
            if (Engine.Singleton.IsEditorHint())
            {
                return;
            }

            if (!Initialized)
            {
                Initialized = true;

                LevelManager.Singleton.OnLoaded.Publish(new()
                {
                    Root = LevelManager.Singleton.Root,
                    Definition = LevelManager.Singleton.Definition,
                });
            }
        }
    }
}
