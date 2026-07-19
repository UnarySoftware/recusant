// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;
using System.Collections.Generic;
using System.Text;

namespace FuncGodot
{
    /// <summary>
    /// Shared entity definition template. Not usable directly: use <see cref="FuncGodotFGDBaseClass"/>,
    /// <see cref="FuncGodotFGDSolidClass"/>, or <see cref="FuncGodotFGDPointClass"/>.
    /// </summary>
    [Tool]
    [GlobalClass]
    public abstract partial class FuncGodotFGDEntityClass : Resource
    {
        protected FuncGodotFGDEntityClass()
        {
            // Initialized here rather than inline: an inline initializer makes the source generator construct
            // this typed array while the class is still being registered, before FuncGodotFGDBaseClass' script
            // is available, which fails with "Script class can only be set together with base class name."
            BaseClasses = [];
        }

        /// FGD class prefix, set by each concrete class (@SolidClass, @PointClass, @BaseClass).
        public string Prefix { get; protected set; } = string.Empty;

        /// Entity classname. Required: both TrenchBroom and the map build match entities on it.
        [Export]
        public string Classname = string.Empty;

        [Export(PropertyHint.MultilineText)]
        public string Description = string.Empty;

        /// Entity is used by the map build but never written to the exported FGD.
        [Export]
        public bool FuncGodotInternal = false;

        /// Base classes to inherit class properties and descriptions from.
        [Export]
        public Godot.Collections.Array<FuncGodotFGDBaseClass> BaseClasses;

        /// <summary>
        /// Key/value pairs that appear in TrenchBroom. After the map is built these are collected into a
        /// dictionary applied to the generated node's <c>func_godot_properties</c>.
        /// </summary>
        [Export]
        public Godot.Collections.Dictionary<string, Variant> ClassProperties = [];

        /// Editor descriptions for the entries in <see cref="ClassProperties"/>.
        [Export]
        public Godot.Collections.Dictionary<string, Variant> ClassPropertyDescriptions = [];

        /// <summary>
        /// Applies class properties to identically named properties on the generated node. Properties must be
        /// the matching type or the build reports an error.
        /// </summary>
        [Export]
        public bool AutoApplyToMatchingNodeProperties = false;

        /// Appearance properties for TrenchBroom (size, color, model, ...).
        [Export]
        public Godot.Collections.Dictionary<string, Variant> MetaProperties = new()
        {
            { "size", new Aabb(new Vector3(-8, -8, -8), new Vector3(8, 8, 8)) },
            { "color", new Color(0.8f, 0.8f, 0.8f) },
        };

        /// <summary>
        /// Node to generate on map build: a built-in Godot class, a script class, or a GDExtension class.
        /// Leave blank on point classes that instantiate a scene instead.
        /// </summary>
        [Export]
        public string NodeClass = string.Empty;

        /// Class property to name the generated node after. Overrides the map settings' entity name property.
        [Export]
        public string NameProperty = string.Empty;

        [Export]
        public Godot.Collections.Array<string> NodeGroups = [];

        /// <summary>
        /// Optional script whose <c>_func_godot_attach_properties</c> is called with this resource during FGD
        /// export, so class properties can be generated programmatically.
        /// </summary>
        [Export]
        public Script EntityExtensionScript;

