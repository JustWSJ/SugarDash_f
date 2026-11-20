using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 인트로 컷툰. 처음 실행(저장된 유형 없음)이면 유형 선택 화면 위에 먼저 뜬다. 흐름: (인트로, 미정) → 컷툰 → 유형 선택 → 타이틀.
    /// 화면 아래 오른쪽 화살표로 다음 장, 왼쪽 화살표로 앞 장으로 가고, 쪽 번호는 두 화살표 사이에 둔다.
    /// 마지막 장에서 오른쪽 화살표를 누르거나 건너뛰기를 누르면 닫히고 유형 선택 화면이 보인다. 건너뛰기 버튼은 Show Skip Button을 켰을 때만 보인다.
    /// 설정 화면의 "인트로 다시보기"(GameFlowController.IntroReplayRequests가 바뀜)로 다시 열 수 있다. 흐름은 그때 타이틀로 바뀌므로 닫으면 타이틀이 보인다.
    /// 연출만이다. 게임 흐름(GameFlowController)을 읽기만 하고 명령을 보내지 않는다. 혈당·기록과 관계없다.
    /// 화면 오브젝트(캔버스, 그림, 버튼)는 처음 열 때 코드로 만든다. 프리팹이 생기면 바꾼다.
    /// </summary>
    public sealed class IntroCuttoonView : MonoBehaviour
    {
        [Tooltip("컷툰 그림. 보여 줄 순서대로 넣는다. 비어 있으면 컷툰을 열지 않는다.")]
        [SerializeField] private Sprite[] panels;
        [Tooltip("버튼·안내 글자에 쓸 글꼴(Jua). 비워 두면 Unity 기본 글꼴을 쓴다.")]
        [SerializeField] private Font font;
        [Tooltip("켜면 앱을 켤 때마다 첫 화면(타이틀) 위에 보여 준다(확인용). 끄면 처음 실행의 유형 선택 앞에서만 보여 준다.")]
        [SerializeField] private bool showEveryLaunch;
        [Tooltip("그림이 여러 장일 때 건너뛰기 버튼을 보여 준다. 끄면 화살표로 끝까지 넘긴다.")]
        [SerializeField] private bool showSkipButton;

        [Header("모양")]
        [SerializeField] private Color backgroundColor = new Color(1f, 0.98f, 0.94f);
        [Tooltip("버튼 바탕. 다른 화면의 기본 버튼(흰색)과 맞춘다(docs/ui-style.md).")]
        [SerializeField] private Color buttonColor = Color.white;
        [Tooltip("버튼 글자·화살표 색")]
        [SerializeField] private Color buttonTextColor = new Color(0.17f, 0.18f, 0.2f);
        [SerializeField] private Color hintColor = new Color(0.17f, 0.18f, 0.2f);
        [Tooltip("버튼 바탕 그림(둥근 상자, 테두리 포함, 9-slice). 비워 두면 네모에 테두리 선을 두른다.")]
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private int sortingOrder = 500;
        [SerializeField] private string skipLabel = "건너뛰기";

        private GameFlowController _flow;
        private GameObject _root;
        private Image _panelImage;
        private Text _pageText;
        private GameObject _skipButton;
        private GameObject _backButton;
        private GameObject _nextButton;
        private Sprite _arrowSprite;
        private int _index;
        private bool _shown;
        private int _replaySeen;

        /// <summary>컷툰이 떠 있다.</summary>
        public bool IsOpen => _root != null && _root.activeSelf;

        /// <summary>
        /// 컷툰을 열 때인지. 아직 보여 주지 않았고, 유형 선택 화면(처음 실행)이거나 매번 보기가 켜져 있고 타이틀일 때 연다.
        /// </summary>
        public static bool ShouldOpen(FlowScreen screen, bool alreadyShown, bool everyLaunch) =>
            !alreadyShown && (screen == FlowScreen.TypeSelect || everyLaunch && screen == FlowScreen.Title);

        /// <summary>설정 화면에서 다시보기를 눌렀는지. 지금 본 횟수와 요청 횟수가 다르고 타이틀(다시보기를 누르면 바뀌는 화면)일 때 연다.</summary>
        public static bool ShouldReplay(FlowScreen screen, int requests, int seen) =>
            screen == FlowScreen.Title && requests != seen;

        /// <summary>index번째 칸이 마지막 칸인지. 마지막 칸에서 다음 화살표를 누르면 닫힌다.</summary>
        public static bool IsLast(int index, int count) => index >= count - 1;

        /// <summary>앞 장 화살표(왼쪽 아래)를 보여 줄지. 첫 장에는 없다.</summary>
        public static bool HasBack(int index) => index > 0;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제하고 컷툰을 닫는다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _replaySeen = _flow.IntroReplayRequests;
            return new Unbinder(() =>
            {
                _flow = null;
                Close();
            });
        }

        private void Update()
        {
            if (_flow == null) return;
            if (ShouldReplay(_flow.Screen, _flow.IntroReplayRequests, _replaySeen))
            {
                _replaySeen = _flow.IntroReplayRequests;
                _shown = true;
                Open();
                return;
            }
            if (!ShouldOpen(_flow.Screen, _shown, showEveryLaunch)) return;
            _shown = true;
            Open();
        }

        private void Open()
        {
            if (panels == null || panels.Length == 0) return;
            if (_root == null) Build();
            _index = 0;
            _root.SetActive(true);
            Show();
        }

        private void Next()
        {
            if (!IsOpen) return;
            if (IsLast(_index, panels.Length)) { Close(); return; }
            _index++;
            Show();
        }

        private void Back()
        {
            if (!IsOpen || !HasBack(_index)) return;
            _index--;
            Show();
        }

        private void Close()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void Show()
        {
            _panelImage.sprite = panels[_index];
            _backButton.SetActive(HasBack(_index));
            // 그림이 한 장이면 쪽 번호를 숨긴다. 건너뛰기는 켜 두었고 그림이 여러 장일 때만 보인다.
            _pageText.gameObject.SetActive(panels.Length > 1);
            _skipButton.SetActive(showSkipButton && panels.Length > 1);
            _pageText.text = (_index + 1) + " / " + panels.Length;
        }

        // ───── 화면 만들기 ─────

        private void Build()
        {
            _root = new GameObject("Intro Cuttoon Canvas");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            _root.AddComponent<GraphicRaycaster>();

            // 배경: 화면 전체를 덮어 아래 타이틀 버튼이 눌리지 않게 터치를 막는다. 넘기기는 아래 화살표로만 한다.
            AddImage("Background", _root.transform, backgroundColor, Vector2.zero, Vector2.one);

            // 건너뛰기를 끄면 위쪽 줄이 비므로 그림을 위로 늘린다.
            float panelTop = showSkipButton ? 0.905f : 0.98f;
            _panelImage = AddImage("Panel", _root.transform, Color.white, new Vector2(0.02f, 0.11f), new Vector2(0.98f, panelTop));
            _panelImage.preserveAspect = true;
            _panelImage.raycastTarget = false;

            // 아래 줄: 왼쪽 앞 장 화살표, 가운데 쪽 번호, 오른쪽 다음 장 화살표(마지막 장에서는 닫기).
            _arrowSprite = CreateArrowSprite();
            _backButton = AddArrowButton("Back", new Vector2(0f, 0f), new Vector2(BottomMargin, BottomMargin), true, Back);
            _nextButton = AddArrowButton("Next", new Vector2(1f, 0f), new Vector2(-BottomMargin, BottomMargin), false, Next);

            _pageText = AddText("Page", _root.transform, 40, hintColor, new Vector2(0.3f, 0f), new Vector2(0.7f, 0f));
            _pageText.rectTransform.pivot = new Vector2(0.5f, 0f);
            _pageText.rectTransform.anchoredPosition = new Vector2(0f, BottomMargin);
            _pageText.rectTransform.sizeDelta = new Vector2(0f, ArrowSize);

            var skip = AddImage("Skip", _root.transform, buttonColor, new Vector2(0.66f, 0.915f), new Vector2(0.96f, 0.975f));
            skip.gameObject.AddComponent<Button>().onClick.AddListener(Close);
            StyleButton(skip);
            _skipButton = skip.gameObject;
            AddText("Label", skip.transform, 38, buttonTextColor, Vector2.zero, Vector2.one).text = skipLabel;
        }

        private const float BottomMargin = 40f;
        private const float ArrowSize = 130f;

        // corner(0,0 왼쪽 아래 / 1,0 오른쪽 아래)에 붙는 고정 크기 버튼.
        private Image AddButton(string name, Vector2 corner, Vector2 offset, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var image = AddImage(name, _root.transform, buttonColor, corner, corner);
            var rect = image.rectTransform;
            rect.pivot = corner;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;
            image.gameObject.AddComponent<Button>().onClick.AddListener(onClick);
            StyleButton(image);
            return image;
        }

        // 둥근 상자 그림이 있으면 그것을 쓰고(테두리가 그림에 들어 있다), 없으면 테두리 선을 두른다.
        private void StyleButton(Image image)
        {
            if (buttonSprite == null)
            {
                AddOutline(image);
                return;
            }
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.2f;
        }

        // 진한 색 테두리를 둘러 밝은 배경 위에서도 버튼이 보이게 한다(둥근 상자 그림이 없을 때).
        private void AddOutline(Image image)
        {
            var outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = buttonTextColor;
            outline.effectDistance = new Vector2(3f, -3f);
        }

        private GameObject AddArrowButton(string name, Vector2 corner, Vector2 offset, bool pointLeft, UnityEngine.Events.UnityAction onClick)
        {
            var button = AddButton(name, corner, offset, new Vector2(ArrowSize, ArrowSize), onClick);
            var arrow = AddImage("Arrow", button.transform, buttonTextColor, new Vector2(0.28f, 0.25f), new Vector2(0.72f, 0.75f));
            arrow.sprite = _arrowSprite;
            arrow.raycastTarget = false;
            // 그림은 오른쪽을 가리킨다. 왼쪽 화살표는 좌우로 뒤집는다.
            if (pointLeft) arrow.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            return button.gameObject;
        }

        // 글꼴에 ▶ 글자가 없을 수 있어 오른쪽을 가리키는 흰 삼각형을 직접 그린다. 색은 Image.color로 입힌다.
        private static Sprite CreateArrowSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                // 왼쪽 변 전체에서 오른쪽 가운데 꼭짓점으로 좁아진다.
                float half = (size * 0.5f) * (1f - Mathf.Abs(y + 0.5f - size * 0.5f) / (size * 0.5f));
                for (int x = 0; x < size; x++)
                {
                    float limit = half * 2f;
                    float a = Mathf.Clamp01(limit - x);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void OnDestroy()
        {
            if (_arrowSprite == null) return;
            Destroy(_arrowSprite.texture);
            Destroy(_arrowSprite);
        }

        private static Image AddImage(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var image = AddRect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text AddText(string name, Transform parent, int size, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var text = AddRect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = true;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform AddRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
