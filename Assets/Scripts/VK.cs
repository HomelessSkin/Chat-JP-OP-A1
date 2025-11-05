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
        internal static void StartAuth() =>
            Application.OpenURL($"{AuthPath}?client_id={AppID}&redirect_uri={RedirectPath}&response_type=token");

        static string AppID = "ksxptucqm12f6cp5";
        static string AuthPath = "https://auth.live.vkvideo.ru/app/oauth2/authorize";
        static string EntryPath = "https://apidev.live.vkvideo.ru";
        static string SocketURL = "wss://pubsub-dev.live.vkvideo.ru/connection/websocket?format=json&cf_protocol_version=v2";
        static string[] Colors = new string[]
        {
            "d66e34", "b8aaff", "1d90ff", "9961f9", "59a840", "e73629", "de6489", "20bba1",
            "f8b301", "0099bb", "7bbeff", "e542ff", "a36c59", "8ba259", "00a9ff", "a20bff"
        };

        Queue<SocketMessage> Responses = new Queue<SocketMessage>();

        internal VK(string name, string channel, int index, MultiChatManager manager) : base(name, channel, index, manager)
        {
            Data.PlatformType = "vk";

            SaveData();
        }
        internal VK(PlatformData data, int index, MultiChatManager manager) : base(data, index, manager)
        {
            Connect();

            SaveData();
        }

        protected override async void Connect()
        {
            if (Data.Enabled)
            {
                using (var request = UnityWebRequest.Get(EntryPath + "/v1/websocket/token"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");

                    var oper = request.SendWebRequest();
                    while (!oper.isDone)
                        await System.Threading.Tasks.Task.Yield();

                    if (request.result == UnityWebRequest.Result.Success)
                        Data.ChannelID = JsonConvert.DeserializeObject<JWT>(request.downloadHandler.text).data.token;
                    else
                        Debug.Log(request.error);
                }

                InitializeSocket(SocketURL);
            }
        }
        protected override void OnOpen(object sender, EventArgs e)
        {
            Socket.Send(JsonConvert.SerializeObject(new ClientMessage
            {
                id = (uint)MessageType.Connection,
                connect = new ClientMessage.Connect
                {
                    token = Data.ChannelID
                }
            }));

            SubscribeToEvent("chat");
        }
        protected override void OnMessage(object sender, MessageEventArgs e)
        {
            if (MultiChatManager.DebugSocket)
                Debug.Log(e.Data);

            Responses.Enqueue(JsonConvert.DeserializeObject<SocketMessage>(e.Data));
        }
        protected override async void SubscribeToEvent(string type)
        {
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

                    var message = new ClientMessage { };
                    switch (type)
                    {
                        case "chat":
                        message.id = (uint)MessageType.ChatSub;
                        message.subscribe = new ClientMessage.Sub { channel = $"{channel.web_socket_channels.chat}" };
                        break;
                    }

                    Socket.Send(JsonConvert.SerializeObject(message));
                }
                else
                    Debug.Log(request.error);
            }
        }
        protected override void ProcessSocketMessages()
        {
            while (Responses.Count > 0)
            {
                var socket = Responses.Dequeue();
                if (socket.id != 0u)
                {
                    Debug.Log($"Successfull {(MessageType)socket.id}");

                    continue;
                }

                if (socket.push == null)
                {
                    Socket.Send("{}");

                    continue;
                }

                var data = socket.push.pub.data;
                switch (data.type)
                {
                    case "channel_chat_message_send":
                    {
                        var message = data.data.chat_message;
                        if (message.author.nick == "ChatBot")
                            continue;

                        if (GetParts(message, out var parts))
                            Enqueue(new MC_Message
                            {
                                Platform = 0,
                                ID = message.id.ToString(),
                                Nick = message.author.nick,
                                Color = $"#{Colors[message.author.nick_color]}",
                                Parts = parts,
                            });
                    }
                    break;
                    case "channel_chat_message_delete":
                    {
                        Manager.DeleteMessage(0, data.data.chat_message.id.ToString());
                    }
                    break;
                }
            }

            bool GetParts(SocketMessage.Push.Pub.Data.DataData.ChatMessage message, out List<MC_Message.Part> parts)
            {
                parts = new List<MC_Message.Part>();
                var tasks = new List<Task>();
                for (int p = 0; p < message.parts.Count; p++)
                {
                    var part = message.parts[p];
                    var mc = new MC_Message.Part { };

                    if (part.link != null && !string.IsNullOrEmpty(part.link.content))
                    {
                        parts = null;

                        return false;
                    }

                    if (part.mention != null)
                        mc.Reply = new MC_Message.Part.Mention { Nick = part.mention.nick };
                    if (part.smile != null && !string.IsNullOrEmpty(part.smile.medium_url))
                        mc.Emote = new MC_Message.Part.Smile { Hash = part.smile.id.GetHashCode(), URL = part.smile.medium_url };
                    if (part.text != null)
                        mc.Message = new MC_Message.Part.Text { Content = part.text.content };

                    parts.Add(mc);
                }

                return true;
            }
        }

        #region JWT
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
        #endregion

        #region CHANNELS REQUEST
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
        #endregion

        #region SOCKET MESSAGE
        [Serializable]
        class SocketMessage
        {
            public uint id;
            public Push push;

            [Serializable]
            public class Push
            {
                public string channel { get; set; }
                public Pub pub;

                [Serializable]
                public class Pub
                {
                    public Data data;

                    [Serializable]
                    public class Data
                    {
                        public string type;
                        public DataData data;

                        [Serializable]
                        public class DataData
                        {
                            public ChatMessage chat_message;

                            [Serializable]
                            public class ChatMessage
                            {
                                public long id;
                                public Author author;
                                public List<Part> parts;

                                [Serializable]
                                public class Author
                                {
                                    public long id;
                                    public bool is_owner;
                                    public string nick;
                                    public int nick_color;
                                    public List<Badge> badges;
                                    public List<Role> roles;
                                    public bool is_moderator;

                                    [Serializable]
                                    public class Badge
                                    {
                                        public string id;
                                        public string medium_url;
                                    }

                                    [Serializable]
                                    public class Role
                                    {

                                    }
                                }

                                [Serializable]
                                public class Part
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
                        }
                    }
                }
            }
        }
        #endregion

        #region CLIENT MESSAGE
        [Serializable]
        class ClientMessage
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
        #endregion

        enum MessageType : uint
        {
            Connection = 1u,
            ChatSub = 2u,
        }
    }
}