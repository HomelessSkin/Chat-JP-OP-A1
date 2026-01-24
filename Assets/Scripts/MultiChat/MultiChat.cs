using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using TMPro;

using UI;

using UnityEngine;

namespace MultiChat
{
    public class MultiChatManager : UIManagerBase
    {
        internal static bool DebugSocket;

        #region DRAWER
        protected override void RedrawTheme(IStorage.Data storage)
        {
            base.RedrawTheme(storage);

            for (int m = 0; m < _Chat.Messages.Count; m++)
            {
                var message = _Chat.Messages[m];
                var drawables = message.GetComponentsInChildren<Drawable>();
                for (int d = 0; d < drawables.Length; d++)
                {
                    var drawable = drawables[d];
                    if (!(drawable as IRedrawable).IsRedrawable())
                        continue;
                    if (TryGetDrawerData(drawable.GetKey(), out var sprite))
                        drawable.SetData(sprite);
                }
            }

            for (int m = 0; m < _Chat.Pool.Count; m++)
            {
                var message = _Chat.Pool[m];
                var drawables = message.GetComponentsInChildren<Drawable>();
                for (int d = 0; d < drawables.Length; d++)
                {
                    var drawable = drawables[d];
                    if (!(drawable as IRedrawable).IsRedrawable())
                        continue;
                    if (TryGetDrawerData(drawable.GetKey(), out var sprite))
                        drawable.SetData(sprite);
                }
            }
        }
        #endregion

        [Space]
        [SerializeField] bool DebugSocketMessages;
        [SerializeField] float RefreshPeriod = 2f;
        [Space]
        [SerializeField] string[] VKScopes;
        [SerializeField] string[] TwitchScopes;

        float T;

        [Space]
        [SerializeField] protected Authentication _Authentication;
        #region AUTHENTICATION
        [Serializable]
        protected class Authentication : DataStorage
        {
            public MenuButton SubmitButton;
            public TMP_InputField TokenField;

            public override void AddData(string serialized, string path, bool fromResources = false, UIManagerBase manager = null)
            {
                AllData.Add(JsonUtility.FromJson<IStorage.Data>(serialized));
            }

            public string GetToken(string platform)
            {
                for (int d = 0; d < AllData.Count; d++)
                    if (AllData[d].Type == platform)
                        return AllData[d].Name;

                return null;
            }
        }

        void SubmitToken(byte platform = 0)
        {
            if (_Authentication.TokenField.text.Contains("access_token="))
            {
                var uri = new Uri(_Authentication.TokenField.text);
                var token = System.Web.HttpUtility.ParseQueryString(uri.Fragment.TrimStart('#'))["access_token"];

                if (!string.IsNullOrEmpty(token))
                {
                    var data = new IStorage.Data { Type = platform == 0 ? "vk" : "twitch", Name = token };

                    _Authentication.AddData(data);
                    _Authentication.Save(data);

                    Log.Warning(this.GetType().ToString(), "Token accepted!");
                }
            }

            Log.Warning(this.GetType().ToString(), "Token rejected!");
        }

        #region VK
        public void StartAuthVK()
        {
            _Authentication.SubmitButton.RemoveAllListeners();
            _Authentication.SubmitButton.AddListener(SubmitVKToken);

            VK.StartAuth();
        }
        public void SubmitVKToken() => SubmitToken();
        #endregion

        #region TWITCH
        public void StartAuthTwitch()
        {
            _Authentication.SubmitButton.RemoveAllListeners();
            _Authentication.SubmitButton.AddListener(SubmitTwitchToken);

            Twitch.StartAuth(TwitchScopes);
        }
        public void SubmitTwitchToken() => SubmitToken(1);
        #endregion

        #endregion

        [Space]
        [SerializeField] PlatformCreation _PlatformCreation;
        #region PLATFORM CREATION
        [Serializable]
        class PlatformCreation : WindowBase
        {
            public DropDown Switch;
            public TMP_InputField NameInput;
            public TMP_InputField ChannelInput;
        }

