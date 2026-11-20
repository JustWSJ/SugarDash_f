using System;
using SugarDash.App;
using SugarDash.Domain.Config;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>유형 선택 화면. Screen이 TypeSelect일 때만 보이고, 1형·2형 버튼으로 유형을 고른다.</summary>
    public sealed class TypeSelectView : MonoBehaviour
    {
        [Tooltip("화면 전체 묶음. 보이고 숨길 대상.")]
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text guideText;
        [Tooltip("두 버튼 아래 작은 안내 띠의 글자.")]
        [SerializeField] private Text noteText;
        [SerializeField] private Button type1Button;
        [SerializeField] private Button type2Button;

        private GameFlowController _flow;

        public static bool IsVisible(FlowScreen screen) => screen == FlowScreen.TypeSelect;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            if (titleText != null) titleText.text = ScreenText.TypeSelectTitle;
            if (guideText != null) guideText.text = ScreenText.TypeSelectGuide;
            if (noteText != null) noteText.text = ScreenText.TypeSelectNote;
            ButtonLabel.Set(type1Button, ScreenText.TypeName(TrackType.Type1));
            ButtonLabel.Set(type2Button, ScreenText.TypeName(TrackType.Type2));
            if (type1Button != null) type1Button.onClick.AddListener(OnType1);
            else Debug.LogWarning("TypeSelectView: type1Button이 비어 있다. 씬에서 연결해야 한다.");
            if (type2Button != null) type2Button.onClick.AddListener(OnType2);
            else Debug.LogWarning("TypeSelectView: type2Button이 비어 있다. 씬에서 연결해야 한다.");
            return new Unbinder(() =>
            {
                if (type1Button != null) type1Button.onClick.RemoveListener(OnType1);
                if (type2Button != null) type2Button.onClick.RemoveListener(OnType2);
                _flow = null;
            });
        }

        private void OnType1() => _flow?.SelectType(TrackType.Type1);
        private void OnType2() => _flow?.SelectType(TrackType.Type2);

        private void Update()
        {
            if (_flow == null) return;
            var visible = IsVisible(_flow.Screen);
            if (root != null && root.activeSelf != visible) root.SetActive(visible);
        }
    }
}
