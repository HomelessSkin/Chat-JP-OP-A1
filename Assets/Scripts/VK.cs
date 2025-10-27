using System;
using System.Collections.Generic;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

namespace MultiChat
{
    internal class VK : Platform
    {
        static string AppID = "ksxptucqm12f6cp5";
        static string AuthPath = "https://auth.live.vkvideo.ru/app/oauth2/authorize";
        static string RedirectPath = "https://oauth.vk.com/blank.html";
        static string EntryPath = "https://apidev.live.vkvideo.ru";

        static string TokenPref = "vk_token";

        internal VK(string name, string channel, int index)
        {
            Index = index;

            Data = new PlatformData
            {
                Enabled = true,

                Name = name,
                Channel = channel,
                PlatformType = "vk"
            };

            SaveData();
        }
        internal VK(PlatformData data, int index)
        {
            Index = index;
            Data = data;

            SaveData();
        }

        protected override IEnumerator<UnityWebRequestAsyncOperation> GetChatMessages()
        {
            var token = PlayerPrefs.GetString(TokenPref);
            if (!string.IsNullOrEmpty(token))
                using (var request = UnityWebRequest
                    .Get($"{EntryPath}/v1/chat/messages?channel_url={Data.Channel}&limit={MessagesLimit}"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {token}");

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var root = JsonConvert.DeserializeObject<RootObject>(request.downloadHandler.text);

                        var startCollecting = LastMessage == 0;
                        for (int m = root.data.chat_messages.Count - 1; m >= 0; m--)
                        {
                            var message = root.data.chat_messages[m];
                            if (startCollecting)
                                MC_Messages.Enqueue(new MC_Message
                                {
                                    Nick = message.author.nick,
                                    Parts = GetParts(message),
                                });
                            else if (message.id == LastMessage)
                                startCollecting = true;
                        }

                        LastMessage = root.data.chat_messages[0].id;
                    }
                    else
                        Debug.LogError(request.error);
                }

            List<MC_Message.MessagePart> GetParts(ChatMessage message)
            {
                var parts = new List<MC_Message.MessagePart>();
                for (int p = 0; p < message.parts.Count; p++)
                {
                    var part = message.parts[p];
                    parts.Add(new MC_Message.MessagePart
                    {
                        Mention = part.mention == null ? default : new MC_Message.Mention { Nick = part.mention.nick },
                        Smile = part.smile == null ? default : new MC_Message.Smile { URL = part.smile.small_url },
                        Text = part.text == null ? default : new MC_Message.Text { Content = part.text.content },
                    });
                }

                return parts;
            }
        }

        internal static void StartAuth() =>
            Application.OpenURL($"{AuthPath}?client_id={AppID}&redirect_uri={RedirectPath}&response_type=token");
        internal static bool SubmitToken(string url, out string token)
        {
            token = null;
            if (url.Contains("access_token="))
            {
                var uri = new Uri(url);
                token = System.Web.HttpUtility.ParseQueryString(uri.Fragment.TrimStart('#'))["access_token"];

                if (!string.IsNullOrEmpty(token))
                {
                    PlayerPrefs.SetString(TokenPref, token);

                    return true;
                }
            }

            return false;
        }

        #region message types
        class RootObject
        {
            public Messages data;
        }
        class Messages
        {
            public List<ChatMessage> chat_messages;
        }
        class ChatMessage
        {
            public Author author;
            public long created_at;
            public long id;
            public bool is_private;
            public List<MessagePart> parts;
        }
        class Author
        {
            public string avatar_url;
            public List<Badge> badges;
            public long id;
            public bool is_moderator;
            public bool is_owner;
            public string nick;
            public long nick_color;
            public List<Role> roles;
        }
        class Badge
        {
            public string achievement_name;
            public string id;
            public string large_url;
            public string medium_url;
            public string name;
            public string small_url;
        }
        class Role
        {
            public string id;
            public string large_url;
            public string medium_url;
            public string name;
            public string small_url;
        }
        class MessagePart
        {
            public Link link;
            public Mention mention;
            public Smile smile;
            public Text text;
        }
        class Link
        {
            public string content;
            public string url;
        }
        class Mention
        {
            public long id;
            public string nick;
        }
        class Smile
        {
            public bool animated;
            public string id;
            public string large_url;
            public string medium_url;
            public string name;
            public string small_url;
        }
        class Text
        {
            public string content;
        }
        #endregion
    }
}