using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Delta.Modules.MiniGame
{
    public class DragBorder : MonoBehaviour,IDropHandler,IPointerEnterHandler 
    {
        [SerializeField] int type = 0;
        private IPointerEnterHandler _pointerEnterHandlerImplementation;
        private const float boundLimint = 50; 

        public void OnDrop(PointerEventData eventData)
        {
            // if (eventData.pointerDrag)
            // {
            //     eventData.pointerDrag.GetComponent<DragObject>().ResetToOriginalRectState();
            // }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerDrag) // Check if something is being dragged
            {
                eventData.pointerDrag.GetComponent<DragObject>().isStopDragging = true;
                //eventData.pointerDrag.GetComponent<DragObject>().ResetToOriginalRectState();
                switch (type)
                {
                    case 0:
                        eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition =
                            eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition + new Vector2(0, -boundLimint*eventData.pointerDrag.GetComponent<RectTransform>().localScale.y);
                        break; 
                    case 1:
                        eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition =
                            eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition + new Vector2(0, boundLimint*eventData.pointerDrag.GetComponent<RectTransform>().localScale.y);
                        break; 
                    case 2:
                        eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition =
                            eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition + new Vector2(boundLimint*eventData.pointerDrag.GetComponent<RectTransform>().localScale.x, 0);
                        break; 
                    case 3:
                        eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition =
                            eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition + new Vector2(-boundLimint*eventData.pointerDrag.GetComponent<RectTransform>().localScale.x, -0);
                        break; 
                }
            }
        }
    }
}