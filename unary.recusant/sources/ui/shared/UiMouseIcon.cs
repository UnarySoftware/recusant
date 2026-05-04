using Godot;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiMouseIcon : Control
    {
        private TextureRect _left;
        private TextureRect _right;
        private TextureRect _wheelUp;
        private TextureRect _wheelDown;
        private bool _initialized = false;

        public override void _Ready()
        {
            _left = GetNode<TextureRect>("%Left");
            _right = GetNode<TextureRect>("%Right");
            _wheelUp = GetNode<TextureRect>("%WheelUp");
            _wheelDown = GetNode<TextureRect>("%WheelDown");
            _initialized = true;
            LeftType = LeftType;
            RightType = RightType;
            Scroll = Scroll;
        }

        public enum ButtonType
        {
            Default,
            Pressed
        };

        [Export]
        public ButtonType LeftType
        {
            get
            {
                return field;
            }
            set
            {
                field = value;

                if (!_initialized)
                {
                    return;
                }

                if (field == ButtonType.Default)
                {
                    _left.Texture = LeftDefault;
                }
                else if (field == ButtonType.Pressed)
                {
                    _left.Texture = LeftPressed;
                }
            }
        }

        [Export]
        public ButtonType RightType
        {
            get
            {
                return field;
            }
            set
            {
                field = value;

                if (!_initialized)
                {
                    return;
                }

                if (field == ButtonType.Default)
                {
                    _right.Texture = RightDefault;
                }
                else if (field == ButtonType.Pressed)
                {
                    _right.Texture = RightPressed;
                }
            }
        }

        public enum ScrollType
        {
            Default,
            Pressed,
            GradientUp,
            GradientDown,
            GradientBoth
        };

        [Export]
        public ScrollType Scroll
        {
            get
            {
                return field;
            }
            set
            {
                field = value;

                if (!_initialized)
                {
                    return;
                }

                if (field == ScrollType.Default)
                {
                    _wheelUp.Texture = ScrollUpDefault;
                    _wheelDown.Texture = ScrollDownDefault;
                }
                else if (field == ScrollType.Pressed)
                {
                    _wheelUp.Texture = ScrollUpPressed;
                    _wheelDown.Texture = ScrollDownPressed;
                }
                else if (field == ScrollType.GradientUp)
                {
                    _wheelUp.Texture = ScrollUpGradient;
                    _wheelDown.Texture = ScrollDownDefault;
                }
                else if (field == ScrollType.GradientDown)
                {
                    _wheelUp.Texture = ScrollUpDefault;
                    _wheelDown.Texture = ScrollDownGradient;
                }
                else if (field == ScrollType.GradientBoth)
                {
                    _wheelUp.Texture = ScrollUpGradient;
                    _wheelDown.Texture = ScrollDownGradient;
                }
            }
        }

        [ExportGroup("Buttons")]
        [Export]
        public Texture2D LeftDefault;

        [Export]
        public Texture2D LeftPressed;

        [Export]
        public Texture2D RightDefault;

        [Export]
        public Texture2D RightPressed;

        [ExportGroup("Scroll Wheel")]
        [Export]
        public Texture2D ScrollUpDefault;

        [Export]
        public Texture2D ScrollUpPressed;

        [Export]
        public Texture2D ScrollUpGradient;

        [Export]
        public Texture2D ScrollDownDefault;

        [Export]
        public Texture2D ScrollDownPressed;

        [Export]
        public Texture2D ScrollDownGradient;
    }
}
