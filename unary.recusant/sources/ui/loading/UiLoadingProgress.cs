using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiLoadingProgress : UiUnit<UiLoadingState>
    {
        public const int MaxEntriesCount = 16;

        [UiElement("%LoadingEntries")]
        private VBoxContainer _entries;

        [UiElement("%LoadingEntry")]
        private Label _entry;

        [UiElement("%ProgressBar")]
        private ProgressBar _progress;

        private readonly Queue<Label> _freeQueue = [];
        private readonly Dictionary<uint, Label> _entriesDictionary = [];

        public override void Initialize()
        {
            _freeQueue.Enqueue(_entry);
            _entry.Visible = false;

            while (_freeQueue.Count < MaxEntriesCount)
            {
                Label label = (Label)_entry.Duplicate();
                _freeQueue.Enqueue(label);
                label.Visible = false;
                _entries.AddChild(label);
            }

            LoadingManager.Singleton.OnJobAdded.Subscribe(OnJobAdded, this);
            LoadingManager.Singleton.OnJobFinished.Subscribe(OnJobFinished, this);
        }

        public override void Deinitialize()
        {
            LoadingManager.Singleton.OnJobAdded.Unsubscribe(this);
            LoadingManager.Singleton.OnJobFinished.Unsubscribe(this);
        }

        private bool OnJobAdded(ref LoadingManager.JobData data)
        {
            Label label = _freeQueue.Dequeue();
            label.Text = data.Job.Name;
            label.Visible = true;
            _entries.MoveChild(label, -1);
            _entriesDictionary[data.Id] = label;
            return true;
        }

        private bool OnJobFinished(ref LoadingManager.JobData data)
        {
            if (!_entriesDictionary.TryGetValue(data.Id, out var label))
            {
                RuntimeLogger.Error(this, $"Failed to release label for a job id: \"{data.Id}\" with text: \"{data.Job.Name}\"");
                return true;
            }

            label.Visible = false;
            _freeQueue.Enqueue(label);

            return true;
        }

        public override void Process(float delta)
        {
            if (!Opened)
            {
                return;
            }

            _progress.Value = LoadingManager.Singleton.GlobalValue;
        }
    }
}
