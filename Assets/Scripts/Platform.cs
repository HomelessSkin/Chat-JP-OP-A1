using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

namespace MultiChat
{
    [System.Serializable]
    public abstract class Platform
    {
        protected static int MessagesLimit = 100;

        protected string Channel;
        protected Queue<MC_Message> Messages = new Queue<MC_Message>();

        public void RefreshChat(MonoBehaviour mono)
        {
            mono.StartCoroutine(GetChatMessages());
        }
        public bool GetMessage(out MC_Message message) => Messages.TryDequeue(out message);

        protected abstract IEnumerator<UnityWebRequestAsyncOperation> GetChatMessages();
    }

    public struct MC_Message
    {
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