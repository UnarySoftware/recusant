// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace FuncGodot
{
    /// <summary>
    /// Parses a Valve 220 format Quake .map file into entity, brush, and group data.
    /// </summary>
    public sealed class FuncGodotParser
    {
        public const string Signature = "[PRS]";

        /// Raised as each parsing step completes. Wired up when the map's ShowProfileInfo flag is set.
        public event Action<string> DeclareStep;

        private void Step(string step)
        {
            DeclareStep?.Invoke(step);
        }

        /// <summary>
        /// Reads the map file and produces its entities and groups, with every entity resolved against the
        /// FGD and its properties converted from strings into the types the definition declares.
        /// </summary>
        public FuncGodotData.ParseData ParseMapData(string mapFile, FuncGodotMapSettings mapSettings)
        {
            FuncGodotData.ParseData parseData = new();

            Step($"Loading map file {mapFile}");

            if (!TryReadMapFile(mapFile, out string[] mapData))
            {
                return parseData;
            }

            Step("Parsing as Valve 220 MAP");

            if (!ParseValveMap(mapData, mapSettings, parseData))
            {
                GD.PushError($"Error: Failed to parse map file ({mapFile})");
                return new FuncGodotData.ParseData();
            }

            Step("Determining groups hierarchy");
            ResolveGroupHierarchy(parseData.Groups);

            Step("Checking entity omission, definition status, and property types");
            ResolveEntityDefinitions(parseData.Entities, mapSettings);

            Step("Removing omitted layers and groups");
            parseData.Groups.RemoveAll(group => group.Omit);

            Step("Map parsing complete");

            return parseData;
        }

        /// <summary>
        /// Reads the raw lines of the map. Exported projects cannot read the .map directly, so the imported
        /// <see cref="QuakeMapFile"/> resource beside it is used instead.
        /// </summary>
        private static bool TryReadMapFile(string mapFile, out string[] mapData)
        {
            mapData = [];

            if (mapFile.StartsWith("uid://"))
            {
                long uid = ResourceUid.TextToId(mapFile);

                if (!ResourceUid.HasId(uid))
                {
                    GD.PushError($"Error: failed to retrieve path for UID ({mapFile})");
                    return false;
                }

                mapFile = ResourceUid.GetIdPath(uid);
            }

            if (FileAccess.FileExists(mapFile))
            {
                using FileAccess file = FileAccess.Open(mapFile, FileAccess.ModeFlags.Read);

                if (file == null)
                {
                    GD.PushError($"Error: Failed to open map file ({mapFile})");
                    return false;
                }

                mapData = file.GetAsText().Replace("\r", "").Split('\n');
                return true;
            }

            // Packed map files are only reachable through the resource the import plugin generated.
            if (ResourceLoader.Load(mapFile) is QuakeMapFile mapResource && !string.IsNullOrEmpty(mapResource.MapData))
            {
                mapData = mapResource.MapData.Replace("\r", "").Split('\n');
                return true;
            }

            GD.PushError($"Error: Failed to open map file ({mapFile})");
            return false;
        }

        private static void ResolveGroupHierarchy(List<FuncGodotData.GroupData> groups)
        {
            foreach (FuncGodotData.GroupData group in groups)
            {
                if (group.ParentId == -1)
                {
                    continue;
                }

                foreach (FuncGodotData.GroupData parent in groups)
                {
                    if (parent.Id == group.ParentId)
                    {
                        group.Parent = parent;
                        break;
                    }
                }
            }
        }

        #region MAP FORMAT

        /// <summary>
        /// Walks the map file line by line, accumulating entities and their brushes. Faces are stored as raw
        /// planes and texture axes here; they only become vertices during geometry generation.
        /// </summary>
        private static bool ParseValveMap(
            string[] mapData,
            FuncGodotMapSettings mapSettings,
            FuncGodotData.ParseData parseData)
        {
            List<FuncGodotData.EntityData> entities = parseData.Entities;
            List<FuncGodotData.GroupData> groups = parseData.Groups;

            FuncGodotData.EntityData entity = null;
            FuncGodotData.BrushData brush = null;

            bool multilineActive = false;
            string multilineKey = string.Empty;
            string multilineValue = string.Empty;
            int multilineLine = -1;

            for (int lineIndex = 0; lineIndex < mapData.Length; lineIndex++)
            {
                int lineNumber = lineIndex + 1;
                string line = mapData[lineIndex].Replace("\t", "").Replace("\r", "");

                // A property value may span lines. Keep consuming until its closing quote.
                if (multilineActive)
                {
                    int valueEnd = FindUnescapedQuote(line);

                    if (valueEnd == -1)
                    {
                        multilineValue += "\n" + line;
                        continue;
                    }

                    multilineValue += "\n" + line[..valueEnd];

                    string tail = line[(valueEnd + 1)..].Trim();

                    if (!string.IsNullOrEmpty(tail))
                    {
                        GD.PushWarning($"Unexpected trailing data after multiline property at line {lineNumber}, ignoring: {tail}");
                    }

                    if (entity == null)
                    {
                        GD.PushError($"Malformed MAP property continuation at line {lineNumber}");
                        return false;
                    }

                    entity.Properties[multilineKey] = multilineValue;
                    multilineActive = false;
                    multilineKey = string.Empty;
                    multilineValue = string.Empty;
                    multilineLine = -1;
                    continue;
                }

                // Open an entity, or a brush inside one.
                if (line.StartsWith('{'))
                {
                    if (entity == null)
                    {
                        entity = new FuncGodotData.EntityData();
                    }
                    else
                    {
                        brush = new FuncGodotData.BrushData();
                    }

                    continue;
                }

                // Commit a brush, or an entity once its brushes are done.
                if (line.StartsWith('}'))
                {
                    if (brush != null)
                    {
                        entity?.Brushes.Add(brush);
                        brush = null;
                        continue;
                    }

                    if (entity == null)
                    {
                        continue;
                    }

                    CommitEntity(entity, entities, groups);
                    entity = null;
                    continue;
                }

                // Key/value pairs belong to the entity, never to a brush.
                if (entity != null && brush == null && line.StartsWith('"'))
                {
                    if (!TryParseQuotedKeyValue(line, out string key, out string value, out bool complete, out string trailing))
                    {
                        GD.PushWarning($"Malformed MAP property at line {lineNumber}, skipping: {line}");
                        continue;
                    }

                    if (complete)
                    {
                        if (!string.IsNullOrEmpty(trailing))
                        {
                            GD.PushWarning($"Unexpected trailing data after property at line {lineNumber}, ignoring: {trailing}");
                        }

                        entity.Properties[key] = value;
                    }
                    else
                    {
                        multilineActive = true;
                        multilineKey = key;
                        multilineValue = value;
                        multilineLine = lineNumber;
                    }

                    continue;
                }

                // Brush faces.
                if (brush != null && line.StartsWith('('))
                {
                    if (TryParseFace(line, lineNumber, mapSettings, out FuncGodotData.FaceData face))
                    {
                        brush.Planes.Add(face.Plane);

                        // An origin brush must be textured entirely with the origin texture to count.
                        if (brush.Faces.Count == 0)
                        {
                            brush.Origin = FuncGodotUtil.IsOrigin(face.Texture, mapSettings);
                        }
                        else if (brush.Origin && !FuncGodotUtil.IsOrigin(face.Texture, mapSettings))
                        {
                            brush.Origin = false;
                        }

                        brush.Faces.Add(face);
                    }
                }
            }

            if (multilineActive)
            {
                GD.PushError($"Unterminated multiline property \"{multilineKey}\" starting at line {multilineLine}");
                return false;
            }

            AssignGroups(entities, groups);

            return true;
        }

        /// <summary>
        /// Files an entity away: TrenchBroom groups and layers become <see cref="FuncGodotData.GroupData"/>
        /// and donate their structural brushes to worldspawn; everything else becomes a real entity.
        /// </summary>
        private static void CommitEntity(
            FuncGodotData.EntityData entity,
            List<FuncGodotData.EntityData> entities,
            List<FuncGodotData.GroupData> groups)
        {
            bool isGroup = entity.Properties.TryGetValue("classname", out Variant classname)
                && classname.AsString() == "func_group"
                && entity.Properties.ContainsKey("_tb_type");

            if (!isGroup)
            {
                entities.Add(entity);
                return;
            }

            // Structural brushes inside a group belong to worldspawn, not to the group node.
            if (entities.Count > 0)
            {
                entities[0].Brushes.AddRange(entity.Brushes);
            }

            FuncGodotData.GroupData group = new()
            {
                Id = entity.Properties["_tb_id"].AsInt32(),
            };

            bool isLayer = entity.Properties["_tb_type"].AsString() == "_tb_layer";

            group.Type = isLayer
                ? FuncGodotData.GroupData.GroupType.Layer
                : FuncGodotData.GroupData.GroupType.Group;

            group.Name = (isLayer ? "layer_" : "group_") + group.Id;

            if (entity.Properties.TryGetValue("_tb_name", out Variant name) && name.AsString() != "Unnamed")
            {
                group.Name += "_" + name.AsString().Replace(" ", "_");
            }

            if (entity.Properties.TryGetValue("_tb_layer", out Variant layerId))
            {
                group.ParentId = layerId.AsInt32();
            }

            if (entity.Properties.TryGetValue("_tb_group", out Variant groupId))
            {
                group.ParentId = groupId.AsInt32();
            }

            group.Omit = entity.Properties.ContainsKey("_tb_layer_omit_from_export");

            groups.Add(group);
        }

        private static void AssignGroups(
            List<FuncGodotData.EntityData> entities,
            List<FuncGodotData.GroupData> groups)
        {
            foreach (FuncGodotData.EntityData entity in entities)
            {
                int groupId = -1;

                if (entity.Properties.TryGetValue("_tb_layer", out Variant layerId))
                {
                    groupId = layerId.AsInt32();
                }
                else if (entity.Properties.TryGetValue("_tb_group", out Variant id))
                {
                    groupId = id.AsInt32();
                }

                if (groupId == -1)
                {
                    continue;
                }

                foreach (FuncGodotData.GroupData group in groups)
                {
                    if (group.Id == groupId)
                    {
                        entity.Group = group;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Parses one Valve 220 face:
        /// <c>( x y z ) ( x y z ) ( x y z ) TEXTURE [ ux uy uz uoffset ] [ vx vy vz voffset ] rotation su sv</c>
        /// </summary>
        private static bool TryParseFace(
            string line,
            int lineNumber,
            FuncGodotMapSettings mapSettings,
            out FuncGodotData.FaceData face)
        {
            face = null;

            // Strip the opening parens so the three plane points and the tail split cleanly.
            string[] tokens = line.Replace("(", "").Split(" ) ");

            if (tokens.Length < 4)
            {
                GD.PushError($"Malformed face at line {lineNumber}, skipping: {line}");
                return false;
            }

            Vector3[] points = new Vector3[3];

            for (int i = 0; i < 3; i++)
            {
                float[] values = SplitFloats(tokens[i].TrimStart('('));

                if (values.Length < 3)
                {
                    GD.PushError($"Malformed plane point at line {lineNumber}, skipping: {line}");
                    return false;
                }

                points[i] = new Vector3(values[0], values[1], values[2]) * mapSettings.ScaleFactor;
            }

            face = new FuncGodotData.FaceData
            {
                Plane = new Plane(points[0], points[1], points[2]),
            };

            // Texture names containing spaces are quoted.
            string tail = tokens[3];
            string texture;

            if (tail.StartsWith('"'))
            {
                int lastQuote = tail.LastIndexOf('"');
                texture = tail.Substring(1, lastQuote - 1);
                tail = tail[(lastQuote + 2)..];
            }
            else
            {
                texture = tail.Split(' ')[0];
                tail = tail[texture.Length..].TrimStart();
            }

            face.Texture = texture.Replace(" . ", "/");

            string[] uvTokens = tail.Split(" ] ");

            if (uvTokens.Length < 3)
            {
                GD.PushError($"Face at line {lineNumber} is not in the Valve 220 format, skipping: {line}");
                return false;
            }

            // [ ux uy uz uoffset ] [ vx vy vz voffset ]: axes as vectors, offsets into the UV transform origin.
            for (int i = 0; i < 2; i++)
            {
                float[] axis = SplitFloats(uvTokens[i].TrimStart('[', ' '));

                if (axis.Length < 4)
                {
                    GD.PushError($"Malformed UV axis at line {lineNumber}, skipping: {line}");
                    return false;
                }

                face.UvAxes.Add(new Vector3(axis[0], axis[1], axis[2]));

                Vector2 origin = face.Uv.Origin;
                origin[i] = axis[3];
                face.Uv.Origin = origin;
            }

            // rotation su sv. Rotation lives in the texture axes under Valve 220, so only the scale is kept.
            float[] scale = SplitFloats(uvTokens[2]);

            if (scale.Length < 3)
            {
                GD.PushError($"Malformed UV scale at line {lineNumber}, skipping: {line}");
                return false;
            }

            face.Uv.X = new Vector2(scale[1], 0.0f) * mapSettings.ScaleFactor;
            face.Uv.Y = new Vector2(0.0f, scale[2]) * mapSettings.ScaleFactor;

            return true;
        }

        #endregion

        #region ENTITY DEFINITIONS

        /// <summary>
        /// Attaches each entity's FGD definition, drops entities in omitted groups, and converts the entity's
        /// string properties into the Variant types the definition declares.
        /// </summary>
        private static void ResolveEntityDefinitions(
            List<FuncGodotData.EntityData> entities,
            FuncGodotMapSettings mapSettings)
        {
            Dictionary<string, FuncGodotFGDEntityClass> entityDefinitions = mapSettings.EntityFgd.GetEntityDefinitions();
            HashSet<string> missingDefinitions = [];

            // Fallbacks, so an entity without a definition still builds into something sane.
            FuncGodotFGDPointClass defaultPointClass = new()
            {
                NodeClass = "Marker3D",
            };

            FuncGodotFGDSolidClass defaultSolidClass = new()
            {
                SpawnType = FuncGodotFGDSolidClass.SpawnTypes.Entity,
                BuildOcclusion = false,
                CollisionShapeType = FuncGodotFGDSolidClass.CollisionShapeTypes.None,
                OriginType = FuncGodotFGDSolidClass.OriginTypes.Brush,
            };

            Dictionary<string, Dictionary<string, Variant>> propertyDefaultsCache = [];
            Dictionary<string, Dictionary<string, Variant>> propertyDescriptionsCache = [];

            for (int i = entities.Count - 1; i >= 0; i--)
            {
                FuncGodotData.EntityData entity = entities[i];

                if (entity.Group != null && entity.Group.Omit)
                {
                    entities.RemoveAt(i);
                    continue;
                }

                if (entity.Properties.TryGetValue("classname", out Variant classnameValue))
                {
                    string classname = classnameValue.AsString();

                    if (entityDefinitions.TryGetValue(classname, out FuncGodotFGDEntityClass definition))
                    {
                        entity.Definition = definition;
                    }
                    else if (missingDefinitions.Add(classname))
                    {
                        GD.PushError($"No entity definition found for \"{classname}\"");
                    }
                }

                entity.Definition ??= entity.Brushes.Count == 0 ? defaultPointClass : defaultSolidClass;

                FuncGodotFGDEntityClass entityDefinition = entity.Definition;

                if (!propertyDefaultsCache.TryGetValue(entityDefinition.Classname, out Dictionary<string, Variant> defaults))
                {
                    defaults = entityDefinition.RetrieveAllClassProperties();
                    propertyDefaultsCache[entityDefinition.Classname] = defaults;
                }

                if (!propertyDescriptionsCache.TryGetValue(entityDefinition.Classname, out Dictionary<string, Variant> descriptions))
                {
                    descriptions = entityDefinition.RetrieveAllClassPropertyDescriptions();
                    propertyDescriptionsCache[entityDefinition.Classname] = descriptions;
                }

                ConvertEntityProperties(entity, defaults, descriptions);
                ApplyPropertyDefaults(entity, defaults, descriptions);
            }
        }

        /// Converts the entity's raw string properties into the types their class property defaults declare.
        private static void ConvertEntityProperties(
            FuncGodotData.EntityData entity,
            Dictionary<string, Variant> defaults,
            Dictionary<string, Variant> descriptions)
        {
            string classname = entity.Definition.Classname;

            foreach (string property in new List<string>(entity.Properties.Keys))
            {
                if (!defaults.TryGetValue(property, out Variant propertyDefault))
                {
                    continue;
                }

                Variant value = entity.Properties[property];

                if (value.VariantType != Variant.Type.String)
                {
                    continue;
                }

                string raw = value.AsString();

                switch (propertyDefault.VariantType)
                {
                    case Variant.Type.Int:
                        {
                            entity.Properties[property] = raw.ToInt();
                            break;
                        }
                    case Variant.Type.Float:
                        {
                            entity.Properties[property] = raw.ToFloat();
                            break;
                        }
                    case Variant.Type.Bool:
                        {
                            entity.Properties[property] = raw.ToInt() != 0;
                            break;
                        }
                    case Variant.Type.Vector2:
                        {
                            if (TryReadFloats(raw, 2, property, classname, "Vector2", out float[] components))
                            {
                                entity.Properties[property] = new Vector2(components[0], components[1]);
                            }

                            break;
                        }
                    case Variant.Type.Vector2I:
                        {
                            if (TryReadFloats(raw, 2, property, classname, "Vector2i", out float[] components))
                            {
                                entity.Properties[property] = new Vector2I((int)components[0], (int)components[1]);
                            }

                            break;
                        }
                    case Variant.Type.Vector3:
                        {
                            if (TryReadFloats(raw, 3, property, classname, "Vector3", out float[] components))
                            {
                                entity.Properties[property] = new Vector3(components[0], components[1], components[2]);
                            }

                            break;
                        }
                    case Variant.Type.Vector3I:
                        {
                            if (TryReadFloats(raw, 3, property, classname, "Vector3i", out float[] components))
                            {
                                entity.Properties[property] = new Vector3I((int)components[0], (int)components[1], (int)components[2]);
                            }

                            break;
                        }
                    case Variant.Type.Vector4:
                        {
                            if (TryReadFloats(raw, 4, property, classname, "Vector4", out float[] components))
                            {
                                entity.Properties[property] = new Vector4(components[0], components[1], components[2], components[3]);
                            }

                            break;
                        }
                    case Variant.Type.Vector4I:
                        {
                            if (TryReadFloats(raw, 4, property, classname, "Vector4i", out float[] components))
                            {
                                entity.Properties[property] = new Vector4I((int)components[0], (int)components[1], (int)components[2], (int)components[3]);
                            }

                            break;
                        }
                    case Variant.Type.Color:
                        {
                            if (TryReadFloats(raw, 3, property, classname, "Color", out float[] components))
                            {
                                Color color = new()
                                {
                                    R8 = (int)components[0],
                                    G8 = (int)components[1],
                                    B8 = (int)components[2],
                                    A = 1.0f,
                                };

                                entity.Properties[property] = color;
                            }

                            break;
                        }
                    case Variant.Type.Dictionary:
                        {
                            // Choices. Only integer-keyed choices convert; string choices stay as strings.
                            if (descriptions.TryGetValue(property, out Variant description)
                                && description.VariantType == Variant.Type.Array)
                            {
                                Godot.Collections.Array entry = description.AsGodotArray();

                                if (entry.Count > 1 && entry[1].VariantType == Variant.Type.Int)
                                {
                                    entity.Properties[property] = raw.ToInt();
                                }
                            }

                            break;
                        }
                    case Variant.Type.Array:
                        {
                            // Flags are stored as a summed bitmask.
                            entity.Properties[property] = raw.ToInt();
                            break;
                        }
                    case Variant.Type.StringName:
                        {
                            entity.Properties[property] = new StringName(raw);
                            break;
                        }
                    case Variant.Type.NodePath:
                        {
                            if (raw.StartsWith('$') || raw.StartsWith('%'))
                            {
                                entity.Properties[property] = new NodePath(raw);
                            }

                            break;
                        }
                }
            }
        }

        /// Fills in every class property the map file left unset, using the definition's declared defaults.
        private static void ApplyPropertyDefaults(
            FuncGodotData.EntityData entity,
            Dictionary<string, Variant> defaults,
            Dictionary<string, Variant> descriptions)
        {
            foreach (KeyValuePair<string, Variant> entry in defaults)
            {
                if (entity.Properties.ContainsKey(entry.Key))
                {
                    continue;
                }

                Variant propertyDefault = entry.Value;

                switch (propertyDefault.VariantType)
                {
                    case Variant.Type.Array:
                        {
                            // Flags: the default is the sum of every flag marked on by default.
                            int flags = 0;

                            foreach (Variant flag in propertyDefault.AsGodotArray())
                            {
                                Godot.Collections.Array values = flag.AsGodotArray();

                                if (values.Count > 2
                                    && values[2].AsBool()
                                    && values[1].VariantType == Variant.Type.Int)
                                {
                                    flags += values[1].AsInt32();
                                }
                            }

                            entity.Properties[entry.Key] = flags;
                            break;
                        }
                    case Variant.Type.Dictionary:
                        {
                            // Choices: the description may name the default, otherwise take the first choice.
                            if (descriptions.TryGetValue(entry.Key, out Variant description)
                                && description.VariantType == Variant.Type.Array)
                            {
                                Godot.Collections.Array values = description.AsGodotArray();

                                if (values.Count > 1
                                    && (values[1].VariantType == Variant.Type.Int
                                        || values[1].VariantType == Variant.Type.String))
                                {
                                    entity.Properties[entry.Key] = values[1];
                                    break;
                                }
                            }

                            // Otherwise the first choice in the dictionary wins.
                            Godot.Collections.Dictionary choices = propertyDefault.AsGodotDictionary();
                            Variant firstChoice = 0;

                            foreach (Variant choice in choices.Keys)
                            {
                                firstChoice = choices[choice];
                                break;
                            }

                            entity.Properties[entry.Key] = firstChoice;

                            break;
                        }
                    case Variant.Type.Object:
                        {
                            // Materials, shaders, and sounds carry through as their resource path.
                            entity.Properties[entry.Key] = propertyDefault.AsGodotObject() is Resource resource
                                ? resource.ResourcePath
                                : string.Empty;

                            break;
                        }
                    case Variant.Type.NodePath:
                    case Variant.Type.Nil:
                        {
                            entity.Properties[entry.Key] = string.Empty;
                            break;
                        }
                    default:
                        {
                            entity.Properties[entry.Key] = propertyDefault;
                            break;
                        }
                }
            }
        }

        #endregion

        #region TEXT

        /// The index of the next quote that is not backslash-escaped, or -1.
        private static int FindUnescapedQuote(string text, int start = 0)
        {
            for (int index = Math.Max(start, 0); index < text.Length; index++)
            {
                if (text[index] != '"')
                {
                    continue;
                }

                int backslashes = 0;

                for (int check = index - 1; check >= 0 && text[check] == '\\'; check--)
                {
                    backslashes++;
                }

                if (backslashes % 2 == 0)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Parses a <c>"key" "value"</c> line. <paramref name="complete"/> is false when the value's closing
        /// quote is missing, meaning it continues on the following lines.
        /// </summary>
        private static bool TryParseQuotedKeyValue(
            string line,
            out string key,
            out string value,
            out bool complete,
            out string trailing)
        {
            key = string.Empty;
            value = string.Empty;
            complete = false;
            trailing = string.Empty;

            if (!line.StartsWith('"'))
            {
                return false;
            }

            int keyEnd = FindUnescapedQuote(line, 1);

            if (keyEnd < 0)
            {
                return false;
            }

            key = line.Substring(1, keyEnd - 1);

            int valueOpen = keyEnd + 1;

            while (valueOpen < line.Length && line[valueOpen] <= ' ')
            {
                valueOpen++;
            }

            if (valueOpen >= line.Length || line[valueOpen] != '"')
            {
                return false;
            }

            valueOpen++;

            int valueEnd = FindUnescapedQuote(line, valueOpen);

            if (valueEnd < 0)
            {
                value = line[valueOpen..];
                return true;
            }

            complete = true;
            value = line[valueOpen..valueEnd];
            trailing = line[(valueEnd + 1)..].Trim();

            return true;
        }

        private static float[] SplitFloats(string text)
        {
            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            float[] values = new float[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                values[i] = float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                    ? value
                    : 0.0f;
            }

            return values;
        }

        private static bool TryReadFloats(
            string raw,
            int count,
            string property,
            string classname,
            string typeName,
            out float[] components)
        {
            components = SplitFloats(raw);

            if (components.Length >= count)
            {
                return true;
            }

            GD.PushError($"Invalid {typeName} format for '{property}' in entity '{classname}': {raw}");
            return false;
        }

        #endregion
    }
}
