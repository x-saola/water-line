using UnityEngine.EventSystems;

namespace Delta.Modules.MiniGame
{
    public interface IGameButton : IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        event System.Action OnClick;
        void AddListener(System.Action listener);
        void RemoveListener(System.Action listener);
    }
}