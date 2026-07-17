using Godot;
using Steamworks;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class WebSurfaceManager : Node, IModSystem
    {
        /*
        private BiDictionary<HHTMLBrowser, Texture2D> _browsers = [];

        private CallResult<HTML_BrowserReady_t> _browserReady;
        private CallResult<HTML_StartRequest_t> _startRequest;
        private Callback<HTML_NeedsPaint_t> _repaint;

        public struct WebSurfaceUpdate
        {
            public HHTMLBrowser BrowserHandle;
            public Texture2D Texture;
        }

        public EventFunc<WebSurfaceUpdate> OnRedraw { get; } = new();

        bool ISystem.Initialize()
        {
            if (!Steam.Initialized)
            {
                return true;
            }

            if (SteamHTMLSurface.Init())
            {
                _browserReady = CallResult<HTML_BrowserReady_t>.Create(OnBrowserReady);
                _browserReady.Set(SteamHTMLSurface.CreateBrowser(null, null));
                _repaint = Callback<HTML_NeedsPaint_t>.Create(Redraw);
            }

            return true;
        }

        public Texture2D DrawUrl(string Url)
        {

        }

        private void OnBrowserReady(HTML_BrowserReady_t pCallback, bool bIOFailure)
        {
            if (bIOFailure)
            {
                return;
            }
            _browser = pCallback.unBrowserHandle;
            SteamHTMLSurface.LoadURL(_browser, "https://steampowered.com", "");
        }

        private void OnBrowserStartRequest(HTML_StartRequest_t pCallback, bool bIOFailure)
        {

        }

        private void Redraw(HTML_NeedsPaint_t data)
        {

        }

        void CreateTest(Texture2D texture)
        {

        }

        void ISystem.Deinitialize()
        {
            if (_browser.m_HHTMLBrowser != 0)
            {
                SteamHTMLSurface.RemoveBrowser(_browser);
            }
            SteamHTMLSurface.Shutdown();
        }
        */
    }
}
