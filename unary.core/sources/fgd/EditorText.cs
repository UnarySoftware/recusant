using Godot;
using System.Collections.Generic;
using System.Text;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    [Icon("res://addons/unary.core.editor/icons/Brush.svg")]
    public partial class EditorText : BaseFgd
    {
        public const int MinFontSize = 4;
        public const int MaxFontSize = 128;
        public const int DefaultFontSize = 16;

        public const float MinViewDistance = 1.0f;
        public const float MaxViewDistance = 1048576.0f;
        public const float DefaultViewDistance = 4096.0f;

        [Export]
        [FgdProperty("The text to display. Use \\n to break the text into several lines.")]
        public string Text = "Text";

        [Export]
        [FgdProperty("The font size to display the text with, in pixels.")]
        public int Size = DefaultFontSize;

        [Export]
        [FgdProperty("The color to display the text with.")]
        public Color Color = Colors.White;

        [Export]
        [FgdProperty("How far away from the camera, in map units, the text remains visible. It fades out over the last 128 units.")]
        public float Distance = DefaultViewDistance;

        public override void _Ready()
        {
#if TOOLS
            if (Engine.Singleton.IsEditorHint())
            {
                _label = GetNodeOrNull<Label3D>(LabelName);
                return;
            }
#endif

            QueueFree();
        }

#if TOOLS

        private const float LabelGap = 2.0f;
        private const float BoundsExtent = 8.0f;
        private const float FadeDistance = 128.0f;
        private const float OutlineReferenceFontSize = 16.0f;
        private const int OutlineUnitsPerPixel = 4;
        private const float ReferenceViewportHeight = 1080.0f;
        private const float ReferenceFieldOfView = 70.0f;
        private const string LabelName = "label";
        private Label3D _label;

        public override void _Process(double delta)
        {
            if (_label != null && Engine.Singleton.IsEditorHint())
            {
                MatchViewport();
            }
        }

        public override void AppliedProperties()
        {
            List<string> lines = SplitLines(Text);

            if (lines.Count == 0)
            {
                return;
            }

            int fontSize = Mathf.Clamp(Size, MinFontSize, MaxFontSize);
            float viewDistance = Mathf.Clamp(Distance, MinViewDistance, MaxViewDistance);
            float scaleFactor = FuncGodot.FuncGodotConfig.Load()?.ScaleFactor
                ?? 1.0f / FuncGodot.FuncGodotConfig.DefaultInverseScaleFactor;

            Label3D label = new()
            {
                Name = LabelName,
                Text = string.Join('\n', lines),
                FontSize = fontSize,
                OutlineSize = OutlineSizeOf(fontSize),
                Modulate = Color,
                OutlineModulate = Colors.Black,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                FixedSize = true,
                Shaded = false,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Position = new Vector3(0.0f, (BoundsExtent + LabelGap) * scaleFactor, 0.0f),
                VisibilityRangeEnd = viewDistance * scaleFactor,
                VisibilityRangeEndMargin = Mathf.Min(FadeDistance, viewDistance) * scaleFactor,
                VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
            };

            AddChild(label);

            label.Owner = FindOwner();

            _label = label;
            MatchViewport();
        }

        private static int OutlineSizeOf(int fontSize)
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(fontSize / OutlineReferenceFontSize));

            return Mathf.RoundToInt(width * Mathf.Sqrt2 * OutlineUnitsPerPixel);
        }

        private void MatchViewport()
        {
            float pixelSize = ResolvePixelSize();

            if (!Mathf.IsEqualApprox(_label.PixelSize, pixelSize))
            {
                _label.PixelSize = pixelSize;
            }
        }

        private static float ResolvePixelSize()
        {
            SubViewport viewport = EditorInterface.Singleton?.GetEditorViewport3D(0);
            Camera3D camera = viewport?.GetCamera3D();

            float height = viewport?.GetVisibleRect().Size.Y ?? 0.0f;

            if (camera == null || height <= 0.0f)
            {
                return 2.0f * Mathf.Tan(Mathf.DegToRad(ReferenceFieldOfView) * 0.5f) / ReferenceViewportHeight;
            }

            return camera.Projection == Camera3D.ProjectionType.Orthogonal
                ? 2.0f / height
                : 2.0f * Mathf.Tan(Mathf.DegToRad(VerticalFieldOfView(camera, viewport)) * 0.5f) / height;
        }

        private static float VerticalFieldOfView(Camera3D camera, Viewport viewport)
        {
            if (camera.KeepAspect == Camera3D.KeepAspectEnum.Height)
            {
                return camera.Fov;
            }

            Vector2 size = viewport.GetVisibleRect().Size;
            float aspect = size.Y > 0.0f ? size.X / size.Y : 1.0f;

            return Mathf.RadToDeg(2.0f * Mathf.Atan(Mathf.Tan(Mathf.DegToRad(camera.Fov) * 0.5f) / aspect));
        }

        private static List<string> SplitLines(string text)
        {
            List<string> lines = [];
            StringBuilder line = new();

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];

                if (character == '\\' && index + 1 < text.Length)
                {
                    char escaped = text[index + 1];

                    if (escaped == 'n')
                    {
                        lines.Add(line.ToString());
                        line.Clear();
                        index++;
                        continue;
                    }

                    if (escaped == '"')
                    {
                        line.Append(escaped);
                        index++;
                        continue;
                    }
                }

                if (character == '\n')
                {
                    lines.Add(line.ToString());
                    line.Clear();
                    continue;
                }

                line.Append(character);
            }

            lines.Add(line.ToString());

            while (lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return lines;
        }

#endif
    }
}
