using Delta.Core;
using Delta.GameOps;

namespace Delta.ProjectName
{
    public class GameState_Init : GameState
    {
        protected override void OnEnter()
        {
            if (DeltaApp.Instance.IsFirebaseReady)
                Initialize();
            else
                DeltaApp.Instance.evtFirebaseInited += Initialize;
        }

        private void Initialize()
        {
            DeltaApp.Instance.evtFirebaseInited -= Initialize;
            Machine.ChangeState<GameState_MainMenu>();
        }
    }
}
