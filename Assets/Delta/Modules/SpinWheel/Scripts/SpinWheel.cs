using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using System.Globalization;
using Delta.Core;
using Delta.Common.UI;
using Delta.Services;

namespace Delta.Modules.SpinWheel
{
   public sealed class SpinWheel : MonoBehaviour
   {
      private IAdService _adService;

      [SerializeField] private GameObject wheelPiecePrefab;

      [Header("Sounds :")]
      [SerializeField] private AudioSource audioSource;
      [SerializeField] private AudioClip tickAudioClip;
      [SerializeField] private AudioClip spinEndAudioClip;
      [SerializeField][Range(0f, 1f)] private float volume = .5f;
      [SerializeField][Range(-3f, 3f)] private float pitch = 1f;

      [Space]
      [Header("Spin wheel settings :")]
      [Range(1, 20)] public int spinDuration = 8;
      public Ease spinEase = Ease.InOutQuart;

      [Space]
      [Header("Wheel pieces config :")]
      [Tooltip("Load configuration from a SpinWheelConfig asset")]
      [SerializeField] private SpinWheelConfig config;

      [Tooltip("Or set wheel pieces directly (overrides config if set)")]
      public WheelPiece[] wheelPieces;

      [Header("Daily Spin Limit")]
      [SerializeField] private int maxDailySpins = 5;

      [Header("Ads")]
      [Tooltip("Placement id passed to IAdService.ShowRewardedAd")]
      [SerializeField] private string adPlacementId = "LuckyWheel";

      [Header("Reward Event")]
      [Tooltip("Subscribe to this event to handle rewards when the spin completes")]
      public SpinWheelRewardEvent OnRewardWon = new SpinWheelRewardEvent();

      private const string PrefKeyDailySpins = "SpinWheel_DailySpins";
      private const string PrefKeyLastSpinDate = "SpinWheel_LastSpinDate";
      private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

      private static SpinWheel _instance;
      public static SpinWheel Instance
      {
         get
         {
            if (_instance == null)
            {
               GameObject go = new GameObject("SpinWheel");
               _instance = go.AddComponent<SpinWheel>();
               DontDestroyOnLoad(go);
            }
            return _instance;
         }
      }

      private SpinWheelUI _view;
      public SpinWheelUI View { get { return _view; } }

      private int _dailySpinCount;
      private string _lastSpinDateStr;
      public string LastSpinDateStr
      {
         get { return _lastSpinDateStr; }
         set
         {
            if (_lastSpinDateStr == value) return;
            _lastSpinDateStr = value;
            PlayerPrefs.SetString(PrefKeyLastSpinDate, value);
            PlayerPrefs.Save();
         }
      }

      public int RemainingSpins { get; private set; }

      // Events
      private UnityAction onSpinStartEvent;
      private UnityAction onSpinEndEvent;


      private int _timeRemaining;
      private bool _isSpinning = false;
      private bool _viewEventsWired = false;
      public bool IsSpinning { get { return _isSpinning; } }
      public bool ShowFirstTime = false;


      private Vector2 pieceMinSize = new Vector2(81f, 146f);
      private Vector2 pieceMaxSize = new Vector2(144f, 213f);
      private int piecesMin = 2;
      private int piecesMax = 12;

      private float pieceAngle;
      private float halfPieceAngle;
      private float halfPieceAngleWithPaddings;

      private double accumulatedWeight;
      private System.Random rand = new System.Random();

      private List<int> nonZeroChancesIndices = new List<int>();

      private bool _isReady = false;

      private void Awake()
      {
         if (_instance == null)
         {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _adService = ServiceLocator.Get<IAdService>();
         }
         else if (_instance != this)
         {
            Destroy(gameObject);
         }
      }

      public void Initialize()
      {
         Init();

         // Load from config if available and wheelPieces not manually set
         if (config != null && (wheelPieces == null || wheelPieces.Length == 0))
         {
            LoadFromConfig(config);
         }

         if (wheelPieces == null || wheelPieces.Length == 0)
         {
            Debug.LogError("[SpinWheel] No wheel pieces configured! Assign a SpinWheelConfig or set wheelPieces array.");
            return;
         }

         pieceAngle = 360f / wheelPieces.Length;
         halfPieceAngle = pieceAngle / 2f;
         halfPieceAngleWithPaddings = halfPieceAngle - (halfPieceAngle / 4f);

         Generate();

         CalculateWeightsAndIndices();
         if (nonZeroChancesIndices.Count == 0)
            Debug.LogError("You can't set all pieces chance to zero");


         // Initialize spin counter
         InitializeSpinCounter();

         _isReady = true;
      }

      public void LoadFromConfig(SpinWheelConfig configAsset)
      {
         if (configAsset == null)
         {
            Debug.LogError("[SpinWheel] Cannot load from null config!");
            return;
         }

         config = configAsset;
         wheelPieces = configAsset.GetWheelPieces();

         Debug.Log($"[SpinWheel] Loaded {wheelPieces.Length} pieces from config '{configAsset.name}'");
      }

