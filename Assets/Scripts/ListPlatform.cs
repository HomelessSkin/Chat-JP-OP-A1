using TMPro;

using UnityEngine;

namespace MultiChat
{
    public class ListPlatform : MonoBehaviour
    {
        [SerializeField] TMP_Text Index;
        [SerializeField] TMP_Text Enabled;
        [SerializeField] TMP_Text Type;
        [SerializeField] TMP_Text Name;
        [SerializeField] TMP_Text Channel;

        MultiChatManager Manager;

        internal void Init(Platform data, MultiChatManager manager)
        {
            Index.text = $"{data.CurrentIndex}";
            Enabled.text = $"{data.Enabled}";
            Type.text = $"{data.Type}";
            Name.text = $"{data.Name}";
            Channel.text = $"{data.Channel}";

            Manager = manager;
        }
    }
}