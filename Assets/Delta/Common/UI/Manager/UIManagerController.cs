using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace Delta.Common.UI
{
    public class ActiveControllerData
    {
        public UIController Controller;
        public CanvasGroup HiddenCanvas;
        public GraphicRaycaster Raycaster;

        public void ActiveUI()
        {
            if (HiddenCanvas != null)
            {
                HiddenCanvas.alpha = 1f;
                HiddenCanvas.interactable = true;
                HiddenCanvas.blocksRaycasts = true;
                HiddenCanvas = null;

                Raycaster.enabled = true;
            }
        }

        public void DisableUI()
        {
            // update hidden canvas        
            if (HiddenCanvas == null)
            {
                HiddenCanvas = Controller.GetComponent<CanvasGroup>();
                if (HiddenCanvas == null)
                {
                    HiddenCanvas = Controller.gameObject.AddComponent<CanvasGroup>();
                }
            }

            HiddenCanvas.alpha = 0f;
            HiddenCanvas.interactable = false;
            HiddenCanvas.blocksRaycasts = false;

            if (Raycaster == null)
            {
                Raycaster = Controller.GetComponent<GraphicRaycaster>();
            }
            Raycaster.enabled = false;
        }
    }

    public class UIVisibleData
    {
        public List<ActiveControllerData> ActiveControllers;
        public UIVisibleData HiddenData;
        public RawImage Background;
    }

    public partial class UIManager : MonoBehaviour
    {
        public const int UI_LAYER_NORMAL = 0;

        public Material UnlitTextureColorMaterial
        {
            get
            {
                return _UnlitTextureColorMaterial;
            }
#if UNITY_EDITOR
            set
            {
                _UnlitTextureColorMaterial = value;
            }
#endif
        }

        [SerializeField] Material _UnlitTextureColorMaterial;

        private LinkedList<RawImage> poolOverlayBackgrounds = new LinkedList<RawImage>();
        private List<UIVisibleData> VisibleUIs;

        public void InitPoolBackgrounds(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var background = CreateBackground();
                StoreBackgroundToPool(background);
            }
        }

        public void ReleaseAllUIInstances(int layer = UI_LAYER_NORMAL)
        {
            var visibleData = VisibleUIs[layer];
            while (visibleData != null)
            {
                if (visibleData.Background != null)
                {
                    StoreBackgroundToPool(visibleData.Background);
                }

                for (int i = 0; i < visibleData.ActiveControllers.Count; i++)
                {
                    var controller = visibleData.ActiveControllers[i].Controller;
                    if (controller == null)
                        continue;

                    controller.UIRemoved();

                    var cachedUnit = FindCachedUnit(controller);
                    if (cachedUnit != null)
                    {
                        cachedUnit.instanceUI = null;
                        cachedUnit.resourceRequest = null;
                        cachedUnit.asset = null;
                        cachedUIData.Remove(cachedUnit.prefab);
                    }

                    DestroyImmediate(controller.gameObject);
                }
                visibleData.ActiveControllers.Clear();

                visibleData = visibleData.HiddenData;
            }

            VisibleUIs[layer] = new UIVisibleData() { ActiveControllers = new List<ActiveControllerData>() };
            Resources.UnloadUnusedAssets();
        }

        public IEnumerator CleanCachedUIInstances(string[] uiPaths)
        {
            foreach (var path in uiPaths)
            {
                var objUI = GetLoadedUI(path);
                if (objUI != null)
                {
                    ReleaseUI(objUI.GetComponent<UIController>(), true);
                    yield return null;
                }

                UICachedUnit cachedUnit;
                if (cachedUIData.TryGetValue(path, out cachedUnit))
                {
                    if (cachedUnit.instanceUI != null)
                    {
                        var instanceUI = cachedUnit.instanceUI;
                        cachedUnit.instanceUI = null;
                        cachedUnit.asset = null;
                        cachedUnit.resourceRequest = null;

                        Destroy(instanceUI);
                    }

                    cachedUIData.Remove(path);
                }
            }
            cachedUIData.Clear();

            Resources.UnloadUnusedAssets();
        }

        UICachedUnit FindCachedUnit(UIController controller)
        {
            foreach (var unit in cachedUIData.Values)
            {
                if (unit.instanceUI == controller.gameObject)
                {
                    return unit;
                }
            }

            return null;
        }

        public T ShowUIOnTop<T>(string path, int layer = UI_LAYER_NORMAL) where T : UIController
        {
            return ShowUIOnTop<T>(path, false, layer);
        }

        public void ShowUIOnTop(UIController instanceUI, int layer = UI_LAYER_NORMAL)
        {
            ShowUIOnTop(instanceUI, false, layer);
        }

        public T ShowUIOnTop<T>(string path, bool overlap, int layer = UI_LAYER_NORMAL) where T : UIController
        {
            bool needStart;
            var instanceUI = ShowUIOnTop<T>(path, overlap, null, out needStart, layer);
            if (needStart)
            {
                instanceUI.UIStart();
            }

            return instanceUI;
        }

        public void ShowUIOnTop(UIController instanceUI, bool overlap, int layer = UI_LAYER_NORMAL)
        {
            // overlap current visible
            if (overlap)
            {
                for (int i = 0; i < VisibleUIs[layer].ActiveControllers.Count; i++)
                {
                    if (instanceUI != null && VisibleUIs[layer].ActiveControllers[i].Controller == instanceUI)
                        continue;

                    // update hidden canvases
                    var activeController = VisibleUIs[layer].ActiveControllers[i];
                    DisableUI(activeController);
                }

                var visible = VisibleUIs[layer];
                while (visible != null && visible.ActiveControllers.Count == 0)
                {
                    visible = visible.HiddenData;
                }

                VisibleUIs[layer] = new UIVisibleData() { ActiveControllers = new List<ActiveControllerData>(), HiddenData = visible };
            }

            var RootUI = UILayers[layer];
            if (instanceUI.transform.parent != RootUI)
            {
                instanceUI.transform.SetParent(RootUI, false);
                RectTransform rectTrans = instanceUI.GetComponent<RectTransform>();
                rectTrans.anchorMin = Vector2.zero;
                rectTrans.anchorMax = Vector2.one;
                rectTrans.offsetMin = rectTrans.offsetMax = Vector2.zero;
                rectTrans.localScale = Vector3.one;

                Canvas canvas = instanceUI.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = CameraUI;
                canvas.planeDistance = 0f;
            }

            if (!overlap)
            {
                instanceUI.transform.SetAsLastSibling();
            }

            var activeData = new ActiveControllerData() { Controller = instanceUI };
            VisibleUIs[layer].ActiveControllers.Add(activeData);

            if (!instanceUI.gameObject.activeSelf)
            {
                instanceUI.gameObject.SetActive(true);
            }

            EnableUI(activeData, layer);
            instanceUI.UIStart();
        }

        public void ShowUI(UIController instanceUI, bool showTop = true, int layer = UI_LAYER_NORMAL)
        {
            var RootUI = UILayers[layer];
            if (instanceUI.transform.parent != RootUI)
            {
                instanceUI.transform.SetParent(RootUI, false);
                RectTransform rectTrans = instanceUI.GetComponent<RectTransform>();
                rectTrans.anchorMin = Vector2.zero;
                rectTrans.anchorMax = Vector2.one;
                rectTrans.offsetMin = rectTrans.offsetMax = Vector2.zero;
                rectTrans.localScale = Vector3.one;

                Canvas canvas = instanceUI.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = CameraUI;
                canvas.planeDistance = 0f;
            }

            if (showTop)
            {
                instanceUI.transform.SetAsLastSibling();
            }

            var activeData = new ActiveControllerData() { Controller = instanceUI };
            VisibleUIs[layer].ActiveControllers.Add(activeData);

            if (!instanceUI.gameObject.activeSelf)
            {
                instanceUI.gameObject.SetActive(true);
            }

            EnableUI(activeData, layer);
            instanceUI.UIStart();
        }

        public void ShowUIWithOverlayBackgroundAsync<T>(string path, System.Action<T> onInstanceCreated) where T : UIController
        {
            var background = GetBackgroundFromPool();
            StartCoroutine(CaptureOverlayBG(background, background.texture as RenderTexture, () =>
            {
                bool needStart;
                var instanceUI = ShowUIOnTop<T>(path, true, background, out needStart);
                onInstanceCreated(instanceUI);

                // 1st time UI is shown
                if (needStart)
                {
                    instanceUI.UIStart();
                }
            }));
        }

        public void ReleaseAllUIs(bool destroy = false)
        {
            var toRelease = new List<UIController>();
            for (int layer = 0; layer < UILayers.Count; layer++)
            {
                var visibleData = VisibleUIs[layer];
                while (visibleData != null)
                {
                    foreach (var data in visibleData.ActiveControllers)
                        if (data.Controller != null) toRelease.Add(data.Controller);
                    visibleData = visibleData.HiddenData;
                }
            }
            foreach (var controller in toRelease)
                ReleaseUI(controller, destroy);
        }

        public void ReleaseUI(UIController controller, bool destroy)
        {
            if (controller == null)
                return;

            int layer = controller.UILayer;
            // find visible stack
            ActiveControllerData activeData = null;
            var visibleData = VisibleUIs[layer];
            UIVisibleData visibleParent = null;
            while (visibleData != null)
            {
                bool found = false;
                for (int i = 0; i < visibleData.ActiveControllers.Count; i++)
                {
                    if (visibleData.ActiveControllers[i].Controller == controller)
                    {
                        activeData = visibleData.ActiveControllers[i];
                        visibleData.ActiveControllers.RemoveAt(i);

                        if (visibleParent != null && visibleData.ActiveControllers.Count == 0)
                        {
                            visibleParent.HiddenData = visibleData.HiddenData;
                        }
                        found = true;

                        break;
                    }
                }

                if (found)
                {
                    break;
                }

                visibleParent = visibleData;
                visibleData = visibleData.HiddenData;
            }

            if (activeData != null)
            {
                if (visibleData.Background != null && visibleData.ActiveControllers.Count == 0)
                {
                    StoreBackgroundToPool(visibleData.Background);
                }

                if (destroy)
                {
                    controller.UIRemoved();

                    var cachedUnit = FindCachedUnit(controller);
                    if (cachedUnit != null)
                    {
                        cachedUnit.instanceUI = null;
                        cachedUnit.resourceRequest = null;
                        cachedUnit.asset = null;
                        cachedUIData.Remove(cachedUnit.prefab);
                    }

                    DestroyImmediate(controller.gameObject);
                }
                else
                {
                    controller.gameObject.SetActive(false);
                    activeData.Controller.UIRemoved();
                    activeData.HiddenCanvas = null;
                }

                if (visibleData == VisibleUIs[layer] && visibleData.ActiveControllers.Count == 0 && visibleData.HiddenData != null)
                {
                    VisibleUIs[layer] = visibleData.HiddenData;

                    foreach (var active in VisibleUIs[layer].ActiveControllers)
                    {
                        if (active.Controller.gameObject.activeInHierarchy)
                        {
                            EnableUI(active, layer);
                        }
                    }
                }
            }
            else if (destroy)
            {
                DestroyImmediate(controller.gameObject);
            }
        }

        T ShowUIOnTop<T>(string path, bool overlap, RawImage background, out bool needStart, int layer = UI_LAYER_NORMAL) where T : UIController
        {
            // try to load from cache
            UICachedUnit cached;
            T instanceUI = null;
            GameObject loadedAsset = null;
            if (!cachedUIData.TryGetValue(path, out cached))
            {
                cached = new UICachedUnit();
                cached.prefab = path;
                cached.asset = loadedAsset = Resources.Load<GameObject>(GetUIPath(path));
                cachedUIData.Add(path, cached);
            }
            instanceUI = cached.instanceUI == null ? null : cached.instanceUI.GetComponent<T>();

            // overlap current visible
            if (overlap)
            {
                for (int i = 0; i < VisibleUIs[layer].ActiveControllers.Count; i++)
                {
                    if (instanceUI != null && VisibleUIs[layer].ActiveControllers[i].Controller == instanceUI)
                        continue;

                    // update hidden canvases
                    var activeController = VisibleUIs[layer].ActiveControllers[i];
                    DisableUI(activeController);
                }
            }

            ActiveControllerData activeData = null;

            // we should load a new instance
            if (instanceUI == null)
            {
                if (cached.resourceRequest != null && !cached.resourceRequest.isDone)
                {
                    loadedAsset = Resources.Load<GameObject>(GetUIPath(path));
                }
                else if (cached.resourceRequest != null)
                {
                    loadedAsset = cached.resourceRequest.asset as GameObject;
                }
                else
                {
                    loadedAsset = cached.asset as GameObject;
                }

                instanceUI = Instantiate(loadedAsset).GetComponent<T>();
                cached.instanceUI = instanceUI.gameObject;
            }
            // got instance from cache, let remove from its visible stack
            else
            {
                var visibleData = VisibleUIs[layer];
                UIVisibleData visibleParent = null;
                while (visibleData != null)
                {
                    bool found = false;
                    for (int i = 0; i < visibleData.ActiveControllers.Count; i++)
                    {
                        if (visibleData.ActiveControllers[i].Controller == instanceUI)
                        {
                            activeData = visibleData.ActiveControllers[i];
                            visibleData.ActiveControllers.RemoveAt(i);

                            if (visibleParent != null && visibleData.ActiveControllers.Count == 0)
                            {
                                visibleParent.HiddenData = visibleData.HiddenData;
                            }
                            found = true;

                            break;
                        }
                    }

                    if (found)
                    {
                        break;
                    }

                    visibleParent = visibleData;
                    visibleData = visibleData.HiddenData;
                }
            }

            // new overlap visible
            if (overlap)
            {
                var visible = VisibleUIs[layer];
                while (visible != null && visible.ActiveControllers.Count == 0)
                {
                    visible = visible.HiddenData;
                }

                VisibleUIs[layer] = new UIVisibleData() { ActiveControllers = new List<ActiveControllerData>(), HiddenData = visible };

                if (background != null)
                {
                    // attach background
                    background.transform.SetParent(instanceUI.transform, false);
                    background.transform.localPosition = new Vector3(0, 0, 1.5f);
                    background.transform.SetAsFirstSibling();
                    background.gameObject.SetActive(true);

                    VisibleUIs[layer].Background = background;
                }
            }

            var RootUI = UILayers[layer];
            if (instanceUI.transform.parent != RootUI)
            {
                instanceUI.transform.SetParent(RootUI, false);
                RectTransform rectTrans = instanceUI.GetComponent<RectTransform>();
                rectTrans.anchorMin = Vector2.zero;
                rectTrans.anchorMax = Vector2.one;
                rectTrans.offsetMin = rectTrans.offsetMax = Vector2.zero;

                Canvas canvas = instanceUI.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = CameraUI;
                canvas.planeDistance = 0f;
            }
            instanceUI.transform.SetAsLastSibling();

            needStart = false;
            if (activeData == null)
            {
                activeData = new ActiveControllerData() { Controller = instanceUI };
                needStart = true;
            }

            VisibleUIs[layer].ActiveControllers.Add(activeData);

            if (!instanceUI.gameObject.activeSelf)
            {
                instanceUI.gameObject.SetActive(true);
            }

            EnableUI(activeData, layer);

            return instanceUI;
        }

        RawImage GetBackgroundFromPool()
        {
            if (poolOverlayBackgrounds.Count == 0)
            {
                RawImage overlayImage = CreateBackground();
                return overlayImage;
            }

            var background = poolOverlayBackgrounds.First.Value;
            poolOverlayBackgrounds.RemoveFirst();

            return background;
        }

        RawImage CreateBackground(int layer = UI_LAYER_NORMAL)
        {
            var RootUI = UILayers[layer];
            // Create overlay image
            RectTransform overlay = new GameObject("GameUI-Overlay", typeof(RectTransform)).GetComponent<RectTransform>();
            overlay.SetParent(RootUI, false);
            overlay.localPosition = new Vector3(0, 0, 1.5f);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            overlay.gameObject.SetActive(false);
            overlay.gameObject.layer = RootUI.gameObject.layer;
            overlay.SetAsFirstSibling();

            // render texture
            var overlayTexture = new RenderTexture(Screen.width / 2, Screen.height / 2, 24, RenderTextureFormat.ARGB32);
            overlayTexture.name = "GameUI-Overlay";
            overlayTexture.autoGenerateMips = false;
            overlayTexture.filterMode = FilterMode.Bilinear;
            overlayTexture.Create();

            // material        
            RawImage overlayImage = overlay.gameObject.AddComponent<RawImage>();
            overlayImage.material = UnlitTextureColorMaterial;
            overlayImage.texture = overlayTexture;

            var w = GameRect.anchorMax.x - GameRect.anchorMin.x;
            var h = GameRect.anchorMax.y - GameRect.anchorMin.y;
            overlayImage.uvRect = new Rect(GameRect.anchorMin, new Vector2(w, h));

            return overlayImage;
        }

        void StoreBackgroundToPool(RawImage background, int layer = UI_LAYER_NORMAL)
        {
            var RootUI = UILayers[layer];
            background.gameObject.SetActive(false);
            background.transform.SetParent(RootUI, false);
            poolOverlayBackgrounds.AddLast(background);
        }

        void EnableUI(ActiveControllerData activeData, int layer)
        {
            activeData.Controller.UIActive(layer);
            activeData.ActiveUI();
        }

        void DisableUI(ActiveControllerData activeData)
        {
            activeData.DisableUI();

            activeData.Controller.UIDeactive();
        }

        void RefreshUI(ActiveControllerData activeData)
        {
            activeData.Controller.UIRefresh();
        }

        void Update()
        {
            for (int layer = 0; layer < UILayers.Count; layer++)
            {
                var activeCount = VisibleUIs[layer].ActiveControllers.Count;
                for (int i = activeCount - 1; i >= 0; i--)
                {
                    var controller = VisibleUIs[layer].ActiveControllers[i].Controller;
                    controller.UIRefresh();
                }
            }

#if UNITY_ANDROID
            
            bool isBackPressed = Input.GetKeyDown(KeyCode.Escape);
            if (isBackPressed)
            {
                UnityEngine.Debug.Log("code ====== : " + isBackPressed);
            }
            bool triggered = false;
            for (int layer = UILayers.Count - 1; layer >= 0; layer--)
            {
                if (isBackPressed)
                {
                    UnityEngine.Debug.Log("layer : " + layer);
                }
                var activeCount = VisibleUIs[layer].ActiveControllers.Count;
                for (int i = activeCount - 1; i >= 0; i--)
                {
                    if (isBackPressed)
                    {
                        UnityEngine.Debug.Log("i : " + i);
                    }
                    if (isBackPressed)
                    {
                        UnityEngine.Debug.Log("---------");
                        var controller = VisibleUIs[layer].ActiveControllers[i].Controller;
                        controller.Back();
                        triggered = true;
                        break;
                    }
                }

                if (triggered)
                    break;
            }
#endif
        }
    }
}

