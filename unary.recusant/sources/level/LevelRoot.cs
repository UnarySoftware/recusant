using Godot;
using System.IO;
using Unary.Core;
using Unary.Core.Editor;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class LevelRoot : Node
    {
        public static StringName LevelRootGroup = new(nameof(LevelRoot));

#if TOOLS

        private T CreateResource<T>(string path, T resource) where T : Resource, new()
        {
            ResourceSaver.Singleton.Save(resource, path);
            return (T)ResourceLoader.Singleton.Load(path);
        }

        private void CreateResources(string targetDirectory)
        {
            Node owner = GetTree().EditedSceneRoot;

            LightmapGIData lightmapData = CreateResource<LightmapGIData>(targetDirectory + "/lightmap.lmbake", new());

            LightmapGI lightmapGi = new()
            {
                Name = "Lightmap",
                LightData = lightmapData,
                Supersampling = true
            };

            AddChild(lightmapGi);
            lightmapGi.Owner = owner;

            ArrayOccluder3D occluderData = CreateResource<ArrayOccluder3D>(targetDirectory + "/occlusion.occ", new());

            OccluderInstance3D occlusion = new()
            {
                Name = "Occlusion",
                Occluder = occluderData
            };

            AddChild(occlusion);
            occlusion.Owner = owner;

            NavigationMesh navigationData = CreateResource<NavigationMesh>(targetDirectory + "/navigation.tres", new()
            {
                CellSize = PlayerConstants.NavCellSize,
                CellHeight = PlayerConstants.NavCellHeight,
                AgentHeight = PlayerConstants.NavAgentHeight,
                AgentRadius = PlayerConstants.NavAgentRadius,
                AgentMaxClimb = PlayerConstants.NavAgentMaxClimb,
                VerticesPerPolygon = PlayerConstants.NavMaxVerticesPerPolygon,
                GeometrySourceGeometryMode = NavigationMesh.SourceGeometryMode.GroupsWithChildren,
                GeometrySourceGroupName = LevelRootGroup,
                GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders
            });

            NavigationRegion3D navigation = new()
            {
                Name = "Navigation",
                NavigationMesh = navigationData
            };

            AddChild(navigation);
            navigation.Owner = owner;

            DirectionalLight3D light = new()
            {
                Name = "GlobalLight"
            };

            AddChild(light);
            light.Owner = owner;
        }

        private void InitializeNodes()
        {
            if (GetChildCount() > 0)
            {
                return;
            }

            string targetPath = EditorInterface.Singleton.GetEditedSceneRoot().SceneFilePath.ToLower().Replace("res://", "").Replace('\\', '/');

            if (string.IsNullOrEmpty(targetPath))
            {
                PluginLogger.Critical(this, "Failed to aquire path of the current scene");
                return;
            }

            string directory = Path.GetDirectoryName(targetPath).Replace('\\', '/');
            string file = Path.GetFileNameWithoutExtension(targetPath);

            if (!directory.EndsWith("levels"))
            {
                PluginLogger.Critical(this, $"Created {nameof(LevelRoot)} is outside of a proper levels folder, skipping node creation");
                return;
            }

            string targetDirectory = directory + '/' + file;

            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, true);
            }

            Directory.CreateDirectory(targetDirectory);

            CreateResources(targetDirectory);
        }

#endif

        public override void _Ready()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                if (!IsInGroup(LevelRootGroup))
                {
                    AddToGroup(LevelRootGroup, true);
                }

                CallDeferred(MethodName.InitializeNodes);
            }
#endif
        }

#if TOOLS

        [ExportToolButton("BuildNavigation")]
        public Callable BuildNavigation => Callable.From(OnBuildNavigation);

        private NavigationRegion3D targetRegion;

        private void OnBuildNavigation()
        {
            NavigationRegion3D region = GetNodeOrNull<NavigationRegion3D>("Navigation");

            if (region == null)
            {
                PluginLogger.Critical(this, $"Failed to aquire a {nameof(NavigationRegion3D)} node to build a navigation");
                return;
            }

            targetRegion = region;

            region.BakeFinished += OnBakeFinished;
            region.BakeNavigationMesh(true);
        }

        private void OnBakeFinished()
        {
            targetRegion.BakeFinished -= OnBakeFinished;

            ResourceSaver.Singleton.Save(targetRegion.NavigationMesh, targetRegion.NavigationMesh.ResourcePath);

            PluginLogger.Critical(this, "Navigation baked successfully.");
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
