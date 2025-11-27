using System;
using System.Collections.Generic;

namespace MultiChat.JSON
{
    [Serializable]
    public class JWT
    {
        public Data data;
    }

    [Serializable]
    public class Pub
    {
        public PubData data;
    }

    [Serializable]
    public class ChannelsResponse
    {
        public Data data;
    }

    [Serializable]
    public class DataData
    {
        public Message chat_message;
    }

    [Serializable]
    public class Link
    {
        public string content;
    }

    [Serializable]
    public class Data
    {
        public string token;
        public Channel channel;
    }

    [Serializable]
    public class Channel
    {
        public string id;
        public string url;
        public WebSocketChannels web_socket_channels;
    }

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

    [Serializable]
    public class SocketMessage
    {
        public uint id;
        public Push push;
        public Metadata metadata;
        public Payload payload;
    }

    [Serializable]
    public class Push
    {
        public string channel;
        public Pub pub;
    }

    [Serializable]
    public class PubData
    {
        public string type;
        public DataData data;
    }

    [Serializable]
    public class Message
    {
        public long id;
        public string text;
        public Author author;
        public List<Part> parts;
        public Fragment[] fragments;
    }

    [Serializable]
    public class Author
    {
        public long id;
        public bool is_owner;
        public string nick;
        public int nick_color;
        public bool is_moderator;
        public List<Badge> badges;
        public List<Role> roles;
    }

    [Serializable]
    public class Badge
    {
        public string id;
        public string medium_url;
        public string set_id;
    }

    [Serializable]
    public class Role
    {
        public string id;
        public string medium_url;
    }

    [Serializable]
    public class Part
    {
        public Link link;
        public Mention mention;
        public Smile smile;
        public Text text;
    }

    [Serializable]
    public class Mention
    {
        public string nick;
        public string user_name;
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

    [Serializable]
    public class ClientMessage
    {
        public uint id;
    }

    [Serializable]
    public class SubMessage : ClientMessage
    {
        public Sub subscribe;
    }

    [Serializable]
    public class Sub
    {
        public string channel;
    }

    [Serializable]
    public class ConnectMessage : ClientMessage
    {
        public Connect connect;
    }

    [Serializable]
    public class Connect
    {
        public string token;
    }

    [Serializable]
    public class EventSubRequest
    {
        public string type;
        public string version;
        public Condition condition;
        public Transport transport;
    }

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
    }

    [Serializable]
    public class Session
    {
        public string id;
    }

    [Serializable]
    public class Event
    {
        public string message_id;
        public string color;
        public string chatter_user_name;
        public Message message;
        public Cheer cheer;
        public Badge[] badges;
    }

    [Serializable]
    public class Fragment
    {
        public string type;
        public string text;
        public Cheermote cheermote;
        public Emote emote;
        public Mention mention;
    }

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
    }

    [Serializable]
    public class Cheer
    {
        public int bits;
    }

    [Serializable]
    public class UserResponse
    {
        public User[] data;
    }

    [Serializable]
    public class User
    {
        public string id;
    }

    [Serializable]
    public class BadgesResponse
    {
        public List<BadgeSet> data;
    }

    [Serializable]
    public class BadgeSet
    {
        public string set_id;
        public List<BadgeVersion> versions;
    }

    [Serializable]
    public class BadgeVersion
    {
        public string id;
        public string image_url_2x;
    }
}