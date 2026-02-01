using System.Collections.Generic;

using Input;

using TMPro;

using UnityEngine;

namespace MultiChat
{
    internal class ChatMessage : MonoBehaviour
    {
        [SerializeField] TMP_Text Content;

        OuterInput Input;

        MultiChatManager Manager;

        List<int> Smiles = new List<int>();
        List<int> Badges = new List<int>();

        internal string GetPlatform() => Input.Platform;
        internal string GetID() => Input.ID;
        internal List<int> GetSmiles() => Smiles;
        internal List<int> GetBadges() => Badges;

        internal void Init(OuterInput input, MultiChatManager manager)
        {
            Smiles.Clear();

            Input = input;
            Manager = manager;

            var color = "#808080";
            if (!string.IsNullOrEmpty(input.NickColor))
                color = input.NickColor;

            var text = "";

            //if (input.Badges != null)
            //    for (int b = 0; b < input.Badges.Count; b++)
            //    {
            //        var badge = input.Badges[b];
            //        text += $"<sprite name=\"Badges_{Manager.GetBadgeID(badge.Hash, gameObject)}\">";

            //        if (!Badges.Contains(badge.Hash))
            //            Badges.Add(badge.Hash);
            //    }

            text += $"<color={color}>{input.Nick}</color>: ";

            if (input.UserInput != null)
            {
                if (input.IsSlashMe)
                    text += $"<color={input.NickColor}><i>";

                for (int pt = 0; pt < input.UserInput.Count; pt++)
                {
                    var part = input.UserInput[pt];
                    if (!string.IsNullOrEmpty(part.Message.Content))
                        text += part.Message.Content;

                    //if (part.Emote.Draw)
                    //{
                    //    var id = Manager.GetSmileID(part.Emote.Hash, gameObject);
                    //    text += $"<sprite name=\"Smiles_{id}\">";

                    //    if (!Smiles.Contains(part.Emote.Hash))
                    //        Smiles.Add(part.Emote.Hash);
                    //}
                }

                if (input.IsSlashMe)
                    text += $"</color></i>";
            }

            Content.text = text;
        }
    }
}