using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Text;

namespace Unary.Core
{
    [Tool]
    [GlobalClass]
    public partial class Steam : Node, ICoreSystem
    {
        private static EditorSettingVariable<bool> _enabled = new()
        {
            EditorDefault = false,
            RuntimeDefault = true
        };

        public static readonly AppId_t AppId = new(1436420);

        // Intentionnaly kept static and not cleaned up when runtime stops.
        // This way we are trying to lower times SteamWorks would unnecessarily reinitialize itself.
        public static bool Initialized { get; private set; } = false;

        private SteamAPIWarningMessageHook_t _messageHook = null;

        public struct PersonaStateChangeData
        {
            public CSteamID SteamId;
            public int PlayerId;
            public string OnlineName;
            public Texture2D Avatar;
        }

        private Callback<PersonaStateChange_t> _personaStateChangeCallback;

        public EventFunc<PersonaStateChangeData> OnIdentityUpdate { get; } = new();

        private static void MessageHook(int severity, StringBuilder stringBuilder)
        {
            stringBuilder.Prepend("[Steamworks] ");

            if (severity == 0)
            {
                RuntimeLogger.Log(Singleton, stringBuilder.ToString());
            }
            else
            {
                RuntimeLogger.Warning(Singleton, stringBuilder.ToString());
            }
        }

        public static bool InitializeSteamWorks()
        {
            if (Initialized)
            {
                return true;
            }

            if (!Packsize.Test())
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_Pack_Size);
                return false;
            }

