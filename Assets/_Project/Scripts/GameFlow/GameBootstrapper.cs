using Delta.Core;
using Delta.Common.UI;
using Delta.GameOps;
using UnityEngine;
using Delta.Services;

namespace Delta.ProjectName
{
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Manager Prefabs")]
        [SerializeField] private UIManager _UIManagerPrefab;

        [Header("Config")]
        [SerializeField] private AudioConfigSO _audioConfig;
        // [SerializeField] private AdConfigSO _adConfig;

        private IGameStateMachine _gameStateMachine;
        private EventBinding<GameStateMachineEvent> _gameStateMachineEventBinding;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Application.runInBackground = false;
            Input.multiTouchEnabled = false;

            var stateMachine = new GameStateMachine();
            ServiceLocator.Register<IGameStateMachine>(stateMachine);
            _gameStateMachine = stateMachine;

            _gameStateMachineEventBinding = new EventBinding<GameStateMachineEvent>(OnGameStateMachineEvent);
            EventBus<GameStateMachineEvent>.Register(_gameStateMachineEventBinding);

            Debug.Log("[GameBootstrapper] Awake");
        }

        private void Start()
        {
            InitializeDeltaApp();
            InitializeServices();
            InitializeManagers();
            InitializeGameState();
        }

        private void Update()
        {
            _gameStateMachine?.Update();
        }

        private void OnDestroy()
        {
            EventBus<GameStateMachineEvent>.Deregister(_gameStateMachineEventBinding);
        }

        private void OnGameStateMachineEvent(GameStateMachineEvent e)
        {
            switch (e.EventType)
            {
                case GameStateMachineEventType.Play:
                    _gameStateMachine.ChangeState<GameState_Play>();
                    break;
            }
        }

        private void InitializeDeltaApp()
        {
            var deltaAppGO = new GameObject("DeltaApp", typeof(DeltaApp));
            var deltaApp = deltaAppGO.GetComponent<DeltaApp>();
            deltaApp.Initialize();
            DontDestroyOnLoad(deltaAppGO);
        }

        private void InitializeServices()
        {
            ServiceLocator.Register<IPlayFabService>(new PlayFabService());
            ServiceLocator.Register<IVibrationService>(new VibrationService());
            ServiceLocator.Register<IAudioService>(new AudioService(_audioConfig));
            ServiceLocator.Register<IAdService>(new AdService());
            ServiceLocator.Register<ITrackingService>(new TrackingService());
            ServiceLocator.Register<IUserDataService>(new UserDataService<UserData>());
            ServiceLocator.Register<IAPService>(new IAPService());
        }

        private void InitializeManagers()
        {
            Instantiate(_UIManagerPrefab);
        }

        private void InitializeGameState()
        {
            _gameStateMachine.ChangeState<GameState_Init>();
        }
    }
}
