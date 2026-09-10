using System.Threading.Tasks;
using Delta.Common.UI;
using Delta.Core;
using Delta.GameOps;
using Delta.Modules;
using Delta.Services;
using UnityEngine;
using UnityEngine.Networking;

namespace Delta.ProjectName
{
    public class GameState_Init : GameState
    {
        private const string InternetCheckUrl = "https://connectivitycheck.gstatic.com/generate_204";
        private const int InternetCheckTimeoutSeconds = 2;

        private IUserDataService _userDataService;
        private IAdService _adService;
        private bool _firebaseReady;
        private bool _userDataReady;

        private LoadingUI _loadingUI;
        private bool _checkingInternet;

        protected override void OnEnter()
        {
            _ = EnterAsync();
        }

        private async Task EnterAsync()
        {
            _loadingUI = UIManager.Instance.ShowUIOnTop<LoadingUI>("LoadingUI", 4);
            _ = _loadingUI.PlayAppearAnimation();

            if (await HasInternetAsync())
                BeginInitialize();
            else
                _loadingUI.ScheduleNoInternetPopup(OnRetryClicked);
        }

        private void OnRetryClicked()
        {
            if (_checkingInternet)
                return;

            _ = RetryCheckAsync();
        }

        private async Task RetryCheckAsync()
        {
            _checkingInternet = true;
            var hasInternet = await HasInternetAsync();
            _checkingInternet = false;

            if (hasInternet)
            {
                _loadingUI.HideNoInternetPopup();
                _loadingUI.ResumeProgressFill();
                BeginInitialize();
            }
            else
            {
                _loadingUI.ShowNoInternetPopup(OnRetryClicked);
            }
        }

        private void BeginInitialize()
        {
            _adService = ServiceLocator.Get<IAdService>();
            _userDataService = ServiceLocator.Get<IUserDataService>();
            _userDataService.OnDataLoaded += OnUserDataLoaded;
            _userDataService.Load();

            if (DeltaApp.Instance.IsFirebaseReady)
                OnFirebaseReady();
            else
                DeltaApp.Instance.evtFirebaseInited += OnFirebaseReady;
        }

        private void OnFirebaseReady()
        {
            DeltaApp.Instance.evtFirebaseInited -= OnFirebaseReady;
            _firebaseReady = true;
            _adService.RequestAd();
            _adService.DeactiveBannerAd();

            TryEnterMainMenu();
        }

        private void OnUserDataLoaded()
        {
            _userDataService.OnDataLoaded -= OnUserDataLoaded;
            _userDataReady = true;
            TryEnterMainMenu();
        }

        private void TryEnterMainMenu()
        {
            if (_firebaseReady && _userDataReady)
                Machine.ChangeState<GameState_MainMenu>();
        }

        private static async Task<bool> HasInternetAsync()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
                return false;

            using var request = UnityWebRequest.Get(InternetCheckUrl);
            request.timeout = InternetCheckTimeoutSeconds;
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            return request.result == UnityWebRequest.Result.Success;
        }
    }
}
