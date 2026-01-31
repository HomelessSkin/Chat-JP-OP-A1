using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Core;

using Input;

using UI;

using UnityEngine;

namespace MultiChat
{
    public class MultiChatManager : UIManagerBase
    {
        #region CANVASSER
        void RenderMainCanvas() => _Canvasser.QueueRender();
        void RenderOBSCanvas() => _Canvasser.QueueRender(1);
        #endregion

        #region DRAWER
        protected override void RedrawTheme(IStorage.Data storage)
        {
            base.RedrawTheme(storage);

            for (int m = 0; m < _Chat.Messages.Count; m++)
            {
                var message = _Chat.Messages[m];
                var drawables = message.GetComponentsInChildren<Drawable>();
                for (int d = 0; d < drawables.Length; d++)
                {
                    var drawable = drawables[d];
                    if (!(drawable as IRedrawable).IsRedrawable())
                        continue;
                    if (TryGetDrawerData(drawable.GetKey(), out var sprite))
                        drawable.SetData(sprite);
                }
            }

            for (int m = 0; m < _Chat.Pool.Count; m++)
            {
                var message = _Chat.Pool[m];
                var drawables = message.GetComponentsInChildren<Drawable>();
                for (int d = 0; d < drawables.Length; d++)
                {
                    var drawable = drawables[d];
                    if (!(drawable as IRedrawable).IsRedrawable())
                        continue;
                    if (TryGetDrawerData(drawable.GetKey(), out var sprite))
                        drawable.SetData(sprite);
                }
            }
        }
        #endregion

        [Space]
        [SerializeField] Chat _Chat;
        #region CHAT
        [Serializable]
        class Chat : WindowBase
        {
            public int MaxChatCount = 100;
            public Transform Content;
            public GameObject MessagePrefab;

            [HideInInspector]
            public List<ChatMessage> Messages = new List<ChatMessage>();
            [HideInInspector]
            public List<ChatMessage> Pool = new List<ChatMessage>();
        }

        public void ClearChat()
        {
            for (int m = 0; m < _Chat.Messages.Count; m++)
                RemoveMessage(_Chat.Messages[m]);

            _Chat.Messages.Clear();
        }
        internal void DeleteMessage(string platform, string id)
        {
            var index = -1;
            for (int m = 0; m < _Chat.Messages.Count; m++)
            {
                var message = _Chat.Messages[m];
                if (message.GetPlatform() == platform &&
                     message.GetID() == id)
                {
                    index = m;

                    RemoveMessage(message);

                    break;
                }
            }

            if (index >= 0)
                _Chat.Messages.RemoveAt(index);
        }

        protected virtual void OnMessage(OuterInput message)
        {
            var m = FromPool();
            m.Init(message, this);

            _Chat.Messages.Add(m);
        }

        void RemoveMessage(ChatMessage message)
        {
            Smiles.RemoveRange(message.GetSmiles(), message.gameObject);
            Badges.RemoveRange(message.GetBadges(), message.gameObject);

            ToPool(message);
        }
        void ToPool(ChatMessage message)
        {
            message.transform.SetParent(null);
            message.gameObject.SetActive(false);

            _Chat.Pool.Add(message);
        }
        ChatMessage FromPool()
        {
            ChatMessage message;
            if (_Chat.Messages.Count >= _Chat.MaxChatCount)
            {
                message = _Chat.Messages[0];

                message.transform.SetParent(null);
                message.transform.SetParent(_Chat.Content);

                _Chat.Messages.RemoveAt(0);
            }
            else if (_Chat.Pool.Count > 0)
            {
                message = _Chat.Pool[0];
                message.transform.SetParent(_Chat.Content);
                message.gameObject.SetActive(true);

                _Chat.Pool.RemoveAt(0);
            }
            else
                message = Instantiate(_Chat.MessagePrefab, _Chat.Content, false).GetComponent<ChatMessage>();

            return message;
        }

        #region SMILES
        internal bool HasSmile(int hash, bool reserveIfFalse = false)
        {
            if (Smiles.HasSprite(hash))
                return true;

            if (Smiles.IsKeyReserved(hash))
                return true;

            if (reserveIfFalse)
                Smiles.ReserveKey(hash);

            return false;
        }
        internal int GetSmileID(int key, GameObject requester) => Smiles.GetSpriteID(key, requester);
        internal void DrawSmile(Texture2D smile, int hash) => Smiles.Draw(smile, hash);
        #endregion

        #region BADGES
        internal bool HasBadge(int hash, bool reserveIfFalse = false)
        {
            if (Badges.HasSprite(hash))
                return true;

            if (Badges.IsKeyReserved(hash))
                return true;

            if (reserveIfFalse)
                Badges.ReserveKey(hash);

            return false;
        }
        internal bool HasBadge(int hash) => Badges.HasSprite(hash);
        internal int GetBadgeID(int key, GameObject requester) => Badges.GetSpriteID(key, requester);
        internal void DrawBadge(Texture2D badge, int hash) => Badges.Draw(badge, hash);

        Task ChatUpd;
        #endregion

        #endregion

        [Space]
        [SerializeField] StreamingSprites Smiles;

        [Space]
        [SerializeField] StreamingSprites Badges;

        protected override void Awake()
        {
            base.Awake();

            Smiles.Prepare();
            Badges.Prepare();

            Log.AddReadListener(RenderMainCanvas);
        }
    }
}