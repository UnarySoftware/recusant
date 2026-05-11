using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiSettingsRootTab : UiUnit<UiSettingsState>
    {
        [UiElement("%RootTabs")]
        private TabBar _rootTabs;

        [UiElement("%Game")]
        private UiSettingsTabBase _game;

        [UiElement("%Graphics")]
        private UiSettingsTabBase _graphics;

        [UiElement("%Controls")]
        private UiSettingsTabBase _controls;

        [UiElement("%Debug")]
        private UiSettingsTabBase _debug;

        private enum CurrentSelection : long
        {
            Game = 0,
            Graphics,
            Controls,
            Debug
        };

        private CurrentSelection _selection = CurrentSelection.Game;

        private readonly Dictionary<CurrentSelection, UiSettingsTabBase> _tabsContents = [];

        public override void Initialize()
        {
#if !TOOLS
            _debug.Visible = false;
#endif

            _tabsContents[CurrentSelection.Game] = _game;
            _tabsContents[CurrentSelection.Graphics] = _graphics;
            _tabsContents[CurrentSelection.Controls] = _controls;
            _tabsContents[CurrentSelection.Debug] = _debug;

            foreach (var entry in _tabsContents)
            {
                UiManager.Singleton.Resolve(entry.Value, entry.Value);
                entry.Value.Initialize();
                entry.Value.Visible = false;
            }

            _rootTabs.TabChanged += OnTabChanged;
        }

        public override void Deinitialize()
        {
            _rootTabs.TabChanged -= OnTabChanged;

            foreach (var entry in _tabsContents)
            {
                entry.Value.Deinitialize();
            }
        }

        public override void Open()
        {
            _tabsContents[_selection].Open();
        }

        public override void Close()
        {
            _tabsContents[_selection].Close();
        }

        public override void Process(float delta)
        {
            foreach (var tab in _tabsContents)
            {
                tab.Value.Process(delta);
            }
        }

        private void OnTabChanged(long tab)
        {
            CurrentSelection selection = (CurrentSelection)tab;

            if (selection == _selection)
            {
                return;
            }

            _tabsContents[_selection].Visible = false;
            _tabsContents[_selection].Close();

            _selection = selection;

            _tabsContents[_selection].Visible = true;
            _tabsContents[_selection].Open();
        }
    }
}
