using Godot;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiSettingsAudio : UiSettingsTabBase
    {
        private static readonly LazyResource<PackedScene> _audioEntryScene = new("uid://cqy7147sanwjc");

        [UiElement("%AudioEntries")]
        private VBoxContainer AudioEntries;

        public override void Initialize()
        {
            var buses = AudioManager.Singleton.Buses;

            List<string> names = [];

            foreach (var bus in buses)
            {
                if (bus.Key != AudioManager.MasterBusName)
                {
                    names.Add(bus.Key);
                }
            }

            names.Sort();
            names.Insert(0, AudioManager.MasterBusName);

            foreach (var busName in names)
            {
                var busIndex = buses[busName];

                ColorRect newBus = (ColorRect)_audioEntryScene.Cache.Instantiate();
                Label label = newBus.GetNode<Label>("%Label");
                HSlider slider = newBus.GetNode<HSlider>("%Slider");
                LineEdit lineEdit = newBus.GetNode<LineEdit>("%Input");

                label.Text = ' ' + busName;

                slider.Value = (int)Mathf.Round(AudioServer.Singleton.GetBusVolumeLinear(busIndex) * 100.0f);

                slider.ValueChanged += value =>
                {
                    float floatValue = (float)value;
                    AudioServer.Singleton.SetBusVolumeLinear(busIndex, floatValue / 100.0f);
                    int volume = (int)Mathf.Round(floatValue);
                    lineEdit.Text = volume.ToString();
                };

                int volume = (int)Mathf.Round(AudioServer.Singleton.GetBusVolumeLinear(busIndex) * 100.0f);
                lineEdit.Text = volume.ToString();

                lineEdit.TextSubmitted += text =>
                {
                    if (int.TryParse(text, out var parsed))
                    {
                        slider.Value = parsed;
                        float volume = parsed / 100.0f;
                        AudioServer.Singleton.SetBusVolumeLinear(busIndex, volume);
                    }
                    else
                    {
                        int volume = (int)Mathf.Round(Mathf.Round(AudioServer.Singleton.GetBusVolumeLinear(busIndex) * 100.0f));
                        lineEdit.Text = volume.ToString();
                    }
                };

                AudioEntries.AddChild(newBus);
            }
        }

        public override void Deinitialize()
        {

        }
    }
}
