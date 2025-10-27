using System.Collections.Generic;

using Core.UI;

using TMPro;

using UnityEngine;

namespace MultiChat
{
    public class MultiChatManager : UIManagerBase
    {
        [SerializeField] float RefreshPeriod = 2f;

        [Header("UI for Manual Token")]
        [SerializeField] Transform TokenInputPanel;
        [SerializeField] TMP_InputField VK_ChannelField;
        [SerializeField] TMP_InputField VK_TokenField;

        float T;
        List<Platform> Platforms = new List<Platform>();

        protected override void Start()
        {
            base.Start();

            if (PlayerPrefs.HasKey("vk_channel"))
            {
                VK_ChannelField.text = PlayerPrefs.GetString("vk_channel");

                SubmitVKToken(PlayerPrefs.GetString("vk_token"));
            }
        }

        void Update()
        {
            T += Time.deltaTime;
            if (T >= RefreshPeriod)
            {
                T = 0f;

                for (int p = 0; p < Platforms.Count; p++)
                    Platforms[p].RefreshChat(this);
            }

            var process = true;
            while (process)
            {
                process = false;
                for (int p = 0; p < Platforms.Count; p++)
                {
                    var gm = Platforms[p].GetMessage(out var message);
                    process |= gm;

                    if (gm)
                    {
                        Debug.Log(message.Parts.Count);
                        // UI MESSAGE PROCESSING
                    }
                }
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

            if (!string.IsNullOrEmpty(VK_ChannelField.text))
                SubmitVKToken(token);

            TokenInputPanel.gameObject.SetActive(false);
        }

        void SubmitVKToken(string token)
        {
            Platforms.Add(new VK(VK_ChannelField.text));

            Debug.Log("VK added");
        }
        #endregion
    }
}