        /// Parses the definition and outputs it in the FGD format.
        public virtual string BuildDefText()
        {
            StringBuilder result = new();
            result.Append(Prefix);

            // Attach generated class properties before anything is read off this resource.
            if (EntityExtensionScript != null)
            {
                GodotObject extension = InstantiateScript(EntityExtensionScript);

                if (extension != null && extension.HasMethod("_func_godot_attach_properties"))
                {
                    extension.Call("_func_godot_attach_properties", this);
                }
            }

            Godot.Collections.Dictionary<string, Variant> metaProperties = MetaProperties.Duplicate();

            StringBuilder baseNames = new();

            foreach (FuncGodotFGDBaseClass baseClass in BaseClasses)
            {
                if (baseClass == null)
                {
                    continue;
                }

                if (baseNames.Length > 0)
                {
                    baseNames.Append(", ");
                }

                baseNames.Append(baseClass.Classname);
            }

            if (baseNames.Length > 0)
            {
                metaProperties["base"] = baseNames.ToString();
            }

            foreach (string property in metaProperties.Keys)
            {
                // Solid classes derive their bounds from their brushes.
                if (Prefix == "@SolidClass" && (property == "size" || property == "model"))
                {
                    continue;
                }

                Variant value = metaProperties[property];

                result.Append(' ').Append(property).Append('(');

                switch (value.VariantType)
                {
                    case Variant.Type.Aabb:
                        {
                            Aabb aabb = value.AsAabb();
                            result.Append($"{aabb.Position.X} {aabb.Position.Y} {aabb.Position.Z}, {aabb.Size.X} {aabb.Size.Y} {aabb.Size.Z}");
                            break;
                        }
                    case Variant.Type.Color:
                        {
                            Color color = value.AsColor();
                            result.Append($"{color.R8} {color.G8} {color.B8}");
                            break;
                        }
                    case Variant.Type.String:
                        {
                            result.Append(value.AsString());
                            break;
                        }
                    case Variant.Type.Dictionary:
                        {
                            result.Append(Json.Stringify(value));
                            break;
                        }
                }

                result.Append(')');
            }

            result.Append(" = ").Append(Classname);

            // Base classes crash some editors when given a description, and so does an empty one.
            if (Prefix != "@BaseClass")
            {
                string description = Description.Replace("\"", "'");

                result.Append(string.IsNullOrEmpty(description)
                    ? $" : \"{Classname}\" "
                    : $" : \"{description}\" ");
            }

            string newline = FuncGodotUtil.Newline();

            result.Append(ClassProperties.Count > 0 ? "[" + newline : "[");

            foreach (string property in ClassProperties.Keys)
            {
                Variant value = ClassProperties[property];

                string propertyType = string.Empty;
                string propertyValue = string.Empty;
                string propertyDescription = BuildPropertyDescription(property, value);

                switch (value.VariantType)
                {
                    case Variant.Type.Int:
                        {
                            propertyType = "integer";
                            propertyValue = value.AsInt64().ToString();
                            break;
                        }
                    case Variant.Type.Float:
                        {
                            propertyType = "float";
                            propertyValue = "\"" + value.AsDouble() + "\"";
                            break;
                        }
                    case Variant.Type.String:
                        {
                            propertyType = "string";
                            propertyValue = "\"" + value.AsString() + "\"";
                            break;
                        }
                    case Variant.Type.Bool:
                        {
                            propertyType = "choices";
                            propertyValue = newline + "\t[" + newline
                                + "\t\t0 : \"No\"" + newline
                                + "\t\t1 : \"Yes\"" + newline
                                + "\t]";
                            break;
                        }
                    case Variant.Type.Vector2:
                        {
                            Vector2 vector = value.AsVector2();
                            propertyType = "string";
                            propertyValue = $"\"{vector.X} {vector.Y}\"";
                            break;
                        }
                    case Variant.Type.Vector2I:
                        {
                            Vector2I vector = value.AsVector2I();
                            propertyType = "string";
                            propertyValue = $"\"{vector.X} {vector.Y}\"";
                            break;
                        }
                    case Variant.Type.Vector3:
                        {
                            Vector3 vector = value.AsVector3();
                            propertyType = "string";
                            propertyValue = $"\"{vector.X} {vector.Y} {vector.Z}\"";
                            break;
                        }
                    case Variant.Type.Vector3I:
                        {
                            Vector3I vector = value.AsVector3I();
                            propertyType = "string";
                            propertyValue = $"\"{vector.X} {vector.Y} {vector.Z}\"";
                            break;
                        }
                    case Variant.Type.Vector4:
                        {
                            Vector4 vector = value.AsVector4();
                            propertyType = "string";
                            propertyValue = $"\"{vector.X} {vector.Y} {vector.Z} {vector.W}\"";
                            break;
                        }
                    case Variant.Type.Vector4I:
                        {
                            Vector4I vector = value.AsVector4I();
                            propertyType = "string";
                            propertyValue = $"\"{vector.X} {vector.Y} {vector.Z} {vector.W}\"";
                            break;
                        }
                    case Variant.Type.Color:
                        {
                            Color color = value.AsColor();
                            propertyType = "color255";
                            propertyValue = $"\"{color.R8} {color.G8} {color.B8}\"";
                            break;
                        }
                    case Variant.Type.Dictionary:
                        {
                            propertyType = "choices";

                            StringBuilder choices = new();
                            choices.Append(newline).Append("\t[").Append(newline);

                            Godot.Collections.Dictionary dictionary = value.AsGodotDictionary();

                            foreach (Variant choice in dictionary.Keys)
                            {
                                Variant choiceValue = dictionary[choice];
                                string choiceString = choiceValue.ToString();

                                if (choiceValue.VariantType == Variant.Type.String && !choiceString.StartsWith('"'))
                                {
                                    choiceString = "\"" + choiceString + "\"";
                                }

                                choices.Append("\t\t").Append(choiceString)
                                    .Append(" : \"").Append(choice).Append('"').Append(newline);
                            }

                            choices.Append("\t]");
                            propertyValue = choices.ToString();
                            break;
                        }
                    case Variant.Type.Array:
                        {
                            propertyType = "flags";

                            StringBuilder flags = new();
                            flags.Append(newline).Append("\t[").Append(newline);

                            foreach (Variant flag in value.AsGodotArray())
                            {
                                Godot.Collections.Array entry = flag.AsGodotArray();

                                if (entry.Count < 3)
                                {
                                    GD.PushError($"{property} has an incorrect flag format. Should be [String name, int value, bool default].");
                                    continue;
                                }

                                flags.Append("\t\t").Append(entry[1])
                                    .Append(" : \"").Append(entry[0]).Append("\" : ")
                                    .Append(entry[2].AsBool() ? "1" : "0")
                                    .Append(newline);
                            }

                            flags.Append("\t]");
                            propertyValue = flags.ToString();
                            break;
                        }
                    case Variant.Type.NodePath:
                        {
                            propertyType = "target_destination";
                            propertyValue = "\"\"";
                            break;
                        }
                    case Variant.Type.Object:
                        {
                            if (value.AsGodotObject() is Resource resource)
                            {
                                propertyValue = "\"" + resource.ResourcePath + "\"";

                                propertyType = resource switch
                                {
                                    Material => "material",
                                    Texture2D => "decal",
                                    AudioStream => "sound",
                                    _ => propertyType,
                                };
                            }
                            else
                            {
                                propertyType = "target_source";
                                propertyValue = "\"\"";
                            }

                            break;
                        }
                }

                if (string.IsNullOrEmpty(propertyValue))
                {
                    continue;
                }

                result.Append('\t').Append(property).Append('(').Append(propertyType).Append(')');

                bool isFlags = value.VariantType == Variant.Type.Array;
                bool isChoices = value.VariantType == Variant.Type.Dictionary;

                if (!isFlags && (!isChoices || !string.IsNullOrEmpty(propertyDescription)))
                {
                    result.Append(" : ").Append(propertyDescription);
                }

                if (value.VariantType == Variant.Type.Bool)
                {
                    result.Append(value.AsBool() ? " : 1 = " : " : 0 = ");
                }
                else if (isFlags || isChoices)
                {
                    result.Append(" = ");
                }
                else
                {
                    result.Append(" : ");
                }

                result.Append(propertyValue).Append(newline);
            }

            result.Append(']').Append(newline);

            return result.ToString();
        }

