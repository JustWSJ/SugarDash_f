using System;
using System.Collections.Generic;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 표시 혈당바, 게이지 경고, 측정기 버튼. 혈당바는 두 유형 모두 표시 혈당(지연된 값)만 보여 주고,
    /// 표시 혈당에 따라 초록 → 노랑 → 빨강으로 색이 바뀐다(BarColor). 측정기를 누르면 그때의 실제 혈당을 보여 준다.
    /// </summary>
    public sealed class HudPresenter : MonoBehaviour
    {
        [SerializeField] private Slider glucoseBar;
        [Header("혈당바 색")]
        [SerializeField] private Color barGreen = new Color(0.30f, 0.75f, 0.35f);
        [SerializeField] private Color barYellow = new Color(0.98f, 0.80f, 0.20f);
        [SerializeField] private Color barRed = new Color(0.90f, 0.25f, 0.20f);
        [SerializeField] private Text glucoseText;
        [SerializeField] private Text warningText;
        [SerializeField] private Button meterButton;
        [SerializeField] private Text meterText;
        [SerializeField] private Text statusText;
        [Tooltip("점수 표시. 비워 두면 표시하지 않는다.")]
        [SerializeField] private Text scoreText;
        [Tooltip("재개 카운트다운 숫자(3·2·1). 비워 두면 표시하지 않는다.")]
        [SerializeField] private Text countdownText;
        [Tooltip("측정 결과 글자를 보여 주는 시간(초). 지나면 지운다.")]
        [SerializeField] private float meterTextShowSec = 3f;

        private RunController _run;
        private GameFlowController _flow;
        private int _shownRunCount;
        private Image _barFill;
        // 측정 결과 글자를 지울 시각(Time.time). 보여 주는 글자가 없으면 음수.
        private float _meterTextHideAt = -1;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 구독과 버튼 연결을 해제한다.</summary>
        public IDisposable Bind(RunController run, GameFlowController flow, DomainEventBus bus,
            GlucoseLevelConfig levels)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));

            glucoseBar.minValue = (float)levels.Min;
            glucoseBar.maxValue = (float)levels.Max;
            glucoseBar.interactable = false;
            if (glucoseBar.fillRect != null) _barFill = glucoseBar.fillRect.GetComponent<Image>();
            meterText.text = "";
            meterButton.onClick.AddListener(OnMeter);

            var subscriptions = new List<IDisposable>
            {
                bus.Subscribe<MeterPressed>(e =>
                {
                    meterText.text = HudText.Measured(e.Measured);
                    _meterTextHideAt = Time.time + meterTextShowSec;
                }),
            };

            return new Unbinder(() =>
            {
                foreach (var subscription in subscriptions) subscription.Dispose();
                meterButton.onClick.RemoveListener(OnMeter);
                _run = null;
                _flow = null;
            });
        }

        private void Update()
        {
            if (_run == null) return;

            if (_flow.IsShowingResult)
            {
                // 주행이 닫혀 혈당 값이 없으므로 마지막 화면을 그대로 둔다. 결과는 ResultScreenView가 보여 준다.
                meterButton.interactable = false;
                if (countdownText != null) countdownText.text = "";
                if (scoreText != null && _flow.LastScore != null)
                    scoreText.text = HudText.Score((int)Math.Floor(_flow.LastScore.Value));
                return;
            }

            if (_run.RunCount != _shownRunCount)
            {
                _shownRunCount = _run.RunCount;
                ClearMeterText();
            }

            if (_meterTextHideAt >= 0 && Time.time >= _meterTextHideAt) ClearMeterText();

            // 2형에는 측정기·게이지 경고가 없어 측정 관련 표시는 숨긴다.
            // 혈당바는 두 유형 모두 표시 혈당이다. 2형도 늦게 따라와 몸의 신호(notice)가 바 색보다 먼저 온다.
            bool type2 = _run.TrackType == TrackType.Type2;
            meterButton.gameObject.SetActive(!type2);
            meterText.enabled = !type2;
            double displayed = _run.Displayed;
            glucoseBar.value = (float)displayed;
            var glucose = _run.CurrentConfig?.Glucose;
            if (_barFill != null && glucose != null)
                _barFill.color = BarColor(displayed, glucose.Levels, glucose.Display.BarGreenFrom, barRed, barYellow, barGreen);
            glucoseText.text = $"{displayed:0} mg/dL";
            warningText.enabled = !type2 && _run.IsGaugeWarningOn;

            // 2~4단계와 알리기 대기 중에는 패널이 화면을 맡는다.
            var phase = _run.Phase;
            meterButton.interactable = _run.State == RunState.Running &&
                                       (phase == HypoPhase.Idle || phase == HypoPhase.Recognize ||
                                        phase == HypoPhase.Grace) &&
                                       IsMeterAllowed(_flow.Tutorial.AllowedControls);

            if (scoreText != null) scoreText.text = HudText.Score((int)Math.Floor(_run.Score));

            if (countdownText != null)
            {
                int? number = AnswerFeedbackText.CountdownNumber(_run.ResumeCountdownRemaining);
                countdownText.text = number.HasValue ? number.Value.ToString() : "";
            }

            // '잠시 멈춤'은 일시정지 메뉴가 버튼 위에 보여 준다(PauseMenuView).
            statusText.text = _run.Phase == HypoPhase.ResumeCountdown && _run.IsEmptyBag ? HudText.PackFastSugar : "";
        }

        /// <summary>
        /// 혈당바 색. hypoThreshold 이하와 hyperThreshold 이상은 빨강, barGreenFrom~normalRangeMax는 초록이고,
        /// 그 사이는 빨강 → 노랑 → 초록(가운데가 노랑)으로 점점 바뀐다.
        /// </summary>
        public static Color BarColor(double glucose, GlucoseLevelConfig levels, double barGreenFrom,
            Color red, Color yellow, Color green)
        {
            if (glucose < barGreenFrom)
                return Blend(Progress(glucose, levels.HypoThreshold, barGreenFrom), red, yellow, green);
            if (glucose > levels.NormalRangeMax)
                return Blend(Progress(glucose, levels.HyperThreshold, levels.NormalRangeMax), red, yellow, green);
            return green;
        }

        // danger(빨강)에서 safe(초록)까지 어디쯤인지 0~1. danger 너머는 0.
        private static float Progress(double glucose, double danger, double safe) =>
            safe == danger ? 1f : Mathf.Clamp01((float)((glucose - danger) / (safe - danger)));

        // 0 빨강, 0.5 노랑, 1 초록.
        private static Color Blend(float t, Color red, Color yellow, Color green) =>
            t < 0.5f ? Color.Lerp(red, yellow, t * 2f) : Color.Lerp(yellow, green, (t - 0.5f) * 2f);

        /// <summary>튜토리얼 조작 잠금에서 측정기가 허용되는지. 일반 판은 All이라 항상 허용된다.</summary>
        public static bool IsMeterAllowed(TutorialControls allowed) => (allowed & TutorialControls.Meter) != 0;

        private void OnMeter()
        {
            if (_run == null || _flow == null || !IsMeterAllowed(_flow.Tutorial.AllowedControls)) return;
            _run.PressMeter();
        }

        private void ClearMeterText()
        {
            meterText.text = "";
            _meterTextHideAt = -1;
        }
    }
}
