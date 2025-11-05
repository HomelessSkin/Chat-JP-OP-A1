using System.Collections.Generic;

using TMPro;

using UnityEngine;

namespace MultiChat
{
    internal class Message : MonoBehaviour
    {
        [SerializeField] TMP_Text Content;

        MC_Message MC;

        MultiChatManager Manager;

        List<int> Smiles = new List<int>();

        internal byte GetPlatform() => MC.Platform;
        internal string GetID() => MC.ID;
        internal List<int> GetSmiles() => Smiles;

        internal void Init(MC_Message message, MultiChatManager manager)
        {
            Smiles.Clear();

            MC = message;
            Manager = manager;

            var color = "#808080";
            if (!string.IsNullOrEmpty(message.Color))
                color = message.Color;

            var text = "";
            for (int b = 0; b < message.Badges.Count; b++)
            {
                var id = Manager.GetBadgeID(message.Badges[b].Hash, gameObject);
                text += $"<sprite name=\"Badges_{id}\">";
            }

            text += $"<color={color}>" + message.Nick + "</color>: ";
            for (int pt = 0; pt < message.Parts.Count; pt++)
            {
                var part = message.Parts[pt];
                if (!string.IsNullOrEmpty(part.Message.Content))
                    text += part.Message.Content;
                if (!string.IsNullOrEmpty(part.Emote.URL))
                {
                    var id = Manager.GetSmileID(part.Emote.Hash, gameObject);
                    text += $"    <sprite name=\"Smiles_{id}\">    ";

                    if (!Smiles.Contains(part.Emote.Hash))
                        Smiles.Add(part.Emote.Hash);
                }
            }
            Content.text = text;
        }
    }
}