using UnityEngine;

namespace Delta.Common.UI
{
    public class UIGraphicsCircle : UIGraphicsRaycastEmpty, ICanvasRaycastFilter
    {
        public float Radius;
        
        private RectTransform _rectTransform;

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
        }

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rectTransform, sp, eventCamera, out var pivotToCursorVector);
            Vector2 pivotOffsetRatio = _rectTransform.pivot - new Vector2(0.5f, 0.5f);
            Vector2 pivotOffset = Vector2.Scale(_rectTransform.rect.size, pivotOffsetRatio);
            Vector2 centerToCursorVector = pivotToCursorVector + pivotOffset;

            return centerToCursorVector.magnitude < Radius;
        }
    }
}
