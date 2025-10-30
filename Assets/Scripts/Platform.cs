using System.Collections.Generic;
using System.Threading.Tasks;

using Core.Util;

using Newtonsoft.Json;

using UnityEngine;

namespace MultiChat
{
    [System.Serializable]
    internal abstract class Platform
    {
        protected static int MessagesLimit = 100;

        protected MultiChatManager Manager;
        protected PlatformData Data;
        internal bool Enabled
        {
            get => Data.Enabled;
            set
            {
                Data.Enabled = value;

                SaveData();
            }
        }

        protected int Index;
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

        protected long LastMessage;
        protected Queue<MC_Message> MC_Messages = new Queue<MC_Message>();

        internal Platform(string name, string channel, int index, MultiChatManager manager)
        {
            Manager = manager;
            Index = index;

            Data = new PlatformData
            {
                Enabled = true,

                Name = name,
                Channel = channel,
            };
        }
        internal Platform(PlatformData data, int index, MultiChatManager manager)
        {
            Manager = manager;
            Index = index;
            Data = data;
        }

        internal void RefreshChat() => GetChatMessages();
        internal void SaveData()
        {
            PlayerPrefs.SetString("platform_" + Index, JsonConvert.SerializeObject(Data));
            PlayerPrefs.Save();
        }
        internal bool GetMessage(out MC_Message message) => MC_Messages.TryDequeue(out message);

        protected abstract void GetChatMessages();
        protected async Task Enqueue(MC_Message message)
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
    }

    [System.Serializable]
    internal struct PlatformData
    {
        public bool Enabled;

        public string Name;
        public string Channel;
        public string PlatformType;
    }

    internal struct MC_Message
    {
        public string Nick;
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
}