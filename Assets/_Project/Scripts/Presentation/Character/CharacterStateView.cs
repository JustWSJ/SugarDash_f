using System;
using SugarDash.App;
using SugarDash.Domain.Director;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>캐릭터 연출 상태. 신호(고혈당·저혈당 증상) > 쉬기 > 보통 순으로 정한다.</summary>
    public enum CharacterState
    {
        Normal,
        Resting,
        TiredHigh,
        TiredLow,
    }

    /// <summary>
    /// 캐릭터 상태를 연출로 바꾼다. 상태는 RunController의 쉬기 여부와 활성 신호로만 정한다.
    /// 혈당을 읽거나 판정하지 않는다. 지친 연출은 연출만이며 달리기 속도·충돌·거리를 바꾸지 않는다.
    /// 2형 높음 신호(TiredHigh)일 때 캐릭터 머리 위에 "지쳤어요" 말풍선을 띄운다(docs/notice-evidence.md 지친 모습).
    /// 말풍선은 처음 필요할 때 코드로 만든다(월드 캔버스, 카메라를 향함).
    /// </summary>
    public sealed class CharacterStateView : MonoBehaviour
    {
        [Tooltip("켜면 주행과 상관없이 아래 샌드박스 상태를 보여 준다(연습용 씬).")]
        [SerializeField] private bool sandboxMode;
        [SerializeField] private CharacterState sandboxState;

        [Header("지친 말풍선")]
        [Tooltip("말풍선 글꼴(Jua). 비워 두면 Unity 기본 글꼴을 쓴다.")]
        [SerializeField] private Font font;
        [Tooltip("말풍선 위치: 이 오브젝트 기준(m). 캐릭터 머리 위에 오게 맞춘다.")]
        [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 2.1f, 0f);
        [Tooltip("말풍선 크기(월드 단위 1 = 화면 단위 몇 개). 작을수록 작게 보인다.")]
        [SerializeField, Min(0.0005f)] private float bubbleScale = 0.006f;
        [SerializeField] private Color bubbleColor = Color.white;
        [SerializeField] private Color bubbleTextColor = new Color(0.29f, 0.2f, 0.17f);
        [Tooltip("말풍선이 위아래로 흔들리는 폭(m)과 한 번 오르내리는 시간(초).")]
        [SerializeField] private float bobHeight = 0.06f;
        [SerializeField, Min(0.01f)] private float bobPeriodSec = 1.2f;

        private RunController _run;
        private CharacterState? _applied;
        private Transform _bubble;
        private Camera _camera;

        /// <summary>신호 > 쉬기 > 보통 순서로 상태를 정한다.</summary>
        public static CharacterState StateOf(bool isResting, NoticeKind? signal)
        {
            if (signal == NoticeKind.High) return CharacterState.TiredHigh;
            if (signal == NoticeKind.Low) return CharacterState.TiredLow;
            return isResting ? CharacterState.Resting : CharacterState.Normal;
        }

        /// <summary>상태별 머리 위 말풍선 글자. 2형 높음(지친 모습)만 "지쳤어요", 나머지는 빈 글자(말풍선 없음).</summary>
        public static string BubbleTextOf(CharacterState state) =>
            state == CharacterState.TiredHigh ? ScreenText.Tired : "";

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        public IDisposable Bind(RunController run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _applied = null;
            return new Unbinder(() =>
            {
                _run = null;
                _applied = null;
                ShowBubble("");
            });
        }

        private void Update()
        {
            CharacterState state;
            if (sandboxMode) state = sandboxState;
            else if (_run != null) state = StateOf(_run.IsResting, _run.ActiveSignal);
            else return;

            if (_applied != state)
            {
                _applied = state;
                Apply(state);
            }
            FollowCamera();
        }

        private void Apply(CharacterState state)
        {
            // 체형(몸 크기·비율)은 바꾸지 않는다. 지금은 머리 위 말풍선만 바꾼다(표정·걸음·자세는 애니메이션이 생기면 더한다).
            ShowBubble(BubbleTextOf(state));
        }

        // 글자가 있으면 말풍선을 켜고(처음이면 만든다), 없으면 끈다.
        private void ShowBubble(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            if (!show)
            {
                if (_bubble != null) _bubble.gameObject.SetActive(false);
                return;
            }
            if (_bubble == null) _bubble = BuildBubble();
            _bubble.GetComponentInChildren<Text>().text = text;
            _bubble.gameObject.SetActive(true);
        }

        // 말풍선이 캐릭터 머리 위에서 살짝 오르내리며 늘 카메라를 보게 한다.
        private void FollowCamera()
        {
            if (_bubble == null || !_bubble.gameObject.activeSelf) return;
            float bob = bobHeight * Mathf.Sin(Time.unscaledTime / bobPeriodSec * 2f * Mathf.PI);
            _bubble.localPosition = bubbleOffset + new Vector3(0f, bob, 0f);
            if (_camera == null) _camera = Camera.main;
            if (_camera != null) _bubble.rotation = _camera.transform.rotation;
        }

        private Transform BuildBubble()
        {
            var root = new GameObject("Tired Bubble", typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.sizeDelta = new Vector2(260f, 96f);
            rect.localScale = Vector3.one * bubbleScale;
            rect.localPosition = bubbleOffset;
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            // 몸통(둥근 대신 단순 사각형) + 아래로 향한 꼬리(45도 돌린 사각형).
            var tail = NewImage("Tail", rect, bubbleColor);
            tail.rectTransform.sizeDelta = new Vector2(34f, 34f);
            tail.rectTransform.anchoredPosition = new Vector2(0f, -48f);
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var body = NewImage("Body", rect, bubbleColor);
            body.rectTransform.anchorMin = Vector2.zero;
            body.rectTransform.anchorMax = Vector2.one;
            body.rectTransform.offsetMin = Vector2.zero;
            body.rectTransform.offsetMax = Vector2.zero;

            var text = new GameObject("Text", typeof(RectTransform)).AddComponent<Text>();
            text.rectTransform.SetParent(rect, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 56;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = bubbleTextColor;
            text.raycastTarget = false;
            return rect;
        }

        private static Image NewImage(string name, RectTransform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
