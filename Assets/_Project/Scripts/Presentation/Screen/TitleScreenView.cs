using System;
using SugarDash.App;
using SugarDash.Domain.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>타이틀 화면. Screen이 Title일 때만 보이고, 게임 타이틀 아래 지금 유형(1형·2형), 시작 버튼, 오른쪽 위 톱니바퀴 설정 버튼을 둔다.
    /// 레벨 기능이 켜져 있으면 왼쪽 위에 레벨 표시(원 안의 레벨, 원과 이어진 상자의 경험치)를 보인다.</summary>
    // [빈 틀] 담당: 화면
    public sealed class TitleScreenView : MonoBehaviour
    {
        [Tooltip("화면 전체 묶음. 보이고 숨길 대상.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text typeText;
        [Tooltip("위쪽 게임 이름 글자(로고 그림을 넣으면 비워 둔다).")]
        [SerializeField] private Text logoText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [Header("레벨 표시(왼쪽 위)")]
        [Tooltip("원과 상자를 묶은 오브젝트. 레벨 기능이 꺼져 있으면 숨긴다.")]
        [SerializeField] private GameObject levelBadge;
        [Tooltip("원 안의 레벨 숫자.")]
        [SerializeField] private Text levelText;
        [Tooltip("원 옆 상자의 경험치 글자.")]
        [SerializeField] private Text xpText;
        [Tooltip("상자 안의 경험치 채움 막대. 바탕(XpBack)을 꽉 채우게 두면 오른쪽 끝(anchorMax.x)을 지금 레벨에서 다음 레벨까지 온 만큼으로 줄인다.")]
        [SerializeField] private RectTransform xpFill;

        private GameFlowController _flow;

        /// <summary>타이틀과 설정 화면에서 보인다. 설정 창은 어두워진 타이틀 위에 뜬다.</summary>
        public static bool IsVisible(FlowScreen screen) => screen == FlowScreen.Title || screen == FlowScreen.Settings;

        public static string TypeLabel(TrackType? type) => ScreenText.TypeName(type);

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            if (logoText != null) logoText.text = ScreenText.GameTitle;
            ButtonLabel.Set(startButton, ScreenText.Start);
            ButtonLabel.Set(settingsButton, ScreenText.Settings);
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            else Debug.LogWarning("TitleScreenView: startButton이 비어 있다. 씬에서 연결해야 한다.");
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettings);
            else Debug.LogWarning("TitleScreenView: settingsButton이 비어 있다. 씬에서 연결해야 한다.");
            return new Unbinder(() =>
            {
                if (startButton != null) startButton.onClick.RemoveListener(OnStart);
                if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
                _flow = null;
            });
        }

        private void OnStart() => _flow?.StartRun();
        private void OnSettings() => _flow?.OpenSettings();

        /// <summary>경험치 채움 비율(0~1): 지금 레벨 시작에서 다음 레벨까지 온 만큼. 마지막 레벨이면 1.</summary>
        public static float XpFillAmount(int totalXp, int levelStartXp, int? nextLevelXp)
        {
            if (nextLevelXp == null) return 1f;
            int span = nextLevelXp.Value - levelStartXp;
            if (span <= 0) return 1f;
            return Mathf.Clamp01((float)(totalXp - levelStartXp) / span);
        }

        private void Update()
        {
            if (_flow == null) return;
            var visible = IsVisible(_flow.Screen);
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
            if (visible && typeText != null) typeText.text = TypeLabel(_flow.CurrentType);
            if (levelBadge != null && levelBadge.activeSelf != _flow.HasLevels) levelBadge.SetActive(_flow.HasLevels);
            if (visible && _flow.HasLevels)
            {
                if (levelText != null) levelText.text = LevelText.Level(_flow.Level);
                if (xpText != null) xpText.text = LevelText.Progress(_flow.TotalXp, _flow.NextLevelXp);
                if (xpFill != null)
                    xpFill.anchorMax = new Vector2(XpFillAmount(_flow.TotalXp, _flow.LevelStartXp, _flow.NextLevelXp), xpFill.anchorMax.y);
            }
            // TODO(화면): 배치(그림, 글자 크기)와 버튼 소리를 꾸민다.
        }
    }
}
