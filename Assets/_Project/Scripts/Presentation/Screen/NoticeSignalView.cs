using System;
using SugarDash.App;
using SugarDash.Domain.Director;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 2형 notice 신호 표시. 활성 신호(RunController.ActiveSignal)만 보고 말풍선·그림을 보여 준다.
    /// 높음은 물컵(갈증)과 깜빡이는 화장실 그림, 낮음은 좌우로 떨리는 손 그림이 글자 왼쪽에 붙는다.
    /// 혈당을 읽거나 판정하지 않는다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class NoticeSignalView : MonoBehaviour
    {
        [SerializeField] private Text signalText;
        [Tooltip("글자를 감싼 흰 말풍선(CanvasGroup). 신호가 없으면 끈다. 말풍선은 늘 불투명하고 깜빡임은 글자에만 건다(말풍선과 꼬리가 겹친 곳이 비쳐 보이지 않게).")]
        [SerializeField] private CanvasGroup bubble;
        [Header("그림 (말풍선 안 글자 왼쪽, 비우면 글자만)")]
        [Tooltip("높음: 갈증(물컵) 그림 칸 묶음.")]
        [SerializeField] private GameObject thirst;
        [Tooltip("높음: 화장실 그림 칸 묶음. 안의 그림(toiletPicture)이 깜빡인다.")]
        [SerializeField] private GameObject toilet;
        [SerializeField] private Graphic toiletPicture;
        [Tooltip("낮음: 떨리는 손 그림 칸 묶음. 안의 그림(shakyHandPicture)이 좌우로 떨린다.")]
        [SerializeField] private GameObject shakyHand;
        [SerializeField] private RectTransform shakyHandPicture;
        [Tooltip("손이 떨리는 폭(화면 단위).")]
        [SerializeField] private float shakeAmplitude = 6f;
        [Tooltip("손이 1초에 떨리는 횟수.")]
        [SerializeField] private float shakePerSec = 8f;

        [Header("깜빡임")]
        [Tooltip("높음 신호가 흐려졌다 밝아지는 한 번의 시간(실제 초).")]
        [SerializeField] private float blinkPeriodSec = 1f;
        [Tooltip("깜빡일 때 가장 흐린 투명도.")]
        [SerializeField] private float blinkMinAlpha = 0.3f;

        private RunController _run;

        /// <summary>신호별 문구(docs/notice-evidence.md). 신호가 없으면 빈 문자열.</summary>
        public static string LabelOf(NoticeKind? signal)
        {
            if (signal == NoticeKind.High) return "목말라요";
            if (signal == NoticeKind.Low) return "몸이 떨려요";
            return "";
        }

        /// <summary>깜빡임 투명도. 주기의 처음과 끝이 1, 가운데가 minAlpha. 주기가 0 이하이면 1.</summary>
        public static float BlinkAlpha(float time, float periodSec, float minAlpha)
        {
            if (!(periodSec > 0)) return 1f;
            float phase = Mathf.Repeat(time, periodSec) / periodSec;
            float wave = 0.5f + 0.5f * Mathf.Cos(phase * 2f * Mathf.PI);
            return Mathf.Lerp(minAlpha, 1f, wave);
        }

        /// <summary>떨리는 손의 좌우 위치. 0에서 시작해 ±amplitude 사이를 1초에 perSec번 오간다.</summary>
        public static float ShakeOffset(float time, float amplitude, float perSec) =>
            amplitude * Mathf.Sin(time * perSec * 2f * Mathf.PI);

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        public IDisposable Bind(RunController run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            return new Unbinder(() =>
            {
                if (signalText != null) signalText.text = "";
                if (bubble != null) bubble.gameObject.SetActive(false);
                ShowPictures(null, 1f);
                _run = null;
            });
        }

        private void Update()
        {
            if (_run == null) return;
            var signal = _run.ActiveSignal;
            // 높음 신호만 깜빡인다(화장실 그림 깜빡임). 화장실 그림이 없으면 글자가 대신 깜빡인다. 낮음은 가만히 둔다.
            float alpha = signal == NoticeKind.High ? BlinkAlpha(Time.unscaledTime, blinkPeriodSec, blinkMinAlpha) : 1f;
            bool toiletBlinks = toiletPicture != null && toilet != null;
            ShowPictures(signal, alpha);
            if (signalText != null) signalText.text = LabelOf(signal);
            if (bubble != null)
            {
                bool show = signal != null;
                if (bubble.gameObject.activeSelf != show) bubble.gameObject.SetActive(show);
                bubble.alpha = 1f;
            }
            if (signalText != null)
            {
                var color = signalText.color;
                color.a = toiletBlinks ? 1f : alpha;
                signalText.color = color;
            }
        }

        private void ShowPictures(NoticeKind? signal, float blinkAlpha)
        {
            SetActive(thirst, signal == NoticeKind.High);
            SetActive(toilet, signal == NoticeKind.High);
            SetActive(shakyHand, signal == NoticeKind.Low);
            if (toiletPicture != null)
            {
                var color = toiletPicture.color;
                color.a = blinkAlpha;
                toiletPicture.color = color;
            }
            if (shakyHandPicture != null)
            {
                float x = signal == NoticeKind.Low ? ShakeOffset(Time.unscaledTime, shakeAmplitude, shakePerSec) : 0f;
                shakyHandPicture.anchoredPosition = new Vector2(x, shakyHandPicture.anchoredPosition.y);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active) target.SetActive(active);
        }
    }
}
