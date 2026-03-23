#if TOOLS

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginTypeResourceInspector : EditorInspectorPlugin, IPluginSystem
    {
        bool ISystem.Initialize()
        {
            this.GetPlugin().AddInspectorPlugin(this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            this.GetPlugin().RemoveInspectorPlugin(this);
        }

        public override bool _CanHandle(GodotObject @object)
        {
            if (@object is TypeResource)
            {
                return true;
            }
            return false;
        }

        public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name, PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
        {
            if (name != nameof(TypeResource.TargetValue))
            {
                return false;
            }

            var typeResource = (TypeResource)@object;

            List<Types.ClassData> targetTypes = [];
            Dictionary<string, int> nameToIndex = [];
            int index = 0;

            if (typeResource.BaseType == null)
            {
                foreach (var targetType in Types.TypeToData)
                {
                    targetTypes.Add(targetType.Value);
                    nameToIndex[targetType.Value.Type] = index;
                    index++;
                }
            }
            else
            {
                string baseName = typeResource.BaseType.Name;

                foreach (var targetType in Types.TypeToData)
                {
                    Types.ClassData data = targetType.Value;

                    while (true)
                    {
                        if (string.IsNullOrEmpty(data.BaseType))
                        {
                            break;
                        }

                        if (!Types.TypeToData.TryGetValue(data.BaseType, out var BaseResult))
                        {
                            break;
                        }

                        if (data.BaseType == baseName)
                        {
                            targetTypes.Add(targetType.Value);
                            nameToIndex[targetType.Key] = index;
                            index++;
                        }

                        data = BaseResult;
                    }
                }
            }

            HBoxContainer root = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            Label nameLabel = new()
            {
                Text = "Value"
            };

            root.AddChild(nameLabel);

            VBoxContainer infoList = new();

            root.AddChild(infoList);

            VBoxContainer dataList = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            root.AddChild(dataList);

            Label filterLabel = new()
            {
                Text = "Filter:"
            };

            infoList.AddChild(filterLabel);

            Label selectionLabel = new()
            {
                Text = "Selection:"
            };

            infoList.AddChild(selectionLabel);

            LineEdit filterLine = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            dataList.AddChild(filterLine);

            OptionButton options = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };

            dataList.AddChild(options);

            string previousValue = typeResource.TargetValue;

            if (Types.UidToData.TryGetValue(typeResource.TargetValue, out var entry))
            {
                previousValue = entry.Type;
            }

            filterLine.Text = previousValue;
            filterLine.TextChanged += (text) => OnTextChanged(text, typeResource, targetTypes, nameToIndex, filterLine, options);

            options.ItemSelected += (index) => OnTypeSelected((int)index, typeResource, targetTypes, nameToIndex, filterLine, options);

            AddPropertyEditor(name, root, false, name);

            OnTextChanged(filterLine.Text, typeResource, targetTypes, nameToIndex, filterLine, options);
            return true;
        }

        private void OnTextChanged(string text, TypeResource typeResource, List<Types.ClassData> targetTypeLists, Dictionary<string, int> nameToIndex, LineEdit lineEdit, OptionButton options)
        {
            options.Clear();

            int index = 0;

            foreach (var targetType in targetTypeLists)
            {
                if (targetType.Type.Contains(text, StringComparison.CurrentCultureIgnoreCase))
                {
                    options.AddItem(targetType.Type, index);
                }
                index++;
            }

            if (options.ItemCount > 0)
            {
                OnTypeSelected(0, typeResource, targetTypeLists, nameToIndex, lineEdit, options);
            }
        }

        private void OnTypeSelected(int index, TypeResource typeResourceObj, List<Types.ClassData> targetTypeLists, Dictionary<string, int> nameToIndex, LineEdit lineEdit, OptionButton options)
        {
            string fullName = options.GetItemText(index);
            int realIndex = nameToIndex[fullName];

            var target = targetTypeLists[realIndex];

            if (target.Uid == string.Empty)
            {
                typeResourceObj.TargetValue = target.Type;
            }
            else
            {
                typeResourceObj.TargetValue = target.Uid;
            }

            typeResourceObj.EmitChanged();
        }
    }
}

#endif
