using TMPro;

using UnityEngine;

namespace MultiChat
{
    internal class Message : MonoBehaviour
    {
        [SerializeField] TMP_Text Content;

        internal void Init(string text)
        {
            Content.text = text;
        }
    }
}