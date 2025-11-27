using System;
using System.Collections.Generic;

using Core.Util.Core.Util;

using UI;

using UnityEngine;

using WebSocketSharp;

namespace MultiChat
{
    [Serializable]
    internal abstract class Platform : Storage.Data
    {
        protected static string RedirectPath = "https://oauth.vk.com/blank.html";

        internal bool Enabled
        {
            get => Data.Enabled;
            set
            {
                Connect();

                Data.Enabled = value;
            }
        }
        internal int CurrentIndex
        {
            get => Index;
            set
            {
                PlayerPrefs.DeleteKey("platform_" + Index);

                Index = value;

                SaveData();
            }
        }
        internal string Type { get => Data.PlatformType; }
        internal string Channel { get => Data.ChannelName; }

        protected int Index;
        protected long LastMessage;
        protected PlatformData Data;

        protected WebSocket Socket;
        protected MultiChatManager Manager;

        protected Queue<MC_Message> MC_Messages = new Queue<MC_Message>();

        internal Platform(string name, string channel, int index, MultiChatManager manager)
        {
            CurrentIndex = index;
            Data = new PlatformData
            {
                Enabled = true,

                PlatformName = name,
                ChannelName = channel,
            };
            Name = name;

            Manager = manager;
        }
        internal Platform(PlatformData data, int index, MultiChatManager manager)
        {
            CurrentIndex = index;
            Data = data;
            Name = data.PlatformName;

            Manager = manager;
        }

        protected abstract void Connect();
        protected abstract void OnOpen(object sender, EventArgs e);
        protected abstract void OnMessage(object sender, MessageEventArgs e);
        protected abstract void SubscribeToEvent(string type);
        protected abstract void ProcessSocketMessages();

        internal void Refresh() => ProcessSocketMessages();
        internal void Disconnect()
        {
            if (Socket != null && Socket.IsAlive)
                Socket.Close();
        }
        internal bool GetMessage(out MC_Message message) => MC_Messages.TryDequeue(out message);

        protected async void Enqueue(MC_Message message)
        {
            if (message.Parts != null)
                for (int p = 0; p < message.Parts.Count; p++)
                {
                    var part = message.Parts[p];

                    if (!string.IsNullOrEmpty(part.Emote.URL))
                    {
                        var smile = await Web.DownloadSpriteTexture(part.Emote.URL);
                        if (smile)
                            Manager.DrawSmile(smile, part.Emote.Hash);
                        else
                            part.Emote.Hash = 0;
                    }

                    message.Parts[p] = part;
                }

            if (message.Badges != null)
                for (int b = 0; b < message.Badges.Count; b++)
                {
                    var part = message.Badges[b];

                    if (!string.IsNullOrEmpty(part.URL))
                    {
                        var badge = await Web.DownloadSpriteTexture(part.URL);
                        if (badge)
                            Manager.DrawBadge(badge, part.Hash);
                    }
                }

            MC_Messages.Enqueue(message);
        }
        protected void SaveData()
        {
            PlayerPrefs.SetString("platform_" + Index, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }
        protected void InitializeSocket(string url)
        {
            if (Socket != null)
                Socket.Close();

            Socket = new WebSocket(url);
            Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            Socket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            Socket.OnOpen += OnOpen;
            Socket.OnMessage += OnMessage;
            Socket.OnClose += OnClose;
            Socket.OnError += OnError;

            Socket.Connect();
        }
        protected void OnClose(object sender, CloseEventArgs e) => Manager.AddMessage($"{e.Reason}\n{e.Code}\n{Type}_Close", UIManagerBase.LogLevel.Error);
        protected void OnError(object sender, ErrorEventArgs e) => Manager.AddMessage($"{e.Message}\n{Type}_Error", UIManagerBase.LogLevel.Error);
    }

    #region DATA
    [Serializable]
    internal struct PlatformData
    {
        public bool Enabled;

        public string PlatformName;
        public string PlatformType;

        public string ChannelID;
        public string ChannelName;

        public string SessionID;
    }
    #endregion

    #region MESSAGE
    internal struct MC_Message
    {
        public byte Platform;
        public string ID;

        public List<Badge> Badges;
        public string NickColor;
        public string Nick;
        public bool IsSlashMe;
        public List<Part> Parts;

        public struct Badge
        {
            public bool IsNeeded;
            public int Hash;
            public string SetID;
            public string ID;
            public string URL;
        }

        public struct Part
        {
            public Mention Reply;
            public Smile Emote;
            public Text Message;

            public struct Mention
            {
                public string Nick;
            }

            public struct Smile
            {
                public bool Draw;
                public int Hash;
                public string URL;
            }

            public struct Text
            {
                public string Content;
            }
        }
    }
    #endregion
}