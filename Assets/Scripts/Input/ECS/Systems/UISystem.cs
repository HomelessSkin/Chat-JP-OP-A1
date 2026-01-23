using Core;

using Input;

using Unity.Entities;

namespace MultiChat
{
    [UpdateInGroup(typeof(InputSystemGroup))]
    [UpdateAfter(typeof(CollectSystem))]
    public partial class UISystem : HandleSystem
    {
        protected override void OnCreate()
        {
            base.OnCreate();

            Group = "UI";
        }
    }
}