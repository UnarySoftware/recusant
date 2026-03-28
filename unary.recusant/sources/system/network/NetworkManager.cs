using System.Collections.Generic;
using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class NetworkManager : Node, IModSystem
    {
        public static EditorSettingVariable<string[]> BannedExtensions = new()
        {
            EditorDefault = [".gdignore", ".patch", ".fgd", ".vmt", ".vtf", "gameinfo.txt", ".vmf", ".vmx", ".cs", ".uid", ".import"],
            RuntimeDefault = [],
            PropertyHint = PropertyHint.ArrayType,
            HintText = "String"
        };

        private bool _local = true;
        private BaseTransport _transport;
        private int _port = 0;

        bool ISystem.Initialize()
        {
            if (Steam.Initialized)
            {
                _local = false;
            }

            //if(_local)
            {
                _port = 55555;
                _transport = new ENetTransport();
            }

            return true;
        }

        public void StartHost()
        {
            _transport.StartHost(_port);
        }

        public void StartClient()
        {
            _transport.StartClient(_port);
        }

        void ISystem.Process(float delta)
        {
            _transport.Process();
        }
    }
}