        public void CreatePlatform()
        {
            if (string.IsNullOrEmpty(_PlatformCreation.NameInput.text))
            {
                Log.Warning(this.GetType().FullName, $"Name Input is empty!");

                return;
            }

            if (string.IsNullOrEmpty(_PlatformCreation.ChannelInput.text))
            {
                Log.Warning(this.GetType().FullName, $"Channel Input is empty!");

                return;
            }

            Platform platform = null;
            switch (_PlatformCreation.Switch.GetValue())
            {
                case 0:
                platform = CreateVK(_PlatformCreation.NameInput.text, _PlatformCreation.ChannelInput.text);
                break;
                case 1:
                platform = CreateTwitch(_PlatformCreation.NameInput.text, _PlatformCreation.ChannelInput.text);
                break;
            }

            if (platform != null)
            {
                _Platforms.List.Add(platform);

                _Platforms.Close();
                _Platforms.AddData(platform.Data);
                _Platforms.Save(platform.Data);
                _Platforms.Open<ListPlatform>(this);

                _PlatformCreation.NameInput.text = "";
                _PlatformCreation.ChannelInput.text = "";

                _PlatformCreation.SetEnabled(false);
            }
        }
        #endregion

        [Space]
        [SerializeField] protected Platforms _Platforms;
        #region PLATFORM LIST
        [Serializable]
        protected class Platforms : ScrollBase
        {
            internal List<Platform> List = new List<Platform>();

            public override void AddData(string serialized, string path, bool fromResources = false, UIManagerBase manager = null)
            {
                var data = JsonUtility.FromJson<Platform.PlatformData>(serialized);
                Platform platform = null;
                switch (data.Type)
                {
                    case "vk":
                    platform = (manager as MultiChatManager).CreateVK(data);
                    break;
                    case "twitch":
                    platform = (manager as MultiChatManager).CreateTwitch(data);
                    break;
                }

                AllData.Add(data);
            }
        }

        public void OpenPlatforms() => _Platforms.Open<ListPlatform>(this);
        public void ClosePlatforms()
        {
            if (_PlatformCreation.IsEnabled())
                return;

            _Platforms.Close();
        }

        internal virtual Platform CreateVK(string name, string channel) => new VK(name, channel, _Authentication.GetToken("vk"));
        internal virtual Platform CreateVK(Platform.PlatformData data) => new VK(data, _Authentication.GetToken("vk"));
        internal virtual Platform CreateTwitch(string name, string channel) => new Twitch(name, channel, _Authentication.GetToken("twitch"));
        internal virtual Platform CreateTwitch(Platform.PlatformData data) => new Twitch(data, _Authentication.GetToken("twitch"));

        protected bool AllWorking()
        {
            for (int p = 0; p < _Platforms.List.Count; p++)
                if (!_Platforms.List[p].IsWorking)
                    return false;

            return true;
        }
        #endregion

        void LoadPlatforms()
        {
            _Authentication.CollectAllData();
            _Platforms.CollectAllData();

            for (int d = 0; d < _Platforms.AllData.Count; d++)
            {
                var data = _Platforms.AllData[d] as Platform.PlatformData;
                switch (data.Type)
                {
                    case "vk":
                    _Platforms.List.Add(CreateVK(data));
                    break;
                    case "twitch":
                    _Platforms.List.Add(CreateTwitch(data));
                    break;
                }
            }
        }

        [Space]
        [SerializeField] Chat _Chat;
        #region CHAT
        [Serializable]
        class Chat : WindowBase
        {
            public int MaxChatCount = 100;
            public Transform Content;
            public GameObject MessagePrefab;

            [HideInInspector]
            public List<ChatMessage> Messages = new List<ChatMessage>();
            [HideInInspector]
            public List<ChatMessage> Pool = new List<ChatMessage>();
        }

        public void ClearChat()
        {
            for (int m = 0; m < _Chat.Messages.Count; m++)
                RemoveMessage(_Chat.Messages[m]);

            _Chat.Messages.Clear();
        }
        internal void DeleteMessage(byte platform, string id)
        {
            var index = -1;
            for (int m = 0; m < _Chat.Messages.Count; m++)
            {
                var message = _Chat.Messages[m];
                if (message.GetPlatform() == platform &&
                     message.GetID() == id)
                {
                    index = m;

                    RemoveMessage(message);

                    break;
                }
            }

            if (index >= 0)
                _Chat.Messages.RemoveAt(index);
        }

