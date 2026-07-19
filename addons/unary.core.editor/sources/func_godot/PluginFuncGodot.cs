// Copyright (c) 2023 func-godot
// C# port of func_godot (https://github.com/func-godot/func_godot_plugin),
// used under the MIT License. See addons/unary.core.editor/sources/func_godot/LICENSE.md.

#if TOOLS

using FuncGodot;
using Godot;

namespace Unary.Core.Editor
{
    [Tool]
    public partial class PluginFuncGodot : IPluginSystem
    {
        private QuakeMapImportPlugin _mapImportPlugin;

        bool ISystem.Initialize()
        {
            _mapImportPlugin = new QuakeMapImportPlugin();
            this.GetPlugin().AddImportPlugin(_mapImportPlugin);
            return true;
        }

        void ISystem.Deinitialize()
        {
            if (_mapImportPlugin == null)
            {
                return;
            }

            this.GetPlugin().RemoveImportPlugin(_mapImportPlugin);
            _mapImportPlugin = null;
        }
    }
}

#endif
