using System.Collections.Generic;

using TMPro;

using UnityEngine;

namespace MultiChat
{
    internal class ChatMessage : MonoBehaviour
    {
        [SerializeField] TMP_Text Content;

        MC_Message MC;

        MultiChatManager Manager;

        List<int> Smiles = new List<int>();
        List<int> Badges = new List<int>();

        internal byte GetPlatform() => MC.Platform;
        internal string GetID() => MC.ID;
        internal List<int> GetSmiles() => Smiles;
        internal List<int> GetBadges() => Badges;

        internal void Init(MC_Message message, MultiChatManager manager)
        {
            Smiles.Clear();

            MC = message;
            Manager = manager;

            var color = "#808080";
            if (!string.IsNullOrEmpty(message.NickColor))
                color = message.NickColor;

            var text = "";

            if (message.Badges != null)
                for (int b = 0; b < message.Badges.Count; b++)
                {
                    var badge = message.Badges[b];
                    text += $"<sprite name=\"Badges_{Manager.GetBadgeID(badge.Hash, gameObject)}\">";

                    if (!Badges.Contains(badge.Hash))
                        Badges.Add(badge.Hash);
                }

            text += $"<color={color}>{message.Nick}</color>: ";

            if (message.Parts != null)
            {
                if (message.IsSlashMe)
                    text += $"<color={message.NickColor}><i>";

                for (int pt = 0; pt < message.Parts.Count; pt++)
                {
                    var part = message.Parts[pt];
                    if (!string.IsNullOrEmpty(part.Message.Content))
                        text += part.Message.Content;
                    if (part.Emote.Draw)
                    {
                        var id = Manager.GetSmileID(part.Emote.Hash, gameObject);
                        text += $"<sprite name=\"Smiles_{id}\">";

                        if (!Smiles.Contains(part.Emote.Hash))
                            Smiles.Add(part.Emote.Hash);
                    }
                }

                if (message.IsSlashMe)
                    text += $"</color></i>";
            }

            Content.text = text;
        }
    }
}