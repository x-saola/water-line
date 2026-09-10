using UnityEngine;

namespace Delta.ProjectName
{
    public class CheatManager : MonoBehaviour
    {
        #if ENABLE_CHEAT
        
        private static CheatManager _instance;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (_instance == null)
            {
                var go = new GameObject("[CheatManager]");
                _instance = go.AddComponent<CheatManager>();
                DontDestroyOnLoad(go);
            }
        }

        private bool _showDebug;

        private const float FLIP_THRESHOLD = 5f;
        private int _flipCount = 0;
        private bool _isFaceDown = false;
        private float _timeSinceFirstFlip = 0f;

        private const float INPUT_THRESHOLD = 1f;
        private string _cheatInputBuffer = "";
        private float _timeSinceLastInput = 0f;

        private const float TAP_THRESHOLD = 0.5f;
        private int _tapCount = 0;
        private float _timeSinceLastTap = 0f;

        // Ads
        private bool _skipAds;

        // Properties
        public bool SkipAds => _skipAds;

        private void Update()
        {
            HandleDebugInput();
            HandleCheatCodeInput();
        }

        private void HandleDebugInput()
        {
            HandleFlipDebugToggle();
            HandleKeyboardDebugToggle();
            HandleTapToggle();
        }

        private void HandleFlipDebugToggle()
        {
            float z = Input.acceleration.z;

            if (!_isFaceDown && z < -0.5f)
            {
                _isFaceDown = true;
            }
            else if (_isFaceDown && z > 0.5f)
            {
                _isFaceDown = false;
                _flipCount++;
                _timeSinceFirstFlip = 0f;
                Debug.Log("Flip count: " + _flipCount);

                if (_flipCount >= 3)
                {
                    _showDebug = !_showDebug;
                    _flipCount = 0;
                }
            }

            if (_flipCount > 0)
            {
                _timeSinceFirstFlip += Time.deltaTime;
                if (_timeSinceFirstFlip > FLIP_THRESHOLD)
                {
                    _flipCount = 0;
                    _timeSinceFirstFlip = 0f;
                }
            }
        }

        private void HandleKeyboardDebugToggle()
        {
            if (Input.GetKeyDown(KeyCode.D))
            {
                _showDebug = !_showDebug;
            }
        }

        private void HandleTapToggle()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Ended)
                {
                    float currentTime = Time.time;
                    if (currentTime - _timeSinceLastTap < TAP_THRESHOLD)
                    {
                        _tapCount++;
                    }
                    else
                    {
                        _tapCount = 1;
                    }
                    _timeSinceLastTap = currentTime;

                    if (_tapCount >= 3)
                    {
                        _showDebug = !_showDebug;
                        _tapCount = 0;
                    }
                }
            }
        }

        private void HandleCheatCodeInput()
        {
            // Check for cheat code input timeout
            if (_cheatInputBuffer.Length > 0)
            {
                _timeSinceLastInput += Time.deltaTime;

                if (_timeSinceLastInput > INPUT_THRESHOLD)
                {
                    int.TryParse(_cheatInputBuffer, out int level);
                    if (level > 0)
                    {
                        // CheatLevel(level);
                    }

                    _cheatInputBuffer = "";
                }
            }

            if (_showDebug)
            {
                return;
            }

            if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
            {
                _timeSinceLastInput = 0f;

                _cheatInputBuffer += Input.inputString.ToUpper();

                // Keep the buffer from getting too long to save memory
                if (_cheatInputBuffer.Length > 20)
                {
                    _cheatInputBuffer = _cheatInputBuffer.Substring(_cheatInputBuffer.Length - 20);
                }

                // if (_cheatInputBuffer.EndsWith(BigBangCheatCode))
                // {
                //     _cheatInputBuffer = "";
                // }
            }
        }

        private void OnGUI()
        {
            if (!_showDebug) return;

            float scale = 2f;
            float defaultBoxHeight = 40f;
            float spaceBoxHeight = 10f;
            float lineHeight = 30f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
        
            float y = 0;

            // y += spaceBoxHeight;

            y += defaultBoxHeight;
            GUI.Box(new Rect(210 + 30, y, 220, defaultBoxHeight + lineHeight * 1), "Ad Settings");

            y += lineHeight;
            _skipAds = GUI.Toggle(new Rect(220 + 30, y, 200, 25), _skipAds, " Skip Ads");

            GUI.matrix = Matrix4x4.identity;
        }
        
        #endif
    }
}
