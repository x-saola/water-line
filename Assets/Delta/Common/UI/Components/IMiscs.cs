namespace Delta.Common
{
    public interface ILayerHandler
    {
        void ChangeLayer(string layer, int sortingOrder);
        void ChangeSortingOrder(int sortingOrder);
    }

    public interface IRotatable
    {
        void Rotate();
        void Rotate(System.Action callback);
    }

    public interface IShowHandler
    {
        System.Action onShowStarted { get; }
        System.Action onShowFinished { get; }
        void DoShow();
    }

    public interface IHideHandler
    {
        System.Action onHideStarted { get; }
        System.Action onHideFinished { get; }
        void DoHide();
    }

    public interface IFadeHandler
    {
        System.Action onFadeOutStarted { get; }
        System.Action onFadeOutFinished { get; }

        System.Action onFadeInStarted { get; }
        System.Action onFadeInFinished { get; }

        void DoFadeOut();
        void DoFadeIn();
    }
}