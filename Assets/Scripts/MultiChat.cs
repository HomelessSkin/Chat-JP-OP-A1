using System.Collections.Generic;

using Newtonsoft.Json;

using TMPro;

using UI;

using UnityEngine;

namespace MultiChat
{
    internal class MultiChatManager : UIManagerBase
    {
        public List<Texture2D> Textures;

        [SerializeField] float RefreshPeriod = 2f;

        float T;

        [Space]
        [Header("Auth")]
        [SerializeField] Transform AuthPanel;
        [SerializeField] TMP_InputField VK_TokenField;

        [Space]
        [Header("Platform Creation")]
        [SerializeField] TMP_Dropdown PlatformSwitch;
        [SerializeField] TMP_InputField PlatformNameInput;
        [SerializeField] TMP_InputField PlatformURLInput;

        [Space]
        [Header("Chat")]
        [SerializeField] Transform ChatContent;
        [SerializeField] GameObject MessagePrefab;

        List<Platform> Platforms = new List<Platform>();

        protected override void Start()
        {
            base.Start();

            var index = 0;
            while (PlayerPrefs.HasKey("platform_" + index))
            {
                var data = JsonConvert.DeserializeObject<PlatformData>(PlayerPrefs.GetString("platform_" + index));
                switch (data.PlatformType)
                {
                    case "vk":
                    Platforms.Add(new VK(data, index, this));
                    break;
                }

                index++;
            }
        }
        void Update()
        {
            T += Time.deltaTime;
            if (T >= RefreshPeriod)
            {
                T = 0f;

                for (int p = 0; p < Platforms.Count; p++)
                    if (Platforms[p].Enabled)
                        Platforms[p].RefreshChat();
            }

            var process = true;
            while (process)
            {
                process = false;
                for (int p = 0; p < Platforms.Count; p++)
                {
                    if (!Platforms[p].Enabled)
                        continue;

                    var got = Platforms[p].GetMessage(out var message);
                    process |= got;

                    if (got)
                    {
                        // MESSAGE PROCESSING
                        var text = message.Nick + ": ";
                        for (int pt = 0; pt < message.Parts.Count; pt++)
                            text += message.Parts[pt].Text.Content;

                        var go = Instantiate(MessagePrefab, ChatContent, false);
                        var m = go.GetComponent<Message>();
                        m.Init(text);
                    }
                }
            }
        }

        public void CreatePlatform()
        {
            switch (PlatformSwitch.value)
            {
                case 0:
                if (!string.IsNullOrEmpty(PlatformNameInput.text) &&
                     !string.IsNullOrEmpty(PlatformURLInput.text))
                    Platforms.Add(new VK(PlatformNameInput.text, PlatformURLInput.text, Platforms.Count, this));
                break;
            }
        }

        #region VK
        public void StartAuthVK() => VK.StartAuth();
        public void SubmitVKToken()
        {
            if (!VK.SubmitToken(VK_TokenField.text, out var token))
            {
                VK_TokenField.text = "";

                return;
            }

            AuthPanel.gameObject.SetActive(false);
        }
        #endregion
    }
}