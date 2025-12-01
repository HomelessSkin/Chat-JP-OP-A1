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
    internal class Twitch : Platform
    {
        internal static string TokenPref = "twitch_token";
        internal static void StartAuth(string[] scopes)
        {
            var scope = "scope=";
            if (scopes != null)
                for (int s = 0; s < scopes.Length; s++)
                    scope += $"{scopes[s]}" + (s == scopes.Length - 1 ? "" : "%20");

            Application.OpenURL($"{AuthPath}?response_type=token&client_id={AppID}&redirect_uri={RedirectPath}&{scope}");
        }

        protected static string AppID = "6ss2l29z27gl1rmz061rajdhd9mgr6";
        protected static string AuthPath = "https://id.twitch.tv/oauth2/authorize";
        protected static string SocketURL = "wss://eventsub.wss.twitch.tv/ws";
        protected static string EventSubURL = "https://api.twitch.tv/helix/eventsub/subscriptions";
        protected static string GetUsersURL = "https://api.twitch.tv/helix/users";
        protected static string EmoteURL = "https://static-cdn.jtvnw.net/emoticons/v2";

        protected Queue<SocketMessage> Responses = new Queue<SocketMessage>();

        internal Twitch(string name, string channel) : base(name, channel)
        {
            Data.Type = "twitch";

            Connect();
        }
        internal Twitch(PlatformData data) : base(data)
        {
            Connect();
        }

        protected override async void Connect()
        {
            if (Data.Enabled)
            {
                using (var request = UnityWebRequest.Get(GetUsersURL + $"?login={Data.Channel}"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");
                    request.SetRequestHeader("Client-ID", AppID);

                    var oper = request.SendWebRequest();
                    while (!oper.isDone)
                        await Task.Yield();

                    if (request.result == UnityWebRequest.Result.Success)
                        Data.ChannelID = JsonUtility
                            .FromJson<UserResponse>(request.downloadHandler.text)
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

            Responses.Enqueue(JsonUtility.FromJson<SocketMessage>(e.Data));
        }
        protected override async Task<bool> SubscribeToEvent(string type)
        {
            var data = JsonUtility.ToJson(new EventSubRequest
            {
                type = type,
                version = "1",
                condition = new Condition
                {
                    broadcaster_user_id = Data.ChannelID,
                    user_id = Data.ChannelID,
                },
                transport = new Transport
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

                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Manager.AddMessage($"Subscribed successfully to:\n{type} event");

                    return true;
                }
                else
                {
                    Manager.AddMessage($"Failed to create subscription to:\n{type} event.\nWith error:\n{request.error}", UIManagerBase.LogLevel.Error);

                    return false;
                }
            }
        }
        protected override async void ProcessSocketMessages()
        {
            while (Responses.Count > 0)
            {
                var message = Responses.Dequeue();
                switch (message.metadata.message_type)
                {
                    case "session_keepalive":
                    KeepAlive();
                    break;
                    case "session_welcome":
                    if (await SessionWelcome(message))
                    {

                    }
                    else
                    {

                    }
                    break;
                    case "notification":
                    Notification(message);
                    break;
                }
            }
        }

        protected virtual void KeepAlive() { }
        protected virtual async Task<bool> SessionWelcome(SocketMessage message)
        {
            Data.SessionID = message.payload.session.id;

            var result = true;
            result &= await SubscribeToEvent("channel.chat.message");
            result &= await SubscribeToEvent("channel.chat.message_delete");

            return result;
        }
        protected virtual async void Notification(SocketMessage message)
        {
            switch (message.metadata.subscription_type)
            {
                case "channel.chat.message":
                {
                    Enqueue(new MC_Message
                    {
                        Platform = 1,
                        ID = message.payload.@event.message_id,

                        Badges = await GetBadges(message.payload.@event.badges),
                        NickColor = message.payload.@event.color,
                        Nick = message.payload.@event.chatter_user_name,
                        IsSlashMe = message.payload.@event.message.text.StartsWith("\u0001ACTION"),
                        Parts = GetParts(message.payload.@event.message.fragments),
                    });
                }
                break;
                case "channel.chat.message_delete":
                {
                    Manager.DeleteMessage(1, message.payload.@event.message_id);
                }
                break;
            }
        }

        protected List<MC_Message.Part> GetParts(Fragment[] fragments)
        {
            var parts = new List<MC_Message.Part>();
            for (int f = 0; f < fragments.Length; f++)
            {
                var fragment = fragments[f];
                var m = new MC_Message.Part();
                if (!string.IsNullOrEmpty(fragment.emote.id))
                {
                    var hash = fragment.emote.id.GetHashCode();
                    m.Emote = new MC_Message.Part.Smile { Hash = hash, Draw = true };

                    if (!Manager.HasSmile(hash, true))
                        m.Emote.URL = EmoteURL + $"/{fragment.emote.id}/static/light/2.0";
                }
                else if (fragment.text != null)
                    m.Message = new MC_Message.Part.Text { Content = fragments[f].text };

                parts.Add(m);
            }

            return parts;
        }
        protected async Task<List<MC_Message.Badge>> GetBadges(Badge[] badges)
        {
            var parts = new List<MC_Message.Badge>() { new MC_Message.Badge { Hash = 1 } };

            var refreshGlobal = false;
            var refreshSub = false;
            for (int f = 0; f < badges.Length; f++)
            {
                var badge = badges[f];
                var hash = (badge.set_id + badge.id).GetHashCode();

                var needed = !Manager.HasBadge(hash, true);
                if (badge.set_id == "subscriber")
                    refreshSub |= needed;
                else
                    refreshGlobal |= needed;

                var b = new MC_Message.Badge
                {
                    IsNeeded = needed,
                    Hash = hash,
                    SetID = badge.set_id,
                    ID = badge.id,
                };

                parts.Add(b);
            }

            if (refreshGlobal)
                await RefreshGlobalSet();
            if (refreshSub)
                await RefreshSubSet();

            return parts;

            async Task RefreshGlobalSet()
            {
                using (var request = UnityWebRequest.Get($"https://api.twitch.tv/helix/chat/badges" + "/global"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");
                    request.SetRequestHeader("Client-ID", AppID);

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var list = new List<Task<Texture2D>>();
                        var response = JsonUtility.FromJson<BadgesResponse>(request.downloadHandler.text);

                        for (int p = 0; p < parts.Count; p++)
                        {
                            var badge = parts[p];
                            if (badge.IsNeeded && badge.SetID != "subscriber")
                            {
                                SetBadgeURL(response, ref badge);

                                parts[p] = badge;
                            }
                        }
                    }
                    else
                        Debug.Log(request.error);
                }
            }
            async Task RefreshSubSet()
            {
                using (var request = UnityWebRequest.Get($"https://api.twitch.tv/helix/chat/badges" +
                    $"?broadcaster_id={Data.ChannelID}"))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(TokenPref)}");
                    request.SetRequestHeader("Client-ID", AppID);

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var list = new List<Task<Texture2D>>();
                        var response = JsonUtility.FromJson<BadgesResponse>(request.downloadHandler.text);

                        for (int p = 0; p < parts.Count; p++)
                        {
                            var badge = parts[p];
                            if (badge.IsNeeded && badge.SetID == "subscriber")
                            {
                                SetBadgeURL(response, ref badge);

                                parts[p] = badge;
                            }
                        }
                    }
                    else
                        Debug.Log(request.error);
                }
            }

            void SetBadgeURL(BadgesResponse response, ref MC_Message.Badge badge)
            {
                for (int r = 0; r < response.data.Count; r++)
                    if (response.data[r].set_id == badge.SetID)
                    {
                        var stop = false;
                        var versions = response.data[r].versions;
                        for (int v = 0; v < versions.Count; v++)
                            if (versions[v].id == badge.ID)
                            {
                                stop = true;

                                badge.URL = versions[v].image_url_2x;

                                break;
                            }

                        if (stop)
                            break;
                    }
            }
        }
    }
}