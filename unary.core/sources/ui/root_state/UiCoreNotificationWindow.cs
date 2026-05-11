using Godot;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class UiCoreNotificationWindow : UiUnit<UiCoreState>
    {
        [UiElement("%Notification")]
        private MarginContainer _root;

        [UiElement("%NotificationOk")]
        private Button _rootOk;

        [UiElement("%NotificationHeader")]
        private Label _header;

        [UiElement("%NotificationInfo")]
        private Label _label;

        public override void Initialize()
        {
            _root.Visible = false;
            _rootOk.Pressed += OnPressed;
        }

        public override void Deinitialize()
        {
            _rootOk.Pressed -= OnPressed;
        }

        private void OnPressed()
        {
            _root.Visible = false;
        }

        public override void Process(float delta)
        {
            if (!_root.Visible && NotificationManager.Singleton.HasData)
            {
                NotificationManager.NotificationData data = NotificationManager.Singleton.Data;
                _header.Text = data.Header;
                _label.Text = data.Text;
                _root.Visible = true;
            }
        }
    }
}
