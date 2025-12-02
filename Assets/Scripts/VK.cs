using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using MultiChat.JSON;

using UI;

using UnityEngine;
using UnityEngine.Networking;

using WebSocketSharp;

namespace MultiChat
{
    internal class VK : Platform
    {
        internal static string TokenPref = "vk_token";
        internal static void StartAuth()
        {
            //var scope = "scope=";
            //if (scopes != null)
            //    for (int s = 0; s < scopes.Length; s++)
            //        scope += $"{scopes[s]}%20";

            //Application.OpenURL($"{AuthPath}?response_type=token&client_id={AppID}&redirect_uri={RedirectPath}&{scope}");
            Application.OpenURL($"{AuthPath}?client_id={AppID}&redirect_uri={RedirectPath}&response_type=token");
        }

        protected static string AppID = "p61uibtoabrmz18v";
        protected static string AuthPath = "https://auth.live.vkvideo.ru/app/oauth2/authorize";
        protected static string EntryPath = "https://apidev.live.vkvideo.ru";
        protected static string SocketURL = "wss://pubsub-dev.live.vkvideo.ru/connection/websocket?format=json&cf_protocol_version=v2";
        protected static string[] Colors = new string[]
            {
            "d66e34", "b8aaff", "1d90ff", "9961f9", "59a840", "e73629", "de6489", "20bba1",
            "f8b301", "0099bb", "7bbeff", "e542ff", "a36c59", "8ba259", "00a9ff", "a20bff"
            };

        protected Queue<SocketMessage> Responses = new Queue<SocketMessage>();

        internal VK(string name, string channel) : base(name, channel)
        {
            Data.Type = "vk";

            Connect();
        }
        internal VK(PlatformData data) : base(data)
        {
            Connect();
        }

        protected override async void Connect()
        {
            if (Data.Enabled)
            {
                using (var request = UnityWebRequest.Get(EntryPath + "/v1/websocket/token"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                        Data.ChannelID = JsonUtility.FromJson<JWT>(request.downloadHandler.text).data.token;
                    else
                        Manager.AddMessage($"{request.error}\n{Type}_Connect", UIManagerBase.LogLevel.Error);
                }

                InitializeSocket(SocketURL);
            }
        }
        protected override void OnOpen(object sender, EventArgs e)
        {
            Socket.Send(JsonUtility.ToJson(new ConnectMessage
            {
                id = (uint)MessageType.Connection,
                connect = new Connect
                {
                    token = Data.ChannelID
                }
            }));
        }
        protected override void OnMessage(object sender, MessageEventArgs e)
        {
            if (MultiChatManager.DebugSocket)
                Debug.Log(e.Data);

            Responses.Enqueue(JsonUtility.FromJson<SocketMessage>(e.Data));
        }
        protected override async Task<bool> SubscribeToEvent(string type)
        {
            using (var request = UnityWebRequest.Get(EntryPath +
                $"/v1/channel" +
                $"?channel_url={Channel.ToLower()}"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");
                request.downloadHandler = new DownloadHandlerBuffer();

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var channel = JsonUtility.FromJson<ChannelsResponse>(request.downloadHandler.text).data.channel;

                    var message = new SubMessage { };
                    switch (type)
                    {
                        case "chat":
                        message.id = (uint)MessageType.ChatSub;
                        message.subscribe = new Sub { channel = $"{channel.web_socket_channels.chat}" };
                        break;
                    }

                    Manager.AddMessage($"Send subscription\nto channel:\n{message.subscribe.channel}\n{Type}_Sub");
                    Socket.Send(JsonUtility.ToJson(message));

                    return true;
                }
                else
                {
                    Manager.AddMessage($"{request.error}\n{Type}_Sub", UIManagerBase.LogLevel.Error);

                    return false;
                }
            }
        }
        protected override void ProcessSocketMessages()
        {
            while (Responses.Count > 0)
            {
                var message = Responses.Dequeue();
                if (message.id != 0u)
                {
                    TechMessage(message);

                    continue;
                }

                if (message.push == null || message.push.pub == null)
                {
                    Ping();

                    continue;
                }

                Notification(message);
            }
        }

        protected virtual async void TechMessage(SocketMessage message)
        {
            Manager.AddMessage($"{(MessageType)message.id}\n{Type}_{Data.Name}_SocketMsg");
            switch ((MessageType)message.id)
            {
                case MessageType.Connection:
                {
                    await SubscribeToEvent("chat");
                }
                break;
                case MessageType.ChatSub:
                {

                }
                break;
            }
        }
        protected virtual void Ping()
        {
            Socket.Send("{}");
        }
        protected virtual void Notification(SocketMessage socket)
        {
            var data = socket.push.pub.data;
            switch (data.type)
            {
                case "channel_chat_message_send":
                {
                    var message = data.data.chat_message;
                    if (message.author.nick == "ChatBot")
                        return;

                    if (GetParts(message, out var parts))
                        EnqueueMessage(new MC_Message
                        {
                            Platform = 0,
                            ID = message.id.ToString(),

                            Badges = GetBadges(message.author),
                            NickColor = $"#{Colors[message.author.nick_color]}",
                            Nick = message.author.nick,
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

        protected bool GetParts(Message message, out List<MC_Message.Part> parts)
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
                {
                    var hash = part.smile.id.GetHashCode();
                    mc.Emote = new MC_Message.Part.Smile { Hash = hash, Draw = true };

                    if (!Manager.HasSmile(hash, true))
                        mc.Emote.URL = part.smile.medium_url;
                }
                if (part.text != null)
                    mc.Message = new MC_Message.Part.Text { Content = part.text.content };

                parts.Add(mc);
            }

            return true;
        }
        protected List<MC_Message.Badge> GetBadges(Author author)
        {
            var badges = new List<MC_Message.Badge>() { new MC_Message.Badge { Hash = 0 } };

            for (int r = 0; r < author.roles.Count; r++)
            {
                var role = author.roles[r];
                var hash = role.id.GetHashCode();

                var b = new MC_Message.Badge { Hash = hash, };
                if (!Manager.HasBadge(hash, true))
                    b.URL = role.medium_url;

                badges.Add(b);
            }

            for (int r = 0; r < author.badges.Count; r++)
            {
                var badge = author.badges[r];
                var hash = badge.id.GetHashCode();

                var b = new MC_Message.Badge { Hash = hash, };
                if (!Manager.HasBadge(hash, true))
                    b.URL = badge.medium_url;

                badges.Add(b);
            }

            return badges;
        }

        protected enum MessageType : uint
        {
            Connection = 1u,
            ChatSub = 2u,
        }
    }
}