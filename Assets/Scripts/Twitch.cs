using System;
using System.Collections.Generic;

using UnityEngine;

using WebSocketSharp;

namespace MultiChat
{
    internal class Twitch : Platform
    {
        internal static string TokenPref = "twitch_token";

        internal new bool Enabled
        {
            get => base.Enabled;
            set
            {
                ManageConnection(value);

                base.Enabled = value;
            }
        }

        static string AppID = "6ss2l29z27gl1rmz061rajdhd9mgr6";
        static string AuthPath = "https://id.twitch.tv/oauth2/authorize";
        static string RedirectPath = "https://oauth.vk.com/blank.html";
        //static string EntryPath = "https://apidev.live.vkvideo.ru";
        static string SocketURL = "wss://eventsub.wss.twitch.tv/ws";
        //static string SocketURL = "wss://irc-ws.chat.twitch.tv:443";

        WebSocket Socket;

        internal Twitch(string name, string channel, int index, MultiChatManager manager) : base(name, channel, index, manager)
        {
            Socket = new WebSocket(SocketURL);
            Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            Socket.SslConfiguration.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            Socket.OnOpen += OnOpen;
            Socket.OnMessage += OnMessage;
            Socket.OnClose += OnClose;
            Socket.OnError += OnError;

            Socket.Connect();

            SaveData();
        }

        internal Twitch(PlatformData data, int index, MultiChatManager manager) : base(data, index, manager)
        {



            SaveData();
        }

        internal static void StartAuth() =>
            Application.OpenURL($"{AuthPath}?response_type=token&client_id={AppID}&redirect_uri={RedirectPath}&scope=user%3Aread%3Achat");

        protected override void GetChatMessages() { }

        void OnOpen(object sender, EventArgs e)
        {
            //Socket.Send($"PASS {PlayerPrefs.GetString(TokenPref)}");
            //Socket.Send($"NICK ru_1n");
            //Socket.Send($"JOIN #ru_1n");
        }
        void OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Data.StartsWith("PING"))
            {
                Socket.Send("PONG :tmi.twitch.tv");

                Debug.Log("PONG");
            }
            else
                Debug.Log(e.Data);
        }
        void OnClose(object sender, CloseEventArgs e) => Debug.Log(e.Reason);
        void OnError(object sender, ErrorEventArgs e) => Debug.Log(e.Message);
        void ManageConnection(bool value)
        {
        }

        [System.Serializable]
        public class LoginRequest
        {
            public string username;
            public string password;
        }

        [System.Serializable]
        public class RegisterRequest
        {
            public string username;
            public string password;
        }

        [System.Serializable]
        public class RefreshTokenRequest
        {
            public string refreshToken;
        }

        [System.Serializable]
        public class SubscribeRequest
        {
            public string roomId;
        }

        [System.Serializable]
        public class SendMessageRequest
        {
            public string roomId;
            public string message;
        }

        [System.Serializable]
        public class TokenResponse
        {
            public bool success;
            public string message;
            public string userId;
            public string accessToken;
            public string refreshToken;
            public int expiresIn;
        }

        [System.Serializable]
        public class AuthResponse
        {
            public bool success;
            public string message;
        }

        [System.Serializable]
        public class TokenValidationResponse
        {
            public bool valid;
            public string userId;
            public string username;
        }

        [System.Serializable]
        public class SubscribeResponse
        {
            public bool success;
            public string message;
        }

        [System.Serializable]
        public class MessagesResponse
        {
            public bool success;
            public List<ChatMessage> messages;
        }

        [System.Serializable]
        public class ChatMessage
        {
            public Subscription subscription;
            public Event eventData;
        }

        [System.Serializable]
        public class Subscription
        {
            public string id;
            public string status;
            public string type;
            public string version;
            public Condition condition;
            public Transport transport;
            public string created_at;
            public int cost;
        }

        [System.Serializable]
        public class Condition
        {
            public string broadcaster_user_id;
            public string user_id;
        }

        [System.Serializable]
        public class Transport
        {
            public string method;
            public string callback;
        }

        [System.Serializable]
        public class Event
        {
            public string broadcaster_user_id;
            public string broadcaster_user_login;
            public string broadcaster_user_name;
            public string chatter_user_id;
            public string chatter_user_login;
            public string chatter_user_name;
            public string message_id;
            public Message message;
            public string color;
            public Badge[] badges;
            public string message_type;
            public Cheer cheer;
            public Reply reply;
            public string channel_points_custom_reward_id;
        }

        [System.Serializable]
        public class Message
        {
            public string text;
            public Fragment[] fragments;
        }

        [System.Serializable]
        public class Fragment
        {
            public string type;
            public string text;
            public Cheermote cheermote;
            public Emote emote;
            public Mention mention;
        }

        [System.Serializable]
        public class Cheermote
        {
            // Можно добавить поля если нужны
            public string prefix;
            public int bits;
            public int tier;
        }

        [System.Serializable]
        public class Emote
        {
            public string id;
            public string emote_set_id;
            public string owner_id;
            public string[] format;
        }

        [System.Serializable]
        public class Mention
        {
            public string user_id;
            public string user_login;
            public string user_name;
        }

        [System.Serializable]
        public class Badge
        {
            public string set_id;
            public string id;
            public string info;
        }

        [System.Serializable]
        public class Cheer
        {
            public int bits;
        }

        [System.Serializable]
        public class Reply
        {
            public string parent_message_id;
            public string parent_message_body;
            public ParentUser parent_user;
            public ThreadUser thread_user;
        }

        [System.Serializable]
        public class ParentUser
        {
            public string user_id;
            public string user_login;
            public string user_name;
        }

        [System.Serializable]
        public class ThreadUser
        {
            public string user_id;
            public string user_login;
            public string user_name;
        }
    }
}
