using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Delta.Common.UI
{
    [System.Serializable]
    public class EndSliderDragEventHandler : UnityEvent<float> { }

    [RequireComponent(typeof(Slider))]
    public class SliderDrag : MonoBehaviour, IPointerUpHandler
    {
        public EndSliderDragEventHandler EndDrag;

        private float SliderValue
        {
            get
            {
                return gameObject.GetComponent<Slider>().value;
            }
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (EndDrag != null)
            {
                EndDrag.Invoke(SliderValue);
            }
        }
    }
}

