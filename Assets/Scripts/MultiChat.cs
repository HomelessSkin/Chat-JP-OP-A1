using System.Collections.Generic;

using Core.Util;

using Newtonsoft.Json;

using TMPro;

using UI;

using UnityEngine;

namespace MultiChat
{
    internal class MultiChatManager : UIManagerBase
    {
        public List<Texture2D> Textures = new List<Texture2D>();

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

        [Space]
        [Header("Smiles")]
        [SerializeField] int DefaultSpritesCount;
        [SerializeField] int SpriteWidth;
        [SerializeField] Texture2D SmileTexture;
        [SerializeField] TMP_SpriteAsset SmileAsset;

        bool[] TextureMap;
        Dictionary<int, (int, List<GameObject>)> Smiles = new Dictionary<int, (int, List<GameObject>)>();

        protected override void Start()
        {
            base.Start();

            TextureMap = new bool[SpriteWidth * SpriteWidth];

            LoadPlatforms();

            void LoadPlatforms()
            {
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
                        {
                            var part = message.Parts[pt];
                            if (!string.IsNullOrEmpty(part.Text.Content))
                                text += part.Text.Content;
                            if (part.Smile.Hash != 0)
                            {
                                var id = Smiles[part.Smile.Hash].Item1;
                                text += $" <sprite name=\"Smiles_{id}\"> ";
                            }
                        }

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
        internal void DrawSmile(Texture2D smile, int hash)
        {
            var id = -1;
            for (int t = DefaultSpritesCount + 1; t < TextureMap.Length; t++)
                if (!TextureMap[t])
                {
                    TextureMap[t] = true;

                    id = t;

                    break;
                }

            if (id < 0)
                id = Random.Range(DefaultSpritesCount + 1, SpriteWidth * SpriteWidth);

            Smiles[hash] = (id, new List<GameObject>());

            smile = DataUtil.ResizeBilinear(smile, 64, 64);
            Textures.Add(smile);

            var x = 64 * (id % SpriteWidth);
            var y = SmileTexture.height - 64 * (id / SpriteWidth + 1);
            SmileTexture.SetPixels32(x, y, 64, 64, smile.GetPixels32());
            SmileTexture.Apply();
            SmileAsset.UpdateLookupTables();
        }
        internal bool HasSmile(int hash, out int id)
        {
            if (Smiles.TryGetValue(hash, out var value))
                id = value.Item1;
            else
                id = -1;

            return id >= 0;
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