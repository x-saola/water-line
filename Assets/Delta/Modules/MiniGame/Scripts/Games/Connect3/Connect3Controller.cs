using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Delta.Common.UI;
using Delta.GameOps;
using Delta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Delta.Modules.MiniGame
{

    public class Connect3Controller : BaseMiniGameController
    {
        public enum Connect3GameState
        {
            Setup,
            Playing,
            Over,
            Win
        }

        [SerializeField] public List<ConnectObject> ConnecterController = new List<ConnectObject>();
        public List<ConnectObject> ChosenConnecterController = new List<ConnectObject>();
        public List<GameObject> ConnectLines = new List<GameObject>();

        [SerializeField] Connect3UI _connect3UI;

        [Space] Connect3GameState _gameState = Connect3GameState.Setup;
        private float _remainTime;
        private int _maxScore;
        private int _score;
        private string _levelId;

        [Space] public bool isSelecting = false;
        private int _selectedColor = -1;

        [Space] [SerializeField] private GameObject _connectControllerPrefab;
        [SerializeField] private GameObject _connectLinelerPrefab;

        [Space] [SerializeField] private Transform _connectObjectsParent;
        [SerializeField] private Transform _linesParent;

        [Space] private Vector2Int _boardSize = new Vector2Int(0, 0);

        private float selectCd = 0.25f;
        private Dictionary<int, int> colorToItem = new Dictionary<int, int>();

        private const string _KEY_DONE_TUTORIAL = "KEY_DONE_MINI_CONNECT_3";

        public override void StartGame()
        {

            _remainTime = 90;
            _boardSize = new Vector2Int(5, 7);
            _maxScore = _boardSize.x * _boardSize.y;
            _score = 0;

            _connect3UI = UIManager.Instance.ShowUIOnTop<Connect3UI>("Connect3MiniGameUI");

            _linesParent = _connect3UI.linesParent;
            _connectObjectsParent = _connect3UI.ConnectObjectsParent;

            List<int> colors = GenerateColorList(_boardSize.x * _boardSize.y);
            int colorIndex = 0;
            for (int x = 0; x < _boardSize.x; x++)
            {
                for (int y = 0; y < _boardSize.y; y++)
                {
                    var pos = 1.5f * (new Vector2(x, y) - new Vector2(_boardSize.x / 2, _boardSize.y / 2));
                    int color = colors[colorIndex];
                    colorIndex++;
                    GameObject connect =
                        Instantiate(Resources.Load<GameObject>("ConnectObjectPrefab"),
                            _connectObjectsParent) as GameObject;
                    connect.GetComponent<RectTransform>().position = pos;
                    ConnecterController.Add(connect.GetComponent<ConnectObject>());
                    connect.GetComponent<ConnectObject>().SetUp(new Vector2Int(x, y), color, colorToItem[color]);
                }
            }

            foreach (var connectObject in ConnecterController)
            {
                connectObject.OnSelected = AddConnect;
                connectObject.OnClickUp = OnPointerUp;
                connectObject.OnClickDown = OnPointerDown;
            }

            var doneTutorial = PlayerPrefs.GetInt(_KEY_DONE_TUTORIAL, 0) == 1;
            if (doneTutorial)
            {
                _gameState = Connect3GameState.Playing;
            }
            else
            {
                var tutorialUI = UIManager.Instance.ShowUIOnTop<MiniGameTutorialUI>("MiniGameTutorialUI");
                tutorialUI.SetTutorialText("Link three or more matching colors to collect everything on the board.");
                tutorialUI.OnClickConfirmButton += () =>
                {
                    PlayerPrefs.SetInt(_KEY_DONE_TUTORIAL, 1);

                    _gameState = Connect3GameState.Playing;
                    UIManager.Instance.ReleaseUI(tutorialUI, true);
                };
            }
        }

        public void SpawnLine(ConnectObject connectObject, ConnectObject newConnectObject)
        {
            GameObject line = Instantiate(Resources.Load<GameObject>("ConnectLine"), _linesParent) as GameObject;
            line.GetComponent<RectTransform>().position = connectObject.GetComponent<RectTransform>().position;
            line.GetComponent<RectTransform>().rotation =
                Quaternion.Euler(new Vector3(0, 0, GetAngle(newConnectObject.Index - connectObject.Index)));
            ConnectLines.Add(line);
        }

        private void OnPointerUp()
        {
            if (_gameState != Connect3GameState.Playing) return;

            isSelecting = false;
            var scaleDowntime = 0.15f;
            var delayDropTime = 0.15f;
            selectCd = scaleDowntime + delayDropTime + 0.05f;

            var isConnected = ChosenConnecterController.Count >= 3;
            if (isConnected)
            {
                _score += ChosenConnecterController.Count;
                _connect3UI.SetScoreText(_score, _maxScore);
            }

            foreach (var connectObject in ChosenConnecterController)
            {
                connectObject.Highlight(false);
                if (isConnected)
                {
                    connectObject.ScaleDown(scaleDowntime);
                }
            }

            ChosenConnecterController.Clear();
            foreach (GameObject obj in ConnectLines)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            ConnectLines.Clear();
            _selectedColor = -1;
            if (isConnected)
            {
                if (_score >= _maxScore)
                {
                    _gameState = Connect3GameState.Win;
                    HandleGameOver(true);
                }
                else
                {
                    StartCoroutine(Delay(scaleDowntime + delayDropTime, DropEmptyTile));
                }
            }
        }

        public void OnPointerDown(ConnectObject co)
        {
            if (_gameState != Connect3GameState.Playing) return;
            isSelecting = true;
            AddConnect(co);
        }

        public bool CheckNearByIndex(Vector2Int index)
        {
            var lastTile = ChosenConnecterController.Last();
            return (index - lastTile.Index).sqrMagnitude <= 2;
        }

        public void FixedUpdate()
        {
            if (selectCd > 0 && !isSelecting)
            {
                selectCd = Mathf.Max(0, selectCd - Time.deltaTime);
            }
        }

        public void AddConnect(ConnectObject co)
        {
            if (!isSelecting || _gameState != Connect3GameState.Playing) return;

            if (ChosenConnecterController.Count == 0)
            {
                if (selectCd > 0) return;

                _selectedColor = co.Color;
                ChosenConnecterController.Add(co);
                co.Highlight();
                return;
            }


            if (ChosenConnecterController.Contains(co))
            {
                if (ChosenConnecterController.Count >= 2 && ChosenConnecterController[^2] == co)
                {
                    ChosenConnecterController.Last().Highlight(false);
                    ChosenConnecterController.RemoveAt(ChosenConnecterController.Count - 1);

                    Destroy(ConnectLines.Last());
                    ConnectLines.Remove(ConnectLines.Last());
                }

                return;
            }
            else
            {
                var lastTile = ChosenConnecterController.Last();
                if (_selectedColor == co.Color && (co.Index - lastTile.Index).sqrMagnitude <= 2)
                {
                    SpawnLine(lastTile, co);
                    ChosenConnecterController.Add(co);
                    co.Highlight();
                }
            }
        }

        List<int> GenerateColorList(int totalCount)
        {

            List<int> colorList = new List<int>(); //8 color

            // Get 4 unique random numbers from 0 to 8
            System.Random random = new System.Random();
            List<int> numbers = Enumerable.Range(0, 9).OrderBy(x => random.Next()).Take(4).ToList();

            // Get 4 unique random numbers from 0 to 11
            List<int> items = Enumerable.Range(0, 11).OrderBy(x => random.Next()).Take(4).ToList();
            for (int i = 0; i < numbers.Count; i++)
            {
                colorToItem[numbers[i]] = items[i];
            }

            // Add 4 of each selected number to colorList
            foreach (int num in numbers)
            {
                colorList.AddRange(Enumerable.Repeat(num, 4));
            }

            // Fill remaining slots with random colors (if needed)
            int remaining = totalCount - colorList.Count;
            for (int i = 0; i < remaining; i++)
            {
                colorList.Add(numbers[Random.Range(0, 4)]);
            }

            // Shuffle the list to ensure randomness
            colorList = colorList.OrderBy(_ => Random.value).ToList();

            return colorList;
        }

        float GetAngle(Vector2 vector)
        {
            return -Mathf.Atan2(vector.x, vector.y) * Mathf.Rad2Deg;
        }

        public void DropEmptyTile()
        {
            //Drop down
            for (int x = 0; x < _boardSize.x; x++)
            {
                for (int y = 0; y < _boardSize.y - 1; y++)
                {
                    var tile = ConnecterController.First(t => t.Index == new Vector2Int(x, y));
                    if (tile.Type == -1)
                    {
                        var findY = y + 1;
                        var findX = x;
                        while (findY < _boardSize.y)
                        {
                            var findTile = ConnecterController.First(t => t.Index == new Vector2Int(findX, findY));

                            if (findTile.Type == -1)
                            {
                                findY++;
                            }
                            else
                            {
                                tile.SetData(findTile.Type, findTile.Color, findTile.Item);
                                findTile.Reset();
                                break;
                            }
                        }
                    }
                }
            }

            //Drop left
            for (int x = 0; x < _boardSize.x - 1; x++)
            {
                var checkTile = ConnecterController.First(t => t.Index == new Vector2Int(x, 0));
                if (checkTile.Type == -1)
                {
                    var findX = x + 1;
                    while (findX < _boardSize.x)
                    {
                        var checkFindTile = ConnecterController.First(t => t.Index == new Vector2Int(findX, 0));
                        if (checkFindTile.Type == -1)
                        {
                            findX++;
                        }
                        else
                        {
                            //move all column
                            for (var findY = 0; findY < _boardSize.y; findY++)
                            {
                                var findTile = ConnecterController.First(t => t.Index == new Vector2Int(findX, findY));
                                if (findTile.Type == -1) break;
                                var tile = ConnecterController.First(t => t.Index == new Vector2Int(x, findY));
                                tile.SetData(findTile.Type, findTile.Color, colorToItem[findTile.Color]);
                                findTile.Reset();
                            }

                            break;
                        }
                    }
                }
            }

            CheckGroupConnected(3);
        }

        bool CheckGroupConnected(int minRequire)
        {
            Debug.Log("CHECK");
            bool[,] visited = new bool[_boardSize.y, _boardSize.x];

            for (int y = 0; y < _boardSize.y; y++)
            {
                for (int x = 0; x < _boardSize.x; x++)
                {
                    if (!visited[y, x])
                    {
                        int count = DepthFirstSearch(visited, y, x,
                            ConnecterController.First(co => co.Index == new Vector2Int(x, y)).Color);
                        if (count >= minRequire)
                        {
                            Debug.Log("CAN CONNECT");
                            return true;
                        }
                    }
                }
            }

            Debug.Log("CAN NOT CONNECT");

            _gameState = Connect3GameState.Over;
            StartCoroutine(Delay(1f, () =>
            {
                HandleGameOver(false);
                ;
            }));

            return false;
        }

        IEnumerator Delay(float seconds, Action callback)
        {
            yield return new WaitForSeconds(seconds);
            callback?.Invoke();
        }

        int DepthFirstSearch(bool[,] visited, int y, int x, int targetColor)
        {
            if (y < 0 || x < 0 || y >= _boardSize.y || x >= _boardSize.x
                || visited[y, x]
                || ConnecterController.First(co => co.Index == new Vector2Int(x, y)).Color != targetColor
                || targetColor == -1)
            {
                return 0;
            }

            visited[y, x] = true;
            int count = 1;

            //(Up, Down, Left, Right, 4 diagonals)
            int[] dr = { -1, 1, 0, 0, -1, -1, 1, 1 };
            int[] dc = { 0, 0, -1, 1, -1, 1, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                count += DepthFirstSearch(visited, y + dr[i], x + dc[i], targetColor);
            }

            return count;
        }

        private void Update()
        {
            if (_gameState == Connect3GameState.Playing)
            {
                _remainTime -= Time.deltaTime;

                _connect3UI.SetTimerText(Mathf.CeilToInt(_remainTime));

                if (_remainTime <= 0)
                {
                    _gameState = Connect3GameState.Over;
                    HandleGameOver(false);
                }
            }
        }

        private void HandleGameOver(bool isWin)
        {
            _connect3UI.SetTimerText(0);
            CalculateRewardCoin();
            ShowGameOverUI();
        }

        private void CalculateRewardCoin()
        {
            var maxCoinReward = 10;
            var maxScore = _maxScore;
            Debug.Log(_score);
            Debug.Log(_maxScore);
            Debug.Log(maxCoinReward);
            Debug.Log((float)_score / maxScore);

            _rewardCoin = Mathf.CeilToInt((float)_score / maxScore * maxCoinReward);
        }

        public override void StopGame()
        {
            UIManager.Instance.ReleaseUI(_connect3UI, true);
            UIManager.Instance.ReleaseUI(_winMiniGameUI, true);

        }
    }
}