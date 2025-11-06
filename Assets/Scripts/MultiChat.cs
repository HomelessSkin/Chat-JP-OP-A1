using System;
using System.Collections.Generic;

using Core.Util;

using Newtonsoft.Json;

using TMPro;

using UI;

using UnityEngine;

namespace MultiChat
{
    internal class MultiChatManager : UIManagerBase
    {
        internal static bool DebugSocket;

        [SerializeField] bool DebugSocketMessages;
        [SerializeField] float RefreshPeriod = 2f;

        float T;

        [Space]
        [SerializeField] Authentication _Authentication;
        #region AUTHENTICATION
        [Serializable]
        class Authentication
        {
            public Transform Panel;
            public MenuButton SubmitButton;
            public TMP_InputField TokenField;
        }
        #endregion

        [Space]
        [SerializeField] PlatformCreation _PlatformCreation;
        #region PLATFORM CREATION
        [Serializable]
        class PlatformCreation
        {
            public TMP_Dropdown Switch;
            public TMP_InputField NameInput;
            public TMP_InputField URLInput;
        }
        #endregion

        [Space]
        [SerializeField] Chat _Chat;
        #region CHAT
        [Serializable]
        class Chat
        {
            public int MaxChatCount = 100;
            public Transform Content;
            public GameObject MessagePrefab;

            [HideInInspector]
            public List<Platform> Platforms = new List<Platform>();
            [HideInInspector]
            public List<Message> Messages = new List<Message>();
            [HideInInspector]
            public List<Message> Pool = new List<Message>();
        }
        #endregion

        [Space]
        [SerializeField] StreamingSprites Smiles;
        [Space]
        [SerializeField] StreamingSprites Badges;

        protected override void Start()
        {
            base.Start();

            Smiles.Prepare();
            Badges.Prepare();

            LoadPlatforms();

            void LoadPlatforms()
            {
                var index = 0;
                while (PlayerPrefs.HasKey("platform_" + index))
                {
                    var data = JsonConvert.DeserializeObject<PlatformData>(PlayerPrefs.GetString("platform_" + index));
                    switch (data.PlatformType)
                    {
                        case "vk":
                        _Chat.Platforms.Add(new VK(data, index, this));
                        break;
                        case "twitch":
                        _Chat.Platforms.Add(new Twitch(data, index, this));
                        break;
                    }

                    index++;
                }
            }
        }
        void Update()
        {
            T += Time.deltaTime;
            if (T >= RefreshPeriod)
            {
                T = 0f;

                for (int p = 0; p < _Chat.Platforms.Count; p++)
                    if (_Chat.Platforms[p].Enabled)
                        _Chat.Platforms[p].Refresh();
            }

            UpdateChat();

            void UpdateChat()
            {
                var process = true;
                while (process)
                {
                    process = false;
                    for (int p = 0; p < _Chat.Platforms.Count; p++)
                    {
                        if (!_Chat.Platforms[p].Enabled)
                            continue;

                        var got = _Chat.Platforms[p].GetMessage(out var message);
                        process |= got;

                        if (got)
                        {
                            var m = FromPool();
                            m.Init(message, this);

                            _Chat.Messages.Add(m);
                        }
                    }
                }

                Message FromPool()
                {
                    Message message;
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
                        message = Instantiate(_Chat.MessagePrefab, _Chat.Content, false).GetComponent<Message>();

                    return message;
                }
            }
        }
        void OnValidate()
        {
            DebugSocket = DebugSocketMessages;
        }
        void OnDestroy()
        {
            for (int p = 0; p < _Chat.Platforms.Count; p++)
                _Chat.Platforms[p].Disconnect();
        }

        public void CreatePlatform()
        {
            if (!string.IsNullOrEmpty(_PlatformCreation.NameInput.text) &&
                 !string.IsNullOrEmpty(_PlatformCreation.URLInput.text))
                switch (_PlatformCreation.Switch.value)
                {
                    case 0:
                    _Chat.Platforms.Add(new VK(_PlatformCreation.NameInput.text, _PlatformCreation.URLInput.text, _Chat.Platforms.Count, this));
                    break;
                    case 1:
                    _Chat.Platforms.Add(new Twitch(_PlatformCreation.NameInput.text, _PlatformCreation.URLInput.text, _Chat.Platforms.Count, this));
                    break;
                }
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

        void RemoveMessage(Message message)
        {
            Smiles.RemoveRange(message.GetSmiles(), message.gameObject);
            Badges.RemoveRange(message.GetBadges(), message.gameObject);

            ToPool(message);

            void ToPool(Message message)
            {
                message.transform.SetParent(null);
                message.gameObject.SetActive(false);

                _Chat.Pool.Add(message);
            }
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
    }
}