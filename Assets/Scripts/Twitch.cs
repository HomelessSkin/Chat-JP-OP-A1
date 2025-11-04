using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

using WebSocketSharp;

namespace MultiChat
{
    internal class Twitch : Platform
    {
        internal static string TokenPref = "twitch_token";
        internal static void StartAuth() =>
            Application.OpenURL($"{AuthPath}?response_type=token&client_id={AppID}&redirect_uri={RedirectPath}&scope=user%3Aread%3Achat");

        static string AppID = "6ss2l29z27gl1rmz061rajdhd9mgr6";
        static string AuthPath = "https://id.twitch.tv/oauth2/authorize";
        static string SocketURL = "wss://eventsub.wss.twitch.tv/ws";
        static string EventSubURL = "https://api.twitch.tv/helix/eventsub/subscriptions";
        static string GetUsersURL = "https://api.twitch.tv/helix/users";
        static string EmoteURL = "https://static-cdn.jtvnw.net/emoticons/v2";

        Queue<SocketMessage> Responses = new Queue<SocketMessage>();

        internal Twitch(string name, string channel, int index, MultiChatManager manager) : base(name, channel, index, manager)
        {
            Data.PlatformType = "twitch";

            Connect();
            SaveData();
        }
        internal Twitch(PlatformData data, int index, MultiChatManager manager) : base(data, index, manager)
        {
            Connect();
            SaveData();
        }

        protected override async void Connect()
        {
            if (Data.Enabled)
            {
                using (var request = UnityWebRequest.Get(GetUsersURL + $"?login={Data.ChannelName}"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");
                    request.SetRequestHeader("Client-ID", AppID);

                    var oper = request.SendWebRequest();
                    while (!oper.isDone)
                        await Task.Yield();

                    if (request.result == UnityWebRequest.Result.Success)
                        Data.ChannelID = JsonConvert
                            .DeserializeObject<UserResponse>(request.downloadHandler.text)
                            .data[0]
                            .id;
                }

                InitializeSocket(SocketURL);
            }
        }
        protected override void OnOpen(object sender, EventArgs e)
        {
        }
        protected override void OnMessage(object sender, MessageEventArgs e)
        {
            if (MultiChatManager.DebugSocket)
            Debug.Log(e.Data);

            Responses.Enqueue(JsonConvert.DeserializeObject<SocketMessage>(e.Data));
        }
        protected override async void SubscribeToEvent(string type)
        {
            var data = JsonConvert.SerializeObject(new EventSubRequest
            {
                type = type,
                version = "1",
                condition = new EventSubRequest.Condition
                {
                    broadcaster_user_id = Data.ChannelID,
                    user_id = Data.ChannelID,
                },
                transport = new EventSubRequest.Transport
                {
                    method = "websocket",
                    session_id = $"{Data.SessionID}",
                },
            });

            using (var request = UnityWebRequest.Post(EventSubURL, "", "application/json"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");
                request.SetRequestHeader("Client-Id", AppID);

                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(data);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                var oper = request.SendWebRequest();
                while (!oper.isDone)
                    await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                    Debug.Log($"Subscribed successfully to a {type} event");
                else
                    Debug.LogError($"Failed to create subscription: {request.error}");
            }
        }
        protected override void ProcessSocketMessages()
        {
            while (Responses.Count > 0)
            {
                var message = Responses.Dequeue();

                switch (message.metadata.message_type)
                {
                    case "session_keepalive":
                    {

                    }
                    break;
                    case "session_welcome":
                    {
                        Data.SessionID = message.payload.session.id;

                        SubscribeToEvent("channel.chat.message");
                        SubscribeToEvent("channel.chat.message_delete");
                    }
                    break;
                    case "notification":
                    {
                        switch (message.metadata.subscription_type)
                        {
                            case "channel.chat.message":
                            {
                                var parts = new List<MC_Message.MessagePart>();
                                var fragments = message.payload.@event.message.fragments;
                                for (int f = 0; f < fragments.Length; f++)
                                {
                                    var fragment = fragments[f];
                                    var m = new MC_Message.MessagePart();
                                    if (fragment.emote != null)
                                        m.Emote = new MC_Message.MessagePart.Smile
                                        {
                                            Hash = fragment.emote.id.GetHashCode(),
                                            URL = EmoteURL + $"/{fragment.emote.id}/static/light/2.0",
                                        };
                                    else if (fragment.text != null)
                                        m.Message = new MC_Message.MessagePart.Text { Content = fragments[f].text };

                                    parts.Add(m);
                                }

                                Enqueue(new MC_Message
                                {
                                    Platform = 1,
                                    ID = message.payload.@event.message_id,
                                    Nick = message.payload.@event.chatter_user_name,
                                    Color = message.payload.@event.color,
                                    Parts = parts,
                                });
                            }
                            break;
                            case "channel.chat.message_delete":
                            {
                                Manager.DeleteMessage(1, message.payload.@event.message_id);
                                Debug.Log("delete");
                            }
                            break;
                        }
                    }
                    break;
                }
            }
        }

        #region SUB REQUEST
        [Serializable]
        class EventSubRequest
        {
            public string type;
            public string version;
            public Condition condition;
            public Transport transport;

            [Serializable]
            public class Condition
            {
                public string broadcaster_user_id;
                public string user_id;
            }

            [Serializable]
            public class Transport
            {
                public string method;
                public string session_id;
            }
        }
        #endregion

        #region SOCKET MESSAGE
        [Serializable]
        class SocketMessage
        {
            public Metadata metadata;
            public Payload payload;

            [Serializable]
            public class Metadata
            {
                public string message_type;
                public string subscription_type;
            }

            [Serializable]
            public class Payload
            {
                public Session session;
                public Event @event;

                public class Session
                {
                    public string id;
                }

                [Serializable]
                public class Event
                {
                    public string chatter_user_name;
                    public string color;
                    public string message_id;
                    public Message message;
                    public Badge[] badges;
                    public Cheer cheer;

                    [Serializable]
                    public class Message
                    {
                        public Fragment[] fragments;

                        [Serializable]
                        public class Fragment
                        {
                            public string type;
                            public string text;
                            public Cheermote cheermote;
                            public Emote emote;
                            public Mention mention;

                            [Serializable]
                            public class Cheermote
                            {
                                public string prefix;
                                public int bits;
                                public int tier;
                            }

                            [Serializable]
                            public class Emote
                            {
                                public string id;
                                public string owner_id;
                            }

                            [Serializable]
                            public class Mention
                            {
                                public string user_name;
                            }
                        }
                    }

                    [Serializable]
                    public class Badge
                    {
                        public string id;
                    }

                    [Serializable]
                    public class Cheer
                    {
                        public int bits;
                    }
                }
            }
        }
        #endregion

        #region USER INFO
        [Serializable]
        class UserResponse
        {
            public User[] data;

            [Serializable]
            public class User
            {
                public string id;
            }
        }
        #endregion
    }
}