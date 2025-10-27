using System.Collections.Generic;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

namespace MultiChat
{
    [System.Serializable]
    internal abstract class Platform
    {
        protected static int MessagesLimit = 100;

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

        internal void RefreshChat(MonoBehaviour mono) => mono.StartCoroutine(GetChatMessages());
        internal void SaveData()
        {
            PlayerPrefs.SetString("platform_" + Index, JsonConvert.SerializeObject(Data));
            PlayerPrefs.Save();
        }

        internal bool GetMessage(out MC_Message message) => MC_Messages.TryDequeue(out message);

        protected abstract IEnumerator<UnityWebRequestAsyncOperation> GetChatMessages();
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
            public int ID;
            public string URL;
        }
        public struct Text
        {
            public string Content;
        }
    }
}