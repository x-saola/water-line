using UnityEngine;

namespace Delta.Common.UI
{
    public class UIGraphicsRaycastClipRect : UIGraphicsRaycastEmpty
    {
        [SerializeField]
        private RectTransform m_clipRect = null;

        public override bool Raycast(Vector2 sp, Camera eventCamera)
        {
            bool ret = base.Raycast(sp, eventCamera);
            if (ret && m_clipRect != null)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(m_clipRect, sp, eventCamera, out local);
                ret = !m_clipRect.rect.Contains(local);
            }
            return ret;
        }
    }
}

