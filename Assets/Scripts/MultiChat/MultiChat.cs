using Core;

using UI;

using UnityEngine;

namespace MultiChat
{
    public class MultiChatManager : UIManagerBase
    {
        #region CANVASSER
        void RenderMainCanvas() => _Canvasser.QueueRender();
        void RenderOBSCanvas() => _Canvasser.QueueRender(1);
        #endregion

        //#region DRAWER
        //protected override void RedrawTheme(IStorage.Data storage)
        //{
        //    base.RedrawTheme(storage);

        //    for (int m = 0; m < _Chat.Messages.Count; m++)
        //    {
        //        var message = _Chat.Messages[m];
        //        var drawables = message.GetComponentsInChildren<Drawable>();
        //        for (int d = 0; d < drawables.Length; d++)
        //        {
        //            var drawable = drawables[d];
        //            if (!(drawable as IRedrawable).IsRedrawable())
        //                continue;
        //            if (TryGetDrawerData(drawable.GetKey(), out var sprite))
        //                drawable.SetData(sprite);
        //        }
        //    }

        //    for (int m = 0; m < _Chat.Pool.Count; m++)
        //    {
        //        var message = _Chat.Pool[m];
        //        var drawables = message.GetComponentsInChildren<Drawable>();
        //        for (int d = 0; d < drawables.Length; d++)
        //        {
        //            var drawable = drawables[d];
        //            if (!(drawable as IRedrawable).IsRedrawable())
        //                continue;
        //            if (TryGetDrawerData(drawable.GetKey(), out var sprite))
        //                drawable.SetData(sprite);
        //        }
        //    }
        //}
        //#endregion

        [Space]
        [SerializeField] StreamingSpritesData Smiles;

        protected override void Awake()
        {
            base.Awake();

            StreamingSprites.Prepare(Smiles);
            Log.AddReadListener(RenderMainCanvas);
        }
    }
}