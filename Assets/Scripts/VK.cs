using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

namespace MultiChat
{
    internal class VK : Platform
    {
        internal static string TokenPref = "vk_token";

        static string AppID = "ksxptucqm12f6cp5";
        static string AuthPath = "https://auth.live.vkvideo.ru/app/oauth2/authorize";
        static string RedirectPath = "https://oauth.vk.com/blank.html";
        static string EntryPath = "https://apidev.live.vkvideo.ru";

        internal VK(string name, string channel, int index, MultiChatManager manager) : base(name, channel, index, manager)
        {
            Data.PlatformType = "vk";

            SaveData();
        }
        internal VK(PlatformData data, int index, MultiChatManager manager) : base(data, index, manager)
        {


            SaveData();
        }

        protected override async void GetChatMessages()
        {
            var token = PlayerPrefs.GetString(TokenPref);
            if (!string.IsNullOrEmpty(token))
                using (var request = UnityWebRequest
                    .Get($"{EntryPath}/v1/chat/messages?channel_url={Data.ChannelName}&limit={MessagesLimit}"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {token}");

                    var oper = request.SendWebRequest();
                    while (!oper.isDone)
                        await Task.Yield();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var root = JsonConvert.DeserializeObject<Root>(request.downloadHandler.text);

                        var startCollecting = LastMessage == 0;
                        for (int m = root.data.chat_messages.Count - 1; m >= 0; m--)
                        {
                            var message = root.data.chat_messages[m];
                            if (startCollecting)
                            {
                                if (message.author.nick == "ChatBot")
                                    continue;

                                if (GetParts(message, out var parts))
                                    Enqueue(new MC_Message
                                    {
                                        Nick = message.author.nick,
                                        Parts = parts,
                                    });
                            }
                            else if (message.id == LastMessage)
                                startCollecting = true;
                        }

                        LastMessage = root.data.chat_messages[0].id;
                    }
                    else
                        Debug.LogError(request.error);
                }

            bool GetParts(Root.Messages.ChatMessage message, out List<MC_Message.MessagePart> parts)
            {
                parts = new List<MC_Message.MessagePart>();
                var tasks = new List<Task>();
                for (int p = 0; p < message.parts.Count; p++)
                {
                    var part = message.parts[p];
                    var mc = new MC_Message.MessagePart { };

                    if (part.link != null && !string.IsNullOrEmpty(part.link.content))
                    {
                        parts = null;

                        return false;
                    }

                    if (part.mention != null)
                        mc.Mention = new MC_Message.Mention { Nick = part.mention.nick };
                    if (part.smile != null && !string.IsNullOrEmpty(part.smile.medium_url))
                        mc.Smile = new MC_Message.Smile { Hash = part.smile.id.GetHashCode(), URL = part.smile.medium_url };
                    if (part.text != null)
                        mc.Text = new MC_Message.Text { Content = part.text.content };

                    parts.Add(mc);
                }

                return true;
            }
        }

        internal static void StartAuth() =>
            Application.OpenURL($"{AuthPath}?client_id={AppID}&redirect_uri={RedirectPath}&response_type=token");

        #region MESSAGE
        [Serializable]
        class Root
        {
            public Messages data;

            [Serializable]
            public class Messages
            {
                public List<ChatMessage> chat_messages;

                [Serializable]
                public class ChatMessage
                {
                    public long id;
                    public Author author;
                    public List<MessagePart> parts;
                }
            }

            [Serializable]
            public class Author
            {
                public string nick;
                public long nick_color;
                public List<Badge> badges;
                public List<Role> roles;

                [Serializable]
                public class Badge
                {
                    public string medium_url;
                }

                [Serializable]
                public class Role
                {
                    public string medium_url;
                }
            }

            [Serializable]
            public class MessagePart
            {
                public Link link;
                public Mention mention;
                public Smile smile;
                public Text text;

                [Serializable]
                public class Link
                {
                    public string content;
                }

                [Serializable]
                public class Mention
                {
                    public string nick;
                }

                [Serializable]
                public class Smile
                {
                    public bool animated;
                    public string id;
                    public string medium_url;
                }

                [Serializable]
                public class Text
                {
                    public string content;
                }
            }
        }
        #endregion
    }
}