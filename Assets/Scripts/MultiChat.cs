using System;
using System.Collections.Generic;

using Core.Util;

using Newtonsoft.Json;

using TMPro;

using UI;

using UnityEngine;
using UnityEngine.UI;

namespace MultiChat
{
    public class MultiChatManager : UIManagerBase
    {
        internal static bool DebugSocket;

        #region DRAWER
        protected override void RedrawTheme(Theme theme)
        {
            base.RedrawTheme(theme);

            for (int m = 0; m < _Chat.Messages.Count; m++)
            {
                var message = _Chat.Messages[m];
                var drawable = message.GetComponent<Drawable>();
                if (TryGetData(drawable.GetKey(), out var sprite))
                    drawable.SetValue(sprite);
            }

            for (int m = 0; m < _Chat.Pool.Count; m++)
            {
                var message = _Chat.Pool[m];
                var drawable = message.GetComponent<Drawable>();
                if (TryGetData(drawable.GetKey(), out var sprite))
                    drawable.SetValue(sprite);
            }
        }
        #endregion

        [Space]
        [SerializeField] bool DebugSocketMessages;
        [SerializeField] float RefreshPeriod = 2f;

        float T;

        [Space]
        [SerializeField] Authentication _Authentication;
        #region AUTHENTICATION
        [Serializable]
        class Authentication : WindowBase
        {
            public MenuButton SubmitButton;
            public TMP_InputField TokenField;
        }

        void SubmitToken(byte platform = 0)
        {
            var pref = VK.TokenPref;
            switch (platform)
            {
                case 1:
                pref = Twitch.TokenPref;
                break;
            }

            SubmitToken();

            bool SubmitToken()
            {
                if (_Authentication.TokenField.text.Contains("access_token="))
                {
                    var uri = new Uri(_Authentication.TokenField.text);
                    var token = System.Web.HttpUtility.ParseQueryString(uri.Fragment.TrimStart('#'))["access_token"];

                    if (!string.IsNullOrEmpty(token))
                    {
                        PlayerPrefs.SetString(pref, token);

                        return true;
                    }
                }

                return false;
            }
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

            Twitch.StartAuth();
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
                AddMessage(0, 2f);

                return;
            }

            if (string.IsNullOrEmpty(_PlatformCreation.ChannelInput.text))
            {
                AddMessage(1, 2f);

                return;
            }

            switch (_PlatformCreation.Switch.GetValue())
            {
                case 0:
                _Platforms.List.Add(new VK(_PlatformCreation.NameInput.text, _PlatformCreation.ChannelInput.text, _Platforms.List.Count + 1, this));
                break;
                case 1:
                _Platforms.List.Add(new Twitch(_PlatformCreation.NameInput.text, _PlatformCreation.ChannelInput.text, _Platforms.List.Count + 1, this));
                break;
            }

            _PlatformCreation.NameInput.text = "";
            _PlatformCreation.ChannelInput.text = "";

            _PlatformCreation.SetEnabled(false);
        }
        #endregion

        [Space]
        [SerializeField] Platforms _Platforms;
        #region PLATFORM LIST
        [Serializable]
        class Platforms : WindowBase
        {
            public ScrollRect Scroll;
            public Transform View;
            public GameObject ContentPrefab;
            public GameObject PlatformPrefab;

            public List<Platform> List = new List<Platform>();
        }

        public void OpenPlatforms()
        {
            _Platforms.Scroll.content = Instantiate(_Platforms.ContentPrefab, _Platforms.View).transform as RectTransform;

            for (int l = 0; l < _Platforms.List.Count; l++)
            {
                var go = Instantiate(_Platforms.PlatformPrefab, _Platforms.Scroll.content);
                var lp = go.GetComponent<ListPlatform>();
                lp.Init(_Platforms.List[l], this);
            }

            _Platforms.SetEnabled(true);
        }
        public void ClosePlatforms()
        {
            Destroy(_Platforms.Scroll.content.gameObject);

            _Platforms.SetEnabled(false);
        }

        void LoadPlatforms()
        {
            var index = 1;
            while (PlayerPrefs.HasKey("platform_" + index))
            {
                var data = JsonConvert.DeserializeObject<PlatformData>(PlayerPrefs.GetString("platform_" + index));
                switch (data.PlatformType)
                {
                    case "vk":
                    _Platforms.List.Add(new VK(data, index, this));
                    break;
                    case "twitch":
                    _Platforms.List.Add(new Twitch(data, index, this));
                    break;
                }

                index++;
            }
        }
        #endregion

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

        void RemoveMessage(ChatMessage message)
        {
            Smiles.RemoveRange(message.GetSmiles(), message.gameObject);
            Badges.RemoveRange(message.GetBadges(), message.gameObject);

            ToPool(message);

            void ToPool(ChatMessage message)
            {
                message.transform.SetParent(null);
                message.gameObject.SetActive(false);

                _Chat.Pool.Add(message);
            }
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
        #endregion

        #endregion

        [Space]
        [SerializeField] StreamingSprites Smiles;

        [Space]
        [SerializeField] StreamingSprites Badges;

        protected override void Awake()
        {
            base.Awake();

            SetLanguage("en");

            Smiles.Prepare();
            Badges.Prepare();

            LoadPlatforms();
            //OpenPlatforms();

            OpenThemes();
        }
        protected override void Update()
        {
            base.Update();

            T += Time.deltaTime;
            if (T >= RefreshPeriod)
            {
                T = 0f;

                for (int p = 0; p < _Platforms.List.Count; p++)
                    if (_Platforms.List[p].Enabled)
                        _Platforms.List[p].Refresh();
            }

            UpdateChat();

            void UpdateChat()
            {
                var process = true;
                while (process)
                {
                    process = false;
                    for (int p = 0; p < _Platforms.List.Count; p++)
                    {
                        if (!_Platforms.List[p].Enabled)
                            continue;

                        var got = _Platforms.List[p].GetMessage(out var message);
                        process |= got;

                        if (got)
                        {
                            var m = FromPool();
                            m.Init(message, this);

                            _Chat.Messages.Add(m);
                        }
                    }
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
            }
        }
        protected override void OnDestroy()
        {
            base.OnDestroy();

            for (int p = 0; p < _Platforms.List.Count; p++)
                _Platforms.List[p].Disconnect();
        }
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            DebugSocket = DebugSocketMessages;
        }
#endif
    }
}