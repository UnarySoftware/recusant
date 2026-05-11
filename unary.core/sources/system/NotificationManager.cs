using Godot;
using System;
using System.Collections.Generic;
using System.Text;

namespace Unary.Core
{
    public partial class NotificationManager : Node, ICoreSystem
    {
        public struct NotificationData
        {
            public string Header;
            public string Text;
        }

        private readonly Queue<NotificationData> _data = [];

        public bool HasData
        {
            get
            {
                return _data.Count > 0;
            }
        }

        public NotificationData Data
        {
            get
            {
                return _data.Dequeue();
            }
        }

        public void SendNotification(string Header, string Text)
        {
            _data.Enqueue(new()
            {
                Header = Header,
                Text = Text
            });
        }
    }
}
