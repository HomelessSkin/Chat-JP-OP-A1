using Core;

using TMPro;

using UI;

using UnityEngine;

namespace MultiChat
{
    public class ListPlatform : ScrollItem
    {
        [SerializeField] TMP_Text Enabled;
        [SerializeField] TMP_Text Type;
        [SerializeField] TMP_Text Channel;

        public override void Init(int index, IStorage.Data data, UIManagerBase manager)
        {
            base.Init(index, data, manager);

            var platform = (Platform.PlatformData)data;

            Enabled.text = $"{platform.Enabled}";
            Type.text = $"{platform.Type}";
            Channel.text = $"{platform.Channel}";
        }
    }
}