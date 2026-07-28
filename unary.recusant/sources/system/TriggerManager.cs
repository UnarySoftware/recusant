using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class TriggerManager : Node, IModSystem, IFgdOwner
    {
        public HashSet<Trigger> Entries { get; private set; } = [];

        bool ISystem.Initialize()
        {
            LevelManager.Singleton.OnLoaded.Subscribe(OnLoaded, this);
            LevelManager.Singleton.OnUnloaded.Subscribe(OnUnloaded, this);
            return true;
        }

        void ISystem.Deinitialize()
        {
            LevelManager.Singleton.OnLoaded.Unsubscribe(this);
            LevelManager.Singleton.OnUnloaded.Unsubscribe(this);
        }

        private bool OnLoaded(ref LevelManager.LevelInfo info)
        {
            Entries = FgdManager.Singleton.OwnByType<Trigger>(this);
            return true;
        }

        private bool OnUnloaded(ref LevelManager.LevelInfo info)
        {
            FgdManager.Singleton.Disown(this, Entries);
            return true;
        }

        public void OnDestroy(BaseFgd fgd)
        {
            Entries.Remove((Trigger)fgd);
        }
    }
}
