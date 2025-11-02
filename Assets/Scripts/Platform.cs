using System;
using System.Collections.Generic;

using Core.Util;

using Newtonsoft.Json;

using UnityEngine;

using WebSocketSharp;

namespace MultiChat
{
    [System.Serializable]
    internal abstract class Platform
    {
        protected static int MessagesLimit = 100;
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

        protected int Index;
        protected long LastMessage;
        protected PlatformData Data;

        protected WebSocket Socket;
        protected MultiChatManager Manager;

        protected Queue<MC_Message> MC_Messages = new Queue<MC_Message>();

        internal Platform(string name, string channel, int index, MultiChatManager manager)
        {
            Manager = manager;
            Index = index;

            Data = new PlatformData
            {
                Enabled = true,

                PlatformName = name,
                ChannelName = channel,
            };
        }
        internal Platform(PlatformData data, int index, MultiChatManager manager)
        {
            Manager = manager;
            Index = index;
            Data = data;
        }

        protected abstract void Connect();
        protected abstract void OnOpen(object sender, EventArgs e);
        protected abstract void OnMessage(object sender, MessageEventArgs e);
        protected abstract void SubscribeToEvent(string type);
        protected abstract void ProcessSocketMessages();

        internal void Refresh() => ProcessSocketMessages();
        internal void Disconnect()
        {
            if (Socket.IsAlive)
                Socket.Close();
        }
        internal bool GetMessage(out MC_Message message) => MC_Messages.TryDequeue(out message);

        protected async void Enqueue(MC_Message message)
        {
            for (int p = 0; p < message.Parts.Count; p++)
            {
                var part = message.Parts[p];

                if (!string.IsNullOrEmpty(part.Smile.URL))
                {
                    var smile = await Web.DownloadSpriteTexture(part.Smile.URL);
                    if (smile)
                    {
                        if (!Manager.HasSmile(part.Smile.Hash, out var id))
                            Manager.DrawSmile(smile, part.Smile.Hash);
                    }
                    else
                        part.Smile.URL = null;
                }

                message.Parts[p] = part;
            }

            MC_Messages.Enqueue(message);
        }
        protected void SaveData()
        {
            PlayerPrefs.SetString("platform_" + Index, JsonConvert.SerializeObject(Data));
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
        protected void OnClose(object sender, CloseEventArgs e) => Debug.Log(e.Reason);
        protected void OnError(object sender, ErrorEventArgs e) => Debug.Log(e.Message);
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
        public string Nick;
        public string Color;
        public List<MessagePart> Parts;

        public struct MessagePart
        {
            public Mention Mention;
            public Smile Smile;
            public Text Text;
        }

        public struct Mention
        {
            public string Nick;
        }

        public struct Smile
        {
            public int Hash;
            public string URL;
        }

        public struct Text
        {
            public string Content;
        }
    }
    #endregion
}