        protected virtual void OnMessage(MC_Message message)
        {
            var m = FromPool();
            m.Init(message, this);

            _Chat.Messages.Add(m);
        }

        void RemoveMessage(ChatMessage message)
        {
            Smiles.RemoveRange(message.GetSmiles(), message.gameObject);
            Badges.RemoveRange(message.GetBadges(), message.gameObject);

            ToPool(message);
        }
        void ToPool(ChatMessage message)
        {
            message.transform.SetParent(null);
            message.gameObject.SetActive(false);

            _Chat.Pool.Add(message);
        }
        ChatMessage FromPool()
        {
            ChatMessage message;
            if (_Chat.Messages.Count >= _Chat.MaxChatCount)
            {
                message = _Chat.Messages[0];

                message.transform.SetParent(null);
                message.transform.SetParent(_Chat.Content);

                _Chat.Messages.RemoveAt(0);
            }
            else if (_Chat.Pool.Count > 0)
            {
                message = _Chat.Pool[0];
                message.transform.SetParent(_Chat.Content);
                message.gameObject.SetActive(true);

                _Chat.Pool.RemoveAt(0);
            }
            else
                message = Instantiate(_Chat.MessagePrefab, _Chat.Content, false).GetComponent<ChatMessage>();

            return message;
        }

        #region SMILES
        internal bool HasSmile(int hash, bool reserveIfFalse = false)
        {
            if (Smiles.HasSprite(hash))
                return true;

            if (Smiles.IsKeyReserved(hash))
                return true;

            if (reserveIfFalse)
                Smiles.ReserveKey(hash);

            return false;
        }
        internal int GetSmileID(int key, GameObject requester) => Smiles.GetSpriteID(key, requester);
        internal void DrawSmile(Texture2D smile, int hash) => Smiles.Draw(smile, hash);
        #endregion

        #region BADGES
        internal bool HasBadge(int hash, bool reserveIfFalse = false)
        {
            if (Badges.HasSprite(hash))
                return true;

            if (Badges.IsKeyReserved(hash))
                return true;

            if (reserveIfFalse)
                Badges.ReserveKey(hash);

            return false;
        }
        internal bool HasBadge(int hash) => Badges.HasSprite(hash);
        internal int GetBadgeID(int key, GameObject requester) => Badges.GetSpriteID(key, requester);
        internal void DrawBadge(Texture2D badge, int hash) => Badges.Draw(badge, hash);

        Task ChatUpd;
        #endregion

        #endregion

        [Space]
        [SerializeField] StreamingSprites Smiles;

        [Space]
        [SerializeField] StreamingSprites Badges;

        protected override void Awake()
        {
            base.Awake();

            Smiles.Prepare();
            Badges.Prepare();

            LoadPlatforms();
        }
        protected override void Update()
        {
            base.Update();

            var list = new List<Task>();
            {
                T += Time.deltaTime;
                if (T >= RefreshPeriod)
                {
                    T = 0f;

                    for (int p = 0; p < _Platforms.List.Count; p++)
                    {
                        var platform = _Platforms.List[p];
                        if (platform.Enabled)
                            list.Add(platform.Refresh());
                    }
                }

                ChatUpd = Task.WhenAll(list);
            }

            if (ChatUpd.IsCompleted)
                UpdateChat();

            void UpdateChat()
            {
                var process = true;
                while (process)
                {
                    process = false;
                    for (int p = 0; p < _Platforms.List.Count; p++)
                    {
                        var platform = _Platforms.List[p];
                        if (!platform.Enabled)
                            continue;

                        var got = platform.GetMessage(out var message);
                        process |= got;

                        if (got)
                            OnMessage(message);
                    }
                }

            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            for (int p = 0; p < _Platforms.List.Count; p++)
            {
                var platform = _Platforms.List[p];
                platform.Disconnect();
            }
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();

            _Authentication.Manager = this;
            _Platforms.Manager = this;

            DebugSocket = DebugSocketMessages;
        }
#endif
    }
}