      public void Show()
      {
         if (_view == null)
         {
            _view = UIManager.Instance.ShowUIOnTop<SpinWheelUI>("SpinWheelUI");

            _isReady = false;
         }
         _view.SpinButton.onClick.RemoveListener(WatchAdToSpin);
         _view.SpinButton.onClick.AddListener(WatchAdToSpin);
         if (!_viewEventsWired)
         {
            onSpinStartEvent += _view.StopIdleLightEffect;
            onSpinStartEvent += _view.PlayLightAnimation;
            onSpinEndEvent += _view.PlayIdleLightEffect;
            onSpinEndEvent += _view.StopLightAnimation;
            _viewEventsWired = true;
         }
         _view.PlayIdleLightEffect();
         if (!_isReady)
         {
            Initialize();
         }
         else
         {
            CheckAndResetDailySpins();
         }

         _view.SetupInfoPanel(wheelPieces);
      }

      // public void HandlePassTime(int time)
      // {
      //    if (time > 10)
      //       return;
      //    _timeRemaining -= time;
      //    TimePassedEvent?.Invoke(_timeRemaining);
      // }

      private void Generate()
      {
         wheelPiecePrefab = InstantiatePiece();

         RectTransform rt = wheelPiecePrefab.transform.GetChild(0).GetComponent<RectTransform>();
         float pieceWidth = Mathf.Lerp(pieceMinSize.x, pieceMaxSize.x, 1f - Mathf.InverseLerp(piecesMin, piecesMax, wheelPieces.Length));
         float pieceHeight = Mathf.Lerp(pieceMinSize.y, pieceMaxSize.y, 1f - Mathf.InverseLerp(piecesMin, piecesMax, wheelPieces.Length));
         rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pieceWidth);
         rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pieceHeight);

         for (int i = 0; i < wheelPieces.Length; i++)
            DrawPiece(i);

         GameObject.Destroy(wheelPiecePrefab);
      }

      private void DrawPiece(int index)
      {
         WheelPiece pieceData = wheelPieces[index];
         SpinWheelPiece piece = InstantiatePiece().GetComponent<SpinWheelPiece>();
         piece.Setup(pieceData, pieceAngle / 2, pieceAngle / 360f);

         // Line
         Transform lineTrns = GameObject.Instantiate(Resources.Load<GameObject>("SpinWheel/Line"), View.LinesParent.position, Quaternion.identity, View.LinesParent).transform;
         lineTrns.RotateAround(View.WheelPiecesParent.position, Vector3.back, (pieceAngle * index) + halfPieceAngle);
         piece.holder.RotateAround(View.WheelPiecesParent.position, Vector3.back, pieceAngle * index);
      }

      private GameObject InstantiatePiece()
      {
         return GameObject.Instantiate(Resources.Load<GameObject>("SpinWheel/Piece"), View.WheelPiecesParent.position, Quaternion.identity, View.WheelPiecesParent);
      }

      public void WatchAdToSpin()
      {
         if (_isSpinning)
            return;

         CheckAndResetDailySpins();

         if (RemainingSpins <= 0)
         {
            return;
         }

         // First free spin of the day
         if (RemainingSpins == maxDailySpins)
         {
            IncrementSpinCount();
            Spin();
            return;
         }

         if (_adService != null && _adService.IsRewardedAdLoaded())
         {
            _adService.ShowRewardedAd(adPlacementId, (success) =>
            {
               if (success)
               {
                  IncrementSpinCount();
                  Spin();
               }
            });
         }
      }


      public void Spin()
      {
         if (!_isSpinning)
         {
            _isSpinning = true;
            if (onSpinStartEvent != null)
               onSpinStartEvent.Invoke();

            int index = GetRandomPieceIndex();
            WheelPiece piece = wheelPieces[index];

            if (piece.Chance == 0 && nonZeroChancesIndices.Count != 0)
            {
               index = nonZeroChancesIndices[UnityEngine.Random.Range(0, nonZeroChancesIndices.Count)];
               piece = wheelPieces[index];
            }

            float angle = -(pieceAngle * index);

            float rightOffset = (angle - halfPieceAngleWithPaddings) % 360;
            float leftOffset = (angle + halfPieceAngleWithPaddings) % 360;

            float minOffset = Mathf.Min(leftOffset, rightOffset);
            float maxOffset = Mathf.Max(leftOffset, rightOffset);
            float randomAngle = UnityEngine.Random.Range(minOffset, maxOffset);

            Vector3 targetRotation = Vector3.back * (randomAngle + 2 * 360 * spinDuration);

            //float prevAngle = wheelCircle.eulerAngles.z + halfPieceAngle ;
            float prevAngle, currentAngle;
            prevAngle = currentAngle = View.WheelCircle.eulerAngles.z;

            bool isIndicatorOnTheLine = false;

            View.WheelCircle
            .DORotate(targetRotation, spinDuration, RotateMode.FastBeyond360)
            .SetEase(spinEase)
            .OnUpdate(() =>
            {
               float diff = Mathf.Abs(Mathf.DeltaAngle(prevAngle, currentAngle));
               if (diff >= halfPieceAngle)
               {
                  prevAngle = currentAngle;
                  isIndicatorOnTheLine = !isIndicatorOnTheLine;
                  if (audioSource != null && tickAudioClip != null)
                  {
                     audioSource.pitch = pitch;
                     audioSource.PlayOneShot(tickAudioClip, volume);
                  }
               }
               currentAngle = View.WheelCircle.eulerAngles.z;
            })
            .OnComplete(() =>
            {
               _isSpinning = false;
               if (onSpinEndEvent != null)
                  onSpinEndEvent.Invoke();

               _view.ShowResultScreen(piece.reward);
               audioSource.PlayOneShot(spinEndAudioClip, volume);

               // Invoke reward event for parent project to handle
               OnRewardWon?.Invoke(piece.reward);

               // TODO: Add tracking event
            });

         }
      }

      public void OnSpinStart(UnityAction action)
      {
         onSpinStartEvent += action;
      }

      public void OnSpinEnd(UnityAction action)
      {
         onSpinEndEvent += action;
      }


      private int GetRandomPieceIndex()
      {
         double r = rand.NextDouble() * accumulatedWeight;

         for (int i = 0; i < wheelPieces.Length; i++)
            if (wheelPieces[i].weight >= r)
               return i;

         return 0;
      }

      private void CalculateWeightsAndIndices()
      {
         for (int i = 0; i < wheelPieces.Length; i++)
         {
            WheelPiece piece = wheelPieces[i];

            //add weights:
            accumulatedWeight += piece.Chance;
            piece.weight = accumulatedWeight;

            //add index :
            piece.Index = i;

            //save non zero chance indices:
            if (piece.Chance > 0)
               nonZeroChancesIndices.Add(i);
         }
      }

      public void Init()
      {
         _timeRemaining = (int)(DateTime.Today.AddDays(1).Subtract(DateTime.Now).TotalSeconds);
      }

      private void Close()
      {
         _isReady = false;
         if (_view != null)
         {
            _view.SpinButton.onClick.RemoveListener(WatchAdToSpin);
            Destroy(_view.gameObject);
            _view = null;
         }

         onSpinStartEvent = null;
         onSpinEndEvent = null;
         _viewEventsWired = false;
      }

      private void InitializeSpinCounter()
      {
         if (PlayerPrefs.HasKey(PrefKeyDailySpins))
         {
            string stored = PlayerPrefs.GetString(PrefKeyDailySpins, string.Empty);
            if (!string.IsNullOrEmpty(stored))
            {
               int parsed;
               _dailySpinCount = int.TryParse(stored, out parsed) ? parsed : 0;
            }
            else
            {
               _dailySpinCount = PlayerPrefs.GetInt(PrefKeyDailySpins, 0);
            }
         }
         else
         {
            _dailySpinCount = 0;
         }
         LastSpinDateStr = PlayerPrefs.HasKey(PrefKeyLastSpinDate) ? PlayerPrefs.GetString(PrefKeyLastSpinDate) : DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture);

         CheckAndResetDailySpins();
      }

      public void CheckAndResetDailySpins()
      {
         if (string.IsNullOrEmpty(LastSpinDateStr))
         {
            // First time initialization 
            LastSpinDateStr = DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture);
            return;
         }

         DateTime lastDate;
         if (!DateTime.TryParseExact(LastSpinDateStr, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out lastDate))
         {
            lastDate = DateTime.Now;
            LastSpinDateStr = lastDate.ToString(DateFormat, CultureInfo.InvariantCulture);
         }
         DateTime now = DateTime.Now;

         // If it's a new day, reset the counter
         if (now.Date > lastDate.Date)
         {
            _dailySpinCount = 0;
            LastSpinDateStr = now.ToString(DateFormat, CultureInfo.InvariantCulture);
            PlayerPrefs.SetInt(PrefKeyDailySpins, _dailySpinCount);
            PlayerPrefs.Save();
         }

         RemainingSpins = Mathf.Max(0, maxDailySpins - _dailySpinCount);

         // Update UI if available
         if (_view != null)
         {
            _view.UpdateRemainingSpinsText(RemainingSpins);
         }
      }

      private void IncrementSpinCount()
      {
         _dailySpinCount++;
         RemainingSpins = Mathf.Max(0, maxDailySpins - _dailySpinCount);
         PlayerPrefs.SetInt(PrefKeyDailySpins, _dailySpinCount);
         PlayerPrefs.Save();

         // Update UI
         _view.UpdateRemainingSpinsText(RemainingSpins);
      }

      public void ResetDailySpins()
      {
         _dailySpinCount = 0;
         LastSpinDateStr = DateTime.Now.ToString(DateFormat, CultureInfo.InvariantCulture);
         PlayerPrefs.SetInt(PrefKeyDailySpins, _dailySpinCount);
         PlayerPrefs.Save();
      }

      public bool IsDailySpinLimitReached()
      {
         return _dailySpinCount >= maxDailySpins;
      }

      [ContextMenu("Open Spin Wheel")]
      public void OpenSpinWheel()
      {
         Show();
      }
   }
}