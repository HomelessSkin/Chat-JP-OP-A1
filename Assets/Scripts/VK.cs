using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

using WebSocketSharp;

namespace MultiChat
{
    internal class VK : Platform
    {
        internal static string TokenPref = "vk_token";

        static string AppID = "ksxptucqm12f6cp5";
        static string AuthPath = "https://auth.live.vkvideo.ru/app/oauth2/authorize";
        static string RedirectPath = "https://oauth.vk.com/blank.html";
        static string EntryPath = "https://apidev.live.vkvideo.ru";
        static string SocketURL = "wss://pubsub-dev.live.vkvideo.ru/connection/websocket?format=json&cf_protocol_version=v2";

        WebSocket Socket;

        internal VK(string name, string channel, int index, MultiChatManager manager) : base(name, channel, index, manager)
        {
            Data.PlatformType = "vk";

            SaveData();
        }
        internal VK(PlatformData data, int index, MultiChatManager manager) : base(data, index, manager)
        {
            ManageConnection(true);

            SaveData();
        }

        protected override async void GetChatMessages()
        {
            return;

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

        //async void SubscribeToEvent(string type)
        //{
        //    var data = JsonConvert.SerializeObject(new EventSubRequest
        //    {
        //        type = type,
        //        version = "1",
        //        condition = new EventSubRequest.Condition
        //        {
        //            broadcaster_user_id = Data.ChannelID,
        //            user_id = Data.ChannelID,
        //        },
        //        transport = new EventSubRequest.Transport
        //        {
        //            method = "websocket",
        //            session_id = $"{Data.SessionID}",
        //        },
        //    });

        //    using (var request = UnityWebRequest.Post(EventSubURL, "", "application/json"))
        //    {
        //        request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");
        //        request.SetRequestHeader("Client-Id", AppID);

        //        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
        //        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        //        request.downloadHandler = new DownloadHandlerBuffer();

        //        var oper = request.SendWebRequest();
        //        while (!oper.isDone)
        //            await Task.Yield();

        //        if (request.result == UnityWebRequest.Result.Success)
        //            Debug.Log($"Subscribed successfully to a {type} event");
        //        else
        //            Debug.LogError($"Failed to create subscription: {request.error}");
        //    }
        //}
        async void ManageConnection(bool value)
        {
            if (value)
            {
                using (var request = UnityWebRequest.Get(EntryPath + "/v1/websocket/token"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");

                    var oper = request.SendWebRequest();
                    while (!oper.isDone)
                        await System.Threading.Tasks.Task.Yield();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Data.ChannelID = JsonConvert.DeserializeObject<JWT>(request.downloadHandler.text).data.token;
                        Debug.Log(Data.ChannelID);
                    }
                    else
                        Debug.Log(request.error);
                }

                if (Socket != null)
                    Socket.Close();

                Socket = new WebSocket(SocketURL);
                Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                Socket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                Socket.OnOpen += OnOpen;
                Socket.OnMessage += OnMessage;
                Socket.OnClose += OnClose;
                Socket.OnError += OnError;

                Socket.Connect();
            }

            async void OnOpen(object sender, EventArgs e)
            {
                var connect = JsonConvert.SerializeObject(new Message { id = 1u, connect = new Message.Connect { token = Data.ChannelID } });
                Socket.Send(connect);

                using (var request = UnityWebRequest.Post(EntryPath +
                    $"/v1/channels" +
                    $"?body={Data.ChannelName}", "", "application/json"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");

                    var body = new ChannelsRequest
                    {
                        channels = new ChannelsRequest.Channel[] { new ChannelsRequest.Channel { url = "ru_1ned" } }
                    };

                    var handler = new UploadHandlerRaw(new System.Text.UTF8Encoding().GetBytes(JsonConvert.SerializeObject(body)));
                    request.uploadHandler = handler;
                    request.downloadHandler = new DownloadHandlerBuffer();

                    var oper = request.SendWebRequest();
                    while (!oper.isDone)
                        await Task.Yield();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var channel = JsonConvert.DeserializeObject<ChannelsResponse>(request.downloadHandler.text).data.channels[0].channel;
                        var message = new Message { id = 1u, subscribe = new Message.Sub { channel = $"{channel.web_socket_channels.chat}" } };

                        Socket.Send(JsonConvert.SerializeObject(message));
                    }
                    else
                        Debug.Log(request.error);
                }
            }
            void OnMessage(object sender, MessageEventArgs e) => Debug.Log(e.Data);
            void OnClose(object sender, CloseEventArgs e) => Debug.Log(e.Reason);
            void OnError(object sender, ErrorEventArgs e) => Debug.Log(e.Message);
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

        [Serializable]
        class JWT
        {
            public Data data;

            [Serializable]
            public class Data
            {
                public string token;
            }
        }

        [Serializable]
        class ChannelsRequest
        {
            public Channel[] channels;

            [Serializable]
            public class Channel
            {
                public string url;
            }
        }

        [Serializable]
        class ChannelsResponse
        {
            public Data data;

            [Serializable]
            public class Data
            {
                public ChannelData[] channels;

                [Serializable]
                public class ChannelData
                {
                    public Channel channel;

                    [Serializable]
                    public class Channel
                    {
                        public string id;
                        public string url;
                        public WebSocketChannels web_socket_channels;

                        [Serializable]
                        public class WebSocketChannels
                        {
                            public string chat;
                            public string private_chat;
                            public string info;
                            public string private_info;
                            public string channel_points;
                            public string private_channel_points;
                            public string limited_chat;
                            public string limited_private_chat;
                        }
                    }
                }
            }
        }

        [Serializable]
        class Message
        {
            public uint id;
            public string type;
            public Connect connect;
            public Sub subscribe;

            [Serializable]
            public class Sub
            {
                public string channel;
            }

            [Serializable]
            public class Connect
            {
                public string token;
            }
        }
    }
}