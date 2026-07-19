// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

using Godot;
using System.Text;

namespace FuncGodot
{
    /// <summary>
    /// FGD PointClass entity definition. Generates either the node named by
    /// <see cref="FuncGodotFGDEntityClass.NodeClass"/> or an instance of <see cref="SceneFile"/>.
    /// </summary>
    [Tool]
    [GlobalClass]
    public partial class FuncGodotFGDPointClass : FuncGodotFGDEntityClass
    {
        public FuncGodotFGDPointClass()
        {
            Prefix = "@PointClass";
        }

        /// Scene to instantiate on map build. Overrides NodeClass and ScriptClass.
        [Export]
        public PackedScene SceneFile;

        /// Script to attach to the generated node. Ignored when SceneFile is set.
        [Export]
        public Script ScriptClass;

        /// <summary>
        /// Rotate the generated node from the <c>angles</c>, <c>mangle</c>, or <c>angle</c> properties, in that
        /// order of priority. Disable to handle rotation yourself.
        /// </summary>
        [Export]
        public bool ApplyRotationOnMapBuild = true;

        /// <summary>
        /// Scale the generated node from the <c>scale</c> property, which may be a float, Vector2, or Vector3.
        /// Disable to handle scale yourself.
        /// </summary>
        [Export]
        public bool ApplyScaleOnMapBuild = true;

        /// <summary>
        /// How this entity appears in TrenchBroom. With several descriptors, the first one without a
        /// <see cref="FuncGodotFGDPointClassDisplayDescriptor.Conditional"/> becomes the default; conditional
        /// descriptors are written in array order.
        /// </summary>
        [Export]
        public Godot.Collections.Array<FuncGodotFGDPointClassDisplayDescriptor> DisplayDescriptors = [];

        public override string BuildDefText()
        {
            if (DisplayDescriptors.Count > 0)
            {
                string displayText = BuildModelText();

                if (!string.IsNullOrEmpty(displayText))
                {
                    MetaProperties["model"] = displayText;
                }
            }

            return base.BuildDefText();
        }

        /// A single descriptor, written either as a bare path or as a TrenchBroom model expression object.
        private static string BuildModelBranchText(FuncGodotFGDPointClassDisplayDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return string.Empty;
            }

            bool usesOptions = !string.IsNullOrEmpty(descriptor.Scale)
                || !string.IsNullOrEmpty(descriptor.Skin)
                || !string.IsNullOrEmpty(descriptor.Frame);

            if (!usesOptions)
            {
                return descriptor.DisplayAssetPath;
            }

            StringBuilder model = new();
            model.Append("{ \"path\": ").Append(descriptor.DisplayAssetPath);

            if (!string.IsNullOrEmpty(descriptor.Skin))
            {
                model.Append(", \"skin\": ").Append(descriptor.Skin);
            }

            if (!string.IsNullOrEmpty(descriptor.Frame))
            {
                model.Append(", \"frame\": ").Append(descriptor.Frame);
            }

            if (!string.IsNullOrEmpty(descriptor.Scale))
            {
                model.Append(", \"scale\": ").Append(descriptor.Scale);
            }

            model.Append(" }");

            return model.ToString();
        }

        /// <summary>
        /// Builds the <c>model</c> meta property. Several descriptors become a TrenchBroom switch expression,
        /// with the conditionless descriptor as the fallback branch.
        /// </summary>
        private string BuildModelText()
        {
            if (DisplayDescriptors.Count == 0)
            {
                return string.Empty;
            }

            if (DisplayDescriptors.Count == 1)
            {
                return BuildModelBranchText(DisplayDescriptors[0]);
            }

            StringBuilder model = new();
            model.Append("{{");

            FuncGodotFGDPointClassDisplayDescriptor defaultDisplay = null;

            foreach (FuncGodotFGDPointClassDisplayDescriptor descriptor in DisplayDescriptors)
            {
                if (string.IsNullOrEmpty(descriptor.Conditional))
                {
                    if (defaultDisplay == null)
                    {
                        defaultDisplay = descriptor;
                    }
                    else
                    {
                        GD.PushError($"{Classname} has a Point Class Display Descriptor without required conditionals set. Must have only 1 conditionless Display Descriptor!");
                    }

                    continue;
                }

                model.Append(descriptor.Conditional).Append(" -> ").Append(BuildModelBranchText(descriptor)).Append(", ");
            }

            if (defaultDisplay != null)
            {
                model.Append(BuildModelBranchText(defaultDisplay)).Append(" }}");
                return model.ToString();
            }

            string result = model.ToString();

            if (result.EndsWith(", "))
            {
                result = result[..^2];
            }

            return result + " }}";
        }
    }
}
