using System;
using SugarDash.App;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 주행 결과 화면. GameFlowController가 결과를 보여 주는 동안 한 번 그리고 나타나며, 끝나면 숨는다.
    /// 점수는 큰 점수 카드에, 대처 요약은 그 아래 줄에, 경험치는 레벨 원과 함께 묶어 보여 준다.
    /// 최고 기록을 새로 세우면 점수 카드를 금색으로 칠하고 "최고 기록!" 띠와 반짝이를 켠다. 문구는 ResultScreenText가 만든다.
    /// </summary>
    public sealed class ResultScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button titleButton;

        [Header("점수 카드")]
        [Tooltip("점수 카드 묶음. 점수가 없으면(튜토리얼, 기록 없음) 숨긴다.")]
        [SerializeField] private GameObject scoreCard;
        [Tooltip("점수 카드 바탕. 최고 기록이면 bestCardColor, 아니면 cardColor로 칠한다.")]
        [SerializeField] private Image scoreCardImage;
        [SerializeField] private Text scoreLabelText;
        [SerializeField] private Text scoreText;
        [Tooltip("점수 아래 작은 띠(최고 점수 또는 이전 최고 점수). 글이 없으면 띠를 숨긴다.")]
        [SerializeField] private GameObject bestChip;
        [SerializeField] private Text bestText;
        [Tooltip("최고 점수 띠의 왕관 아이콘. 최고 기록일 때(이전 최고 점수를 보여 줄 때)는 끈다.")]
        [SerializeField] private GameObject bestChipIcon;
        [Tooltip("최고 기록일 때만 켜는 것들(\"최고 기록!\" 띠, 반짝이).")]
        [SerializeField] private GameObject newBestDecor;
        [SerializeField] private Text newBestText;
        [SerializeField] private Color cardColor = new Color(0.99f, 0.89f, 0.91f);
        [SerializeField] private Color bestCardColor = new Color(1f, 0.83f, 0.36f);

        [Header("요약 마무리 줄 색")]
        [SerializeField] private Color encourageColor = new Color(0.71f, 0.33f, 0.16f);
        [SerializeField] private Color praiseColor = new Color(0.18f, 0.49f, 0.23f);

        [Header("경험치")]
        [Tooltip("경험치 묶음(레벨 원 + 경험치 줄). 경험치가 없으면 숨긴다.")]
        [SerializeField] private GameObject levelRow;
        [Tooltip("레벨 원 안의 숫자.")]
        [SerializeField] private Text levelNumberText;
        [Tooltip("경험치·레벨 오름 줄.")]
        [SerializeField] private Text levelText;

        [Header("레벨 오름 연출")]
        [Tooltip("레벨이 오른 판에만 켜는 것들(\"레벨 업!\" 띠, 반짝이).")]
        [SerializeField] private GameObject levelUpDecor;
        [SerializeField] private Text levelUpText;
        [Tooltip("레벨이 오르면 한 번 커졌다 돌아오는 레벨 원.")]
        [SerializeField] private RectTransform levelCircle;
        [Tooltip("레벨 원이 커졌다 돌아오는 시간(실제 초). 결과 화면이 뜬 뒤 popDelaySec 뒤에 시작한다.")]
        [SerializeField] private float popSec = 0.5f;
        [SerializeField] private float popDelaySec = 0.3f;
        [Tooltip("가장 커질 때의 배율.")]
        [SerializeField] private float popScale = 1.3f;

        private GameFlowController _flow;
        private bool _shown;
        // 레벨 원 연출이 시작된 시각(unscaled). 연출이 없으면 음수.
        private float _popStart = -1f;

        /// <summary>
        /// 레벨 원 배율. delay 전과 delay+duration 뒤는 1, 그 사이에 1 → peak → 1로 한 번 부풀었다 돌아온다.
        /// </summary>
        public static float PopScaleAt(float elapsed, float delay, float duration, float peak)
        {
            float t = elapsed - delay;
            if (t <= 0f || !(duration > 0f) || t >= duration) return 1f;
            return 1f + (peak - 1f) * Mathf.Sin(Mathf.PI * t / duration);
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));

            root.SetActive(false);
            retryButton.onClick.AddListener(OnRetry);
            // 씬에서 타이틀 버튼을 아직 연결하지 않았을 수 있다.
            if (titleButton != null) titleButton.onClick.AddListener(OnTitle);
            if (scoreLabelText != null) scoreLabelText.text = ResultScreenText.ScoreLabel;
            if (newBestText != null) newBestText.text = ResultScreenText.NewBestBadge;
            if (levelUpText != null) levelUpText.text = LevelText.LevelUpBadge;

            return new Unbinder(() =>
            {
                retryButton.onClick.RemoveListener(OnRetry);
                if (titleButton != null) titleButton.onClick.RemoveListener(OnTitle);
                _flow = null;
            });
        }

        private void Update()
        {
            if (_flow == null) return;
            if (_popStart >= 0f) Pop();
            if (_flow.IsShowingResult == _shown) return;

            _shown = _flow.IsShowingResult;
            if (_shown) Draw();
            else root.SetActive(false);
        }

        private void Pop()
        {
            float elapsed = Time.unscaledTime - _popStart;
            if (levelCircle != null) levelCircle.localScale = Vector3.one * PopScaleAt(elapsed, popDelaySec, popSec, popScale);
            if (elapsed >= popDelaySec + popSec) _popStart = -1f;
        }

        private void Draw()
        {
            if (_flow.IsTutorialResult)
            {
                titleText.text = ResultScreenText.TutorialTitle;
                summaryText.text = "";
                SetActive(scoreCard, false);
                SetActive(levelRow, false);
                StartLevelUp(false);
                root.SetActive(true);
                return;
            }
            var result = _flow.LastResult;
            titleText.text = ResultScreenText.Title(result);
            summaryText.text = ColorClosing(ResultScreenText.Summary(result), ResultScreenText.Closing(result),
                ResultScreenText.IsPraise(result) ? praiseColor : encourageColor);
            DrawScore(_flow.LastScore, _flow.BestScore, _flow.PreviousBest, _flow.IsNewBest);
            DrawLevel(_flow.XpGained, _flow.LeveledUp, _flow.Level);
            root.SetActive(true);
        }

        private void DrawScore(double? score, double? best, double? previousBest, bool isNewBest)
        {
            SetActive(scoreCard, score != null);
            if (score == null) return;
            if (scoreText != null) scoreText.text = ResultScreenText.ScoreValue(score.Value);
            if (scoreCardImage != null) scoreCardImage.color = isNewBest ? bestCardColor : cardColor;
            SetActive(newBestDecor, isNewBest);
            string chip = ResultScreenText.BestChip(best, previousBest, isNewBest);
            SetActive(bestChip, chip.Length > 0);
            SetActive(bestChipIcon, !isNewBest);
            if (bestText != null) bestText.text = chip;
        }

        private void DrawLevel(int? xpGained, bool leveledUp, int level)
        {
            SetActive(levelRow, xpGained != null);
            if (levelNumberText != null) levelNumberText.text = LevelText.Level(level);
            if (levelText != null) levelText.text = LevelText.ResultLines(xpGained, leveledUp, level);
            StartLevelUp(xpGained != null && leveledUp);
        }

        /// <summary>레벨이 오른 판이면 띠·반짝이를 켜고 레벨 원 연출을 시작한다. 아니면 끄고 원 크기를 되돌린다.</summary>
        private void StartLevelUp(bool leveledUp)
        {
            SetActive(levelUpDecor, leveledUp);
            _popStart = leveledUp ? Time.unscaledTime : -1f;
            if (levelCircle != null) levelCircle.localScale = Vector3.one;
        }

        /// <summary>요약의 마무리 줄(맨 아래 줄)만 색을 입힌다(Text의 Rich Text). 마무리 줄이 없으면 그대로.</summary>
        public static string ColorClosing(string summary, string closing, Color color)
        {
            if (string.IsNullOrEmpty(closing) || !summary.EndsWith(closing)) return summary;
            string head = summary.Substring(0, summary.Length - closing.Length);
            return head + "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + closing + "</color>";
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }

        private void OnRetry() => _flow?.Retry();

        private void OnTitle() => _flow?.BackToTitle();
    }
}
