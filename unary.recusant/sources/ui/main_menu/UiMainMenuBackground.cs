using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class UiMainMenuBackground : UiUnit<UiMainMenuState>
    {
        [UiElement("%Background")]
        private TextureRect _texture;

        public static LazyResource<Texture2D> DefaultTexture = new("uid://mevml1mjtphx");

        public override void Initialize()
        {
            _texture.Visible = true;
            UiLoadingState.Singleton.Background.Visible = true;
            LevelManager.Singleton.OnLoadStarted.Subscribe(OnLoadStarted, this);
            LevelManager.Singleton.OnLoaded.Subscribe(OnLoadFinished, this);
        }

        public override void Deinitialize()
        {
            LevelManager.Singleton.OnLoadStarted.Unsubscribe(this);
            LevelManager.Singleton.OnLoaded.Unsubscribe(this);
        }

        private bool _finished = false;

        private bool OnLoadStarted(ref LevelManager.LevelInfo data)
        {
            _finished = false;

            Texture2D texture = null;

            LazyResource resource = data.Definition.Texture;

            if (resource != null && resource.TargetValue != string.Empty)
            {
                texture = resource.Load<Texture2D>();
            }

            texture ??= DefaultTexture.Cache;

            _texture.Texture = texture;
            UiLoadingState.Singleton.Background.Texture = texture;

            return true;
        }

        private float _fadeTimer = 0.0f;
        private float _fadeBackground = 1.5f;

        private bool OnLoadFinished(ref LevelManager.LevelInfo data)
        {
            _finished = true;

            if (data.Definition.Background)
            {
                _fadeTimer = _fadeBackground;
            }
            else
            {
                _fadeTimer = 0.0f;
            }

            return true;
        }

        public override void Open()
        {

        }

        public override void Close()
        {

        }

        public override void Process(float delta)
        {
            if (!_finished)
            {
                return;
            }

            _fadeTimer -= delta;

            _texture.Modulate = new(1.0f, 1.0f, 1.0f, Mathf.Clamp(_fadeTimer, 0.0f, 1.0f));

            if (_fadeTimer <= 0.0f)
            {
                _fadeTimer = 0.0f;
                _finished = false;
            }
        }
    }
}
