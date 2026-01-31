using Input;

using Unity.Entities;

using UnityEngine;

namespace MultiChat
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public partial class EndSystem : EndEventSystem
    {
        MultiChatManager Manager;

        protected override void GetRef()
        {
            if (!Manager)
            {
                var go = GameObject.FindGameObjectWithTag("UIManager");
                if (go)
                    Manager = go.GetComponent<MultiChatManager>();
            }
        }
    }
}