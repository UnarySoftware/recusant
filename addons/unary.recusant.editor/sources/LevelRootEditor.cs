#if TOOLS

using Godot;
using System.IO;
using Unary.Core.Editor;

namespace Unary.Recusant.Editor
{
    public static class LevelRootEditor
    {
        private static T CreateResource<T>(string path, T resource) where T : Resource, new()
        {
            ResourceSaver.Singleton.Save(resource, path);
            return (T)ResourceLoader.Singleton.Load(path);
        }

        private static void CreateResources(LevelRoot node, string targetDirectory)
        {
            Node owner = node.GetTree().EditedSceneRoot;

            LightmapGIData lightmapData = CreateResource<LightmapGIData>(targetDirectory + "/lightmap.lmbake", new());

            LightmapGI lightmapGi = new()
            {
                Name = "Lightmap",
                LightData = lightmapData,
                Supersampling = true
            };

            node.AddChild(lightmapGi);
            lightmapGi.Owner = owner;

            ArrayOccluder3D occluderData = CreateResource<ArrayOccluder3D>(targetDirectory + "/occlusion.occ", new());

            OccluderInstance3D occlusion = new()
            {
                Name = "Occlusion",
                Occluder = occluderData
            };

            node.AddChild(occlusion);
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
                GeometrySourceGroupName = LevelRoot.LevelRootGroup,
                GeometryParsedGeometryType = NavigationMesh.ParsedGeometryType.StaticColliders
            });

            NavigationRegion3D navigation = new()
            {
                Name = "Navigation",
                NavigationMesh = navigationData
            };

            node.AddChild(navigation);
            navigation.Owner = owner;

            DirectionalLight3D light = new()
            {
                Name = "GlobalLight"
            };

            node.AddChild(light);
            light.Owner = owner;
        }

        public static void InitializeNodes(LevelRoot node)
        {
            if (node.GetChildCount() > 0)
            {
                return;
            }

            string targetPath = EditorInterface.Singleton.GetEditedSceneRoot().SceneFilePath.ToLower().Replace("res://", "").Replace('\\', '/');

            if (string.IsNullOrEmpty(targetPath))
            {
                PluginLogger.Critical(node, "Failed to aquire path of the current scene");
                return;
            }

            string directory = Path.GetDirectoryName(targetPath).Replace('\\', '/');
            string file = Path.GetFileNameWithoutExtension(targetPath);

            if (!directory.EndsWith("levels"))
            {
                PluginLogger.Critical(node, $"Created {nameof(LevelRoot)} is outside of a proper levels folder, skipping node creation");
                return;
            }

            string targetDirectory = directory + '/' + file;

            if (Directory.Exists(targetDirectory))
            {
                Directory.Delete(targetDirectory, true);
            }

            Directory.CreateDirectory(targetDirectory);

            CreateResources(node, targetDirectory);
        }

        private static NavigationRegion3D targetRegion;
        private static LevelRoot targetNode;

        public static void BuildNavigation(LevelRoot node)
        {
            NavigationRegion3D region = node.GetNodeOrNull<NavigationRegion3D>("Navigation");

            if (region == null)
            {
                PluginLogger.Critical(node, $"Failed to aquire a {nameof(NavigationRegion3D)} node to build a navigation");
                return;
            }

            targetRegion = region;
            targetNode = node;

            region.BakeFinished += OnBakeFinished;
            region.BakeNavigationMesh(true);
        }

        private static void OnBakeFinished()
        {
            targetRegion.BakeFinished -= OnBakeFinished;

            ResourceSaver.Singleton.Save(targetRegion.NavigationMesh, targetRegion.NavigationMesh.ResourcePath);

            PluginLogger.Critical(targetNode, "Navigation baked successfully.");
        }
    }
}

#endif
