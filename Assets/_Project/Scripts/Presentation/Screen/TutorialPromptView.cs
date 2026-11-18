using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 튜토리얼 설명 창과 "다시 해봐요" 문구. TutorialController의 단계(Stage)만 보고 보이고 숨긴다.
    /// 판정과 시계는 건드리지 않고, 화면 터치와 "건너뛰기"는 GameFlowController에 명령으로 넘긴다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class TutorialPromptView : MonoBehaviour
    {
        [Tooltip("설명 창 묶음. 화면 전체를 덮어 아래 패널·측정기 터치를 막는다. 넘기기는 ▶·◀ 버튼으로만 한다.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text messageText;
        [Tooltip("\"다시 해봐요\" 문구. 설명 창과 따로 보이고 숨는다.")]
        [SerializeField] private Text retryText;
        [Tooltip("\"다시 해봐요\" 글자를 감싼 띠. 있으면 글자 대신 띠째 보이고 숨는다.")]
        [SerializeField] private GameObject retryBadge;
        [Tooltip("그림 자리 안의 그림. 그림(sprite)이 없으면 그리지 않고 빈 자리만 보인다.")]
        [SerializeField] private Image image;
        [Tooltip("그림 자리 안에 음식 그림을 나란히 놓는 칸들(왼쪽부터). 장마다 음식 수만큼 켠다.")]
        [SerializeField] private Image[] foodPictures;
        [Tooltip("음식 그림 에셋(ChoicePictures). 선택 패널과 같은 에셋을 가리킨다.")]
        [SerializeField] private ChoicePictures pictureAsset;
        [Tooltip("그림 자리 안의 밀기 화살표(오른쪽을 가리키는 그림). 이동·점프·슬라이드 안내에서 미는 방향으로 돌려, 그 방향으로 움직이며 나타났다 사라지기를 되풀이한다.")]
        [SerializeField] private Image swipeArrow;
        [Tooltip("화살표가 한 번 움직이는 거리(가운데에서 양쪽으로 이만큼).")]
        [SerializeField] private float swipeTravel = 45f;
        [Tooltip("화살표가 한 번 나타났다 사라지는 시간(실제 초).")]
        [SerializeField] private float swipePeriodSec = 1.2f;
        [Tooltip("Root에 붙는 화면 전체 버튼. 아래 터치를 막기만 하고 눌러도 넘어가지 않는다(넘기기는 ▶ 버튼만).")]
        [SerializeField] private Button tapArea;
        [Tooltip("\"화면을 눌러요\" 작은 안내 글자. 박스 아래쪽에 둔다.")]
        [SerializeField] private Text hintText;
        [Tooltip("설명 창이 보인 뒤 이 시간(초)이 지나기 전의 터치는 무시한다.")]
        [SerializeField] private float tapDelaySec = 0.5f;
        [SerializeField] private Button skipButton;
        [Header("장 넘기기 (설명이 여러 장일 때만 보임)")]
        [Tooltip("앞 장 버튼. 첫 장에서는 숨긴다.")]
        [SerializeField] private Button backButton;
        [Tooltip("다음 장 버튼. 마지막 장에서 누르면 화면을 누른 것처럼 안내가 이어진다.")]
        [SerializeField] private Button nextButton;
        [Tooltip("쪽 번호(예: 1 / 3).")]
        [SerializeField] private Text pageText;

        private GameFlowController _flow;
        // 설명 창(지금 장)이 보이기 시작한 시각(unscaled). 보이지 않으면 음수.
        private float _shownAt = -1f;
        // 지금 안내의 몇 번째 장인지. 설명 창이 숨거나 안내가 바뀌면 0으로.
        private int _page;
        private TutorialStep? _pageStep;

        public static bool IsPromptVisible(TutorialStage stage) => stage == TutorialStage.Prompt;
        public static bool IsRetryVisible(TutorialStage stage) => stage == TutorialStage.Retry;
        public static bool AcceptsTap(float shownFor, float delay) => shownFor >= delay;

        /// <summary>장이 더 남았는지. 남았으면 누를 때 다음 장으로 넘어가고, 마지막 장이면 안내를 넘긴다.</summary>
        public static bool HasNextPage(int page, int pageCount) => page + 1 < pageCount;

        /// <summary>앞 장 버튼을 보일지. 여러 장이고 첫 장이 아닐 때.</summary>
        public static bool HasPrevPage(int page, int pageCount) => pageCount > 1 && page > 0;

        /// <summary>쪽 번호 글자. 한 장이면 빈 글자.</summary>
        public static string PageLabel(int page, int pageCount) => pageCount > 1 ? $"{page + 1} / {pageCount}" : "";

        /// <summary>안내가 가르치는 미는 방향. 이동·점프·슬라이드 안내만 있고 나머지는 Vector2.zero.</summary>
        public static Vector2 SwipeDirection(TutorialStep? step)
        {
            switch (step)
            {
                case TutorialStep.MoveLeft: return Vector2.left;
                case TutorialStep.MoveRight: return Vector2.right;
                case TutorialStep.Jump: return Vector2.up;
                case TutorialStep.Slide: return Vector2.down;
                default: return Vector2.zero;
            }
        }

        /// <summary>
        /// 화살표 한 번의 움직임. 주기의 앞 70% 동안 −1에서 +1로 밀려가며(처음엔 빠르고 끝에서 느려짐)
        /// 앞 20%에 나타나고 50~70%에 사라진다. 나머지 30%는 쉰다. offset은 −1~1, alpha는 0~1.
        /// </summary>
        public static void SwipeFrame(float time, float periodSec, out float offset, out float alpha)
        {
            float phase = periodSec > 0f ? Mathf.Repeat(time, periodSec) / periodSec : 0f;
            float move = Mathf.Clamp01(phase / 0.7f);
            float eased = 1f - (1f - move) * (1f - move);
            offset = -1f + 2f * eased;
            if (phase < 0.2f) alpha = phase / 0.2f;
            else if (phase < 0.5f) alpha = 1f;
            else if (phase < 0.7f) alpha = 1f - (phase - 0.5f) / 0.2f;
            else alpha = 0f;
        }

        /// <summary>
        /// 설명 창이 보이기 시작한 시각을 갱신한다. 보이면 처음 보인 시각을 지키고(없으면 now), 숨으면 −1.
        /// 설명 창이 숨었다가(직접 해 봄·"다시 해봐요") 다시 뜨면 지연이 처음부터 다시 시작된다.
        /// </summary>
        public static float NextShownAt(bool promptVisible, float shownAt, float now)
        {
            if (!promptVisible) return -1f;
            return shownAt < 0f ? now : shownAt;
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            if (root == null) Debug.LogWarning("TutorialPromptView: root가 비어 있다. 씬에서 연결해야 한다.");
            if (messageText == null) Debug.LogWarning("TutorialPromptView: messageText가 비어 있다. 씬에서 연결해야 한다.");
            if (retryText == null) Debug.LogWarning("TutorialPromptView: retryText가 비어 있다. 씬에서 연결해야 한다.");
            // 화면 아무 곳이나 눌러서는 넘어가지 않는다. tapArea는 아래 터치를 막기만 한다.
            if (tapArea == null) Debug.LogWarning("TutorialPromptView: tapArea가 비어 있다. 아래 패널 터치가 막히지 않을 수 있다.");
            if (nextButton == null) Debug.LogWarning("TutorialPromptView: nextButton이 비어 있다. 설명 창을 넘길 수 없다.");
            if (hintText != null) hintText.text = TutorialText.TapHint;
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkip);
                SetLabel(skipButton, TutorialText.Skip);
            }
            else Debug.LogWarning("TutorialPromptView: skipButton이 비어 있다. 씬에서 연결해야 한다.");
            if (retryText != null) retryText.text = TutorialText.TryAgain;
            if (backButton != null) backButton.onClick.AddListener(OnBack);
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            if (image != null) image.enabled = image.sprite != null;
            SetActive(RetryObject, false);
            return new Unbinder(() =>
            {
                if (skipButton != null) skipButton.onClick.RemoveListener(OnSkip);
                if (backButton != null) backButton.onClick.RemoveListener(OnBack);
                if (nextButton != null) nextButton.onClick.RemoveListener(OnNext);
                if (root != null) root.SetActive(false);
                SetActive(RetryObject, false);
                _flow = null;
                _shownAt = -1f;
                _page = 0;
                _pageStep = null;
            });
        }

        private GameObject RetryObject => retryBadge != null ? retryBadge : retryText != null ? retryText.gameObject : null;

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }

        private static void SetLabel(Button button, string label)
        {
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.text = label;
        }

        // ▶ 버튼. 다음 장이 있으면 넘기고, 마지막 장이면 안내를 이어 간다.
        private void OnNext()
        {
            if (!TapReady()) return;
            var tutorial = _flow.Tutorial;
            if (tutorial.Step != null && HasNextPage(_page, TutorialText.Pages(tutorial.Step.Value, tutorial.Type).Count))
            {
                // 다음 장. 장마다 터치 지연을 처음부터 센다.
                _page++;
                _shownAt = Time.unscaledTime;
                return;
            }
            tutorial.Acknowledge();
        }

        // 앞 장으로. 지연은 다음 장과 같게 다시 센다.
        private void OnBack()
        {
            if (!TapReady() || _page <= 0) return;
            _page--;
            _shownAt = Time.unscaledTime;
        }

        // 건너뛰기는 설명 창 밖 화면 왼쪽 아래 구석에 따로 있어 설명 창을 넘기는 터치와 겹치지 않는다. 지연 없이 받는다.
        private void OnSkip() => _flow?.SkipTutorial();

        /// <summary>건너뛰기 버튼을 보일지. 건너뛸 수 있는 튜토리얼 안내 구간이면 설명 창이 없어도 언제든 보인다(일시정지 메뉴 중 제외).</summary>
        public static bool IsSkipVisible(FlowScreen screen, bool pauseMenuOpen, bool guided, bool canSkip) =>
            screen == FlowScreen.Running && !pauseMenuOpen && guided && canSkip;

        private bool TapReady() =>
            _flow != null && _shownAt >= 0f && AcceptsTap(Time.unscaledTime - _shownAt, tapDelaySec);

        private void Update()
        {
            if (_flow == null) return;
            var tutorial = _flow.Tutorial;
            var stage = tutorial.Stage;

            // root는 화면 전체를 덮어 아래 패널·측정기 터치를 막는다.
            bool prompt = IsPromptVisible(stage);
            _shownAt = NextShownAt(prompt, _shownAt, Time.unscaledTime);
            if (root != null && root.activeSelf != prompt) root.SetActive(prompt);
            if (!prompt || tutorial.Step != _pageStep)
            {
                _page = 0;
                _pageStep = prompt ? tutorial.Step : null;
            }
            TutorialPage page = null;
            int pageCount = 0;
            if (prompt && tutorial.Step != null)
            {
                var pages = TutorialText.Pages(tutorial.Step.Value, tutorial.Type);
                pageCount = pages.Count;
                page = pages[Mathf.Min(_page, pages.Count - 1)];
                if (messageText != null) messageText.text = page.Text;
            }
            DrawFoods(page);
            if (backButton != null) SetActive(backButton.gameObject, HasPrevPage(_page, pageCount));
            if (nextButton != null) SetActive(nextButton.gameObject, pageCount > 0);
            if (pageText != null)
            {
                pageText.text = PageLabel(_page, pageCount);
                SetActive(pageText.gameObject, pageCount > 1);
            }
            bool skip = IsSkipVisible(_flow.Screen, _flow.IsPauseMenuOpen, tutorial.IsGuided, tutorial.CanSkip);
            if (skipButton != null && skipButton.gameObject.activeSelf != skip) skipButton.gameObject.SetActive(skip);

            SetActive(RetryObject, IsRetryVisible(stage));
            DrawSwipeArrow(prompt ? tutorial.Step : null);
            // 설명 창이 나타나는 연출은 Box의 ScreenPopIn이 한다.
        }

        private void DrawFoods(TutorialPage page)
        {
            if (foodPictures == null) return;
            var set = ChoicePictures.Of(pictureAsset);
            for (int i = 0; i < foodPictures.Length; i++)
            {
                var slot = foodPictures[i];
                if (slot == null) continue;
                var sprite = page != null && i < page.FoodIds.Count ? set.Food(page.FoodIds[i]) : null;
                slot.sprite = sprite;
                SetActive(slot.gameObject, sprite != null);
            }
        }

        private void DrawSwipeArrow(TutorialStep? step)
        {
            if (swipeArrow == null) return;
            var direction = SwipeDirection(step);
            bool show = direction != Vector2.zero;
            if (swipeArrow.gameObject.activeSelf != show) swipeArrow.gameObject.SetActive(show);
            if (!show) return;
            // 설명 창이 뜬 때부터 센다. 창이 다시 뜨면 화살표도 처음부터 움직인다.
            SwipeFrame(Time.unscaledTime - _shownAt, swipePeriodSec, out float offset, out float alpha);
            var rect = swipeArrow.rectTransform;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            rect.anchoredPosition = direction * (offset * swipeTravel);
            var color = swipeArrow.color;
            color.a = alpha;
            swipeArrow.color = color;
        }
    }
}
