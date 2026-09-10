using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Delta.Modules.MiniGame
{
    public class DragSlot : MonoBehaviour,IDropHandler
    {
        [SerializeField] private Image _avatar;


        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag)
            {
                if (eventData.pointerDrag.GetComponent<DragObject>().CheckComplete())
                {
                    //Todo: Play sound CorrectMove here

                    eventData.pointerDrag.transform.SetParent(this.transform);
                    eventData.pointerDrag.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                    eventData.pointerDrag.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                    eventData.pointerDrag.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                    eventData.pointerDrag.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
                }
                else
                {
                    //Todo: Play sound IncorrectMove here
                    eventData.pointerDrag.GetComponent<DragObject>().ResetToOriginalRectState();
                }
            }
        }
        
        public void LoadAvatar( string id)
        {
          //Todo: Find way to set image here
            //_avatar.SetAvatar(id);
        }
    }
}