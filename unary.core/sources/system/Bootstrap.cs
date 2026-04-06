using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace Unary.Core
{
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Entity.svg")]
    public partial class Bootstrap : Node
    {
        public static Bootstrap Singleton { get; private set; } = null;

        private readonly SystemCollector<ICoreSystem> _systems = new();

        public T GetSystem<T>() where T : ICoreSystem
        {
            return (T)_systems.GetSystem(typeof(T));
        }

        public Action OnFinishInitialization;

        public bool FinishedInitialization { get; private set; }

        public static void Dummy()
        {

        }

        private bool _quitting = false;

        public override void _Ready()
        {
            try
            {
                OnFinishInitialization = Dummy;

                RuntimeLogger.Initialize();

                Singleton = this;

                Types.Initialize(RuntimeLogger.Critical);

                if (!_systems.Initialize(["unary.core"], this))
                {
                    Quit(1);
                    return;
                }

                FinishedInitialization = true;
                OnFinishInitialization();

                if (!_systems.PostInitialize())
                {
                    Quit(1);
                    return;
                }
            }
            catch (Exception ex)
            {
                RuntimeLogger.Critical(this, ex.Message + '\n' + ex.StackTrace);
                Quit(1);
            }
        }

        public void Quit(int exitCode)
        {
            _quitting = true;
            GetTree().Quit(exitCode);
        }

        public override void _Process(double delta)
        {
            if (_quitting)
            {
                return;
            }

            _systems.Process((float)delta);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_quitting)
            {
                return;
            }

            _systems.PhysicsProcess((float)delta);
        }

        public override void _ExitTree()
        {
            _systems.Deinitialize();

            Singleton = null;

            Types.Deinitialize();

            RuntimeLogger.Deinitialize();
        }
    }
}