        /// <summary>
        /// Builds the quoted description for a class property. Choices properties may carry their default
        /// value alongside the description as a <c>[String description, int/String default]</c> array.
        /// </summary>
        private string BuildPropertyDescription(string property, Variant value)
        {
            if (!ClassPropertyDescriptions.TryGetValue(property, out Variant description))
            {
                return "\"\"";
            }

            if (value.VariantType != Variant.Type.Dictionary || description.VariantType != Variant.Type.Array)
            {
                return "\"" + description.AsString() + "\"";
            }

            Godot.Collections.Array entry = description.AsGodotArray();

            if (entry.Count > 1
                && (entry[1].VariantType == Variant.Type.Int || entry[1].VariantType == Variant.Type.String))
            {
                string defaultValue = entry[1].VariantType == Variant.Type.Int
                    ? entry[1].AsInt64().ToString()
                    : "\"" + entry[1].AsString() + "\"";

                return "\"" + entry[0].AsString() + "\" : " + defaultValue;
            }

            GD.PushError($"{property} has incorrect description format. Should be [String description, int / String default value].");
            return "\"\" : 0";
        }

        /// Instantiates a GDScript or C# script resource, matching GDScript's <c>Script.new()</c>.
        private static GodotObject InstantiateScript(Script script)
        {
            return script switch
            {
                CSharpScript cSharpScript => cSharpScript.New().AsGodotObject(),
                _ => null,
            };
        }

        /// Collects this class' properties plus everything inherited from its base classes.
        public Dictionary<string, Variant> RetrieveAllClassProperties(Dictionary<string, Variant> properties = null)
        {
            properties ??= [];

            foreach (string key in ClassProperties.Keys)
            {
                properties[key] = ClassProperties[key];
            }

            foreach (FuncGodotFGDBaseClass baseClass in BaseClasses)
            {
                baseClass?.RetrieveAllClassProperties(properties);
            }

            return properties;
        }

        public Dictionary<string, Variant> RetrieveAllClassPropertyDescriptions(Dictionary<string, Variant> descriptions = null)
        {
            descriptions ??= [];

            foreach (string key in ClassPropertyDescriptions.Keys)
            {
                descriptions[key] = ClassPropertyDescriptions[key];
            }

            foreach (FuncGodotFGDBaseClass baseClass in BaseClasses)
            {
                baseClass?.RetrieveAllClassPropertyDescriptions(descriptions);
            }

            return descriptions;
        }
    }
}
