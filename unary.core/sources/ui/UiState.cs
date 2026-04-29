
using Godot;
using System;
using System.Collections.Generic;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    [SingletonProvider("Unary.Core.Bootstrap.Singleton.GetSystem<UiManager>().GetState")]
    public abstract partial class UiState : Control
    {
        public bool Opened
        {
            get
            {
                return Visible;
            }
        }

        private Control _rootCache;
        public Control Root
        {
            get
            {
                _rootCache ??= (Control)GetChild(0);
                return _rootCache;
            }
        }

        public Dictionary<Type, UiUnitBase> Units;

        public T GetUnit<T>() where T : UiUnitBase
        {
            if (Units.TryGetValue(typeof(T), out var result))
            {
                return (T)result;
            }
            return null;
        }

        public virtual void Initialize()
        {

        }

        public virtual void Deinitialize()
        {

        }

        public virtual void Open()
        {

        }

        public virtual void Close()
        {

        }

        public virtual void Process(float delta)
        {

        }

        public abstract Type GetBackState();
    }
}
