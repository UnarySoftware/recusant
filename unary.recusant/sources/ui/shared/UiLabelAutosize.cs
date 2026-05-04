using Godot;
using System.Collections.Generic;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiLabelAutosize : Label
    {
        [Export]
        public string TargetText
        {
            get
            {
                return Text;
            }
            set
            {
                Text = TargetText;
                if (_initialized)
                {
                    TryResizing();
                }
            }
        }

        private readonly List<int> _entries = [];

        private void RepopulateEntries()
        {
            _entries.Clear();

            for (int i = MinSize; i <= MaxSize; i += SizeStep)
            {
                _entries.Add(i);
            }
        }

        [Export]
        public int MinSize
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                if (_initialized)
                {
                    RepopulateEntries();
                }
            }
        } = 8;

        [Export]
        public int MaxSize
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                if (_initialized)
                {
                    RepopulateEntries();
                }
            }
        } = 256;

        [Export]
        public int SizeStep
        {
            get
            {
                return field;
            }
            set
            {
                field = value;
                if (_initialized)
                {
                    RepopulateEntries();
                }
            }
        } = 4;

        private bool _initialized = false;

        public override void _Ready()
        {
            ItemRectChanged += TryResizing;
            _initialized = true;
            RepopulateEntries();
            TryResizing();
        }

        private bool CheckTextFits(int fontSize)
        {
            Vector2 size = Size;
            Vector2 resolvedSize = LabelSettings.Font.GetMultilineStringSize(Text, HorizontalAlignment, -1, fontSize, MaxLinesVisible);

            float width = size.X - resolvedSize.X;
            float height = size.Y - resolvedSize.Y;

            if (width > 0.1f && height > 0.1f)
            {
                return true;
            }

            return false;
        }

        private void TryResizing()
        {
            LabelSettings ??= new();

            if (LabelSettings.Font == null)
            {
                return;
            }

            int left = 0;
            int right = _entries.Count - 1;
            int bestSizeIndex = -1;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                int fontSize = _entries[mid];

                if (CheckTextFits(fontSize))
                {
                    bestSizeIndex = mid;
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            if (bestSizeIndex != -1)
            {
                int optimalFontSize = _entries[bestSizeIndex];
                LabelSettings.FontSize = optimalFontSize;
            }
        }
    }
}