            if (!DllCheck.Test())
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_Dll_Check);
                return false;
            }

            try
            {
                if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_Dll_Not_Found, e.Message, e.StackTrace);
                return false;
            }

            string appId = AppId.ToString();

            try
            {
                System.Environment.SetEnvironmentVariable("SteamAppId", appId);
                System.Environment.SetEnvironmentVariable("SteamOverlayGameId", appId);
                System.Environment.SetEnvironmentVariable("SteamGameId", appId);
            }
            catch (Exception e)
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_EnvironmentVariable_Failed, e.Message, e.StackTrace);
                return false;
            }

            try
            {
                Initialized = SteamAPI.Init();
            }
            catch (Exception e)
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_Init_Failed, e.Message, e.StackTrace);
                return false;
            }

            if (Initialized)
            {
                Initialized = SteamUser.BLoggedOn();
            }

            return Initialized;
        }

        bool ISystem.Initialize()
        {

#if TOOLS
            if (_enabled.Value)
            {
                this.Log("Starting Steam in online mode");
            }
            else
            {
                this.Log("Starting Steam in offline mode");
                Initialized = false;
                return true;
            }
#endif

            if (!InitializeSteamWorks())
            {
                InitializationError.Show(InitializationError.ErrorType.Steamworks_Not_Launched);
                return false;
            }

            if (_messageHook == null)
            {
                _messageHook = new SteamAPIWarningMessageHook_t(MessageHook);
                SteamClient.SetWarningMessageHook(_messageHook);
            }

            _personaStateChangeCallback = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);

            SteamNetworkingUtils.InitRelayNetworkAccess();

            return true;
        }

        private static void FlipImageVertically(byte[] image, uint width, uint height)
        {
            uint bytesPerPixel = 4;
            uint rowSize = width * bytesPerPixel;
            byte[] tempRow = new byte[rowSize];

            for (uint y = 0; y < height / 2; y++)
            {
                uint sourceRowStart = y * rowSize;
                uint destRowStart = (height - 1 - y) * rowSize;

                Array.Copy(image, sourceRowStart, tempRow, 0, rowSize);
                Array.Copy(image, destRowStart, image, sourceRowStart, rowSize);
                Array.Copy(tempRow, 0, image, destRowStart, rowSize);
            }
        }

        private void ProcessChange(EPersonaChange change, CSteamID id)
        {
            bool changed = false;

            string personaName = null;

            if (change.HasFlag(EPersonaChange.k_EPersonaChangeName) ||
            change.HasFlag(EPersonaChange.k_EPersonaChangeNameFirstSet) ||
            change.HasFlag(EPersonaChange.k_EPersonaChangeNickname))
            {
                personaName = SteamFriends.GetFriendPersonaName(id);
                changed = true;
            }

            Texture2D profilePicture = null;

            if (change.HasFlag(EPersonaChange.k_EPersonaChangeAvatar))
            {
                // TODO Revamp this code to use large avatar version, since it requires additional callback handling
                int avatarId = SteamFriends.GetMediumFriendAvatar(id);

                if (SteamUtils.GetImageSize(avatarId, out uint imageWidth, out uint imageHeight))
                {
                    byte[] image = new byte[imageWidth * imageHeight * 4];
                    if (SteamUtils.GetImageRGBA(avatarId, image, (int)(imageWidth * imageHeight * 4)))
                    {
                        FlipImageVertically(image, imageWidth, imageHeight);

                        // TODO Check if this even works
                        profilePicture = new();
                        Image targetImage = profilePicture.GetImage();
                        targetImage.SetData((int)imageWidth, (int)imageHeight, false, Image.Format.Rgba8, image);

                        //profilePicture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, false);
                        //profilePicture.LoadRawTextureData(image);
                        //profilePicture.Apply();
                    }
                }

                if (profilePicture != null)
                {
                    changed = true;
                }
            }

            if (changed)
            {
                PersonaStateChangeData data = new()
                {
                    SteamId = id,
                    Avatar = profilePicture,
                    OnlineName = personaName
                };

                OnIdentityUpdate.Publish(data);
            }
        }

        private void OnPersonaStateChange(PersonaStateChange_t data)
        {
            EPersonaChange change = data.m_nChangeFlags;
            ProcessChange(change, new(data.m_ulSteamID));
        }

        public void RequestUserInformation(CSteamID id)
        {
            if (!Initialized)
            {
                return;
            }

            if (!SteamFriends.RequestUserInformation(id, false))
            {
                EPersonaChange change = EPersonaChange.k_EPersonaChangeNickname | EPersonaChange.k_EPersonaChangeAvatar;
                ProcessChange(change, id);
            }
        }

        bool ISystem.PostInitialize()
        {
            return true;
        }

        void ISystem.Deinitialize()
        {
            if (_messageHook != null)
            {
                SteamClient.SetWarningMessageHook(null);
            }

            if (Initialized)
            {
                SteamAPI.Shutdown();
                Initialized = false;
            }
        }

        void ISystem.Process(float delta)
        {
            if (Initialized)
            {
                SteamAPI.RunCallbacks();
            }
        }

        private CSteamID _steamId;
        public CSteamID SteamId
        {
            get
            {
                if (Initialized && _steamId == default)
                {
                    _steamId = SteamUser.GetSteamID();
                }
                return _steamId;
            }
        }

        private string _personaName;
        public string PersonaName
        {
            get
            {
                if (Initialized && _personaName == default)
                {
                    _personaName = SteamFriends.GetPersonaName();
                }
                return _personaName;
            }
        }

        public Dictionary<string, PublishedFileId_t> GetModsInfo()
        {
            Dictionary<string, PublishedFileId_t> result = new();

            if (!Initialized)
            {
                return null;
            }

            uint subscribed = SteamUGC.GetNumSubscribedItems();

            if (subscribed == 0)
            {
                return null;
            }

            PublishedFileId_t[] items = new PublishedFileId_t[subscribed];

            uint itemsCount = SteamUGC.GetSubscribedItems(items, subscribed);

            if (itemsCount == 0)
            {
                return null;
            }

            for (uint i = 0; i < itemsCount; i++)
            {
                if (SteamUGC.GetItemInstallInfo(items[i], out ulong _, out string folder, 1024, out uint _))
                {
                    result[folder] = items[i];
                }
            }

            return result;
        }
    }
}
