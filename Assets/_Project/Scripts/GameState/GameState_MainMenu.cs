using Delta.Core;
using Delta.Services;
using UnityEngine.SceneManagement;

namespace Delta.ProjectName
{
    public class GameState_MainMenu : GameState
    {
        protected override void OnEnter()
        {
            ServiceLocator.Get<IAdService>().SetBannerPlacement("MainMenu");
            SceneManager.LoadSceneAsync("MainMenu").completed += async _ =>
            {
                Machine.ChangeState<GameState_Play>();
            };
        }
    }
}
