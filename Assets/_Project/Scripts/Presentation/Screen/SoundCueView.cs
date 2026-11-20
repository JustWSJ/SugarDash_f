using System;
using System.Collections.Generic;
using SugarDash.App;
using SugarDash.Domain.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 화면 효과음. 버튼 누름, 선택, 게이지 경고, 선택 패널 열림, 재개 카운트다운, 결과, 레벨 업, 트랙 음식 먹기에 소리를 낸다.
    /// 소리만 낸다. 판정·혈당·기록을 바꾸지 않고 정답·오답을 소리로 나누지 않는다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundCueView : MonoBehaviour
    {
        [Tooltip("효과음 묶음 에셋. 비우면 소리가 나지 않는다.")]
        [SerializeField] private SoundCues cues;
        [Tooltip("이 아래의 버튼을 누르면 버튼 소리가 난다. 보통 Canvas.")]
        [SerializeField] private Transform buttonRoot;
        [Tooltip("이 아래의 버튼에는 버튼 소리를 걸지 않는다. 선택 패널은 고른 뒤 이벤트로 선택 소리를 낸다.")]
        [SerializeField] private Transform[] skipButtonsUnder = Array.Empty<Transform>();
        [Tooltip("결과 화면이 뜬 뒤 레벨 업 소리까지의 시간(초). 결과 화면의 레벨 원 연출(Pop Delay Sec)과 맞춘다.")]
        [SerializeField] private float levelUpDelaySec = 0.3f;

        private AudioSource _source;
        private GameFlowController _flow;
        private RunController _run;
        private readonly List<Button> _buttons = new List<Button>();
        // 한 번 누름에 버튼 소리와 선택 소리가 겹치지 않게, 한 프레임에 하나만 낸다.
        private int _pressFrame = -1;
        private bool _wasFrozen;
        private double _lastCountdown;
        private bool _wasResult;
        private float _levelUpAt = -1f;

        /// <summary>
        /// 카운트다운 소리. 보이는 숫자(남은 초를 올림)가 바뀌면 CountTick, 0이 되면 CountGo, 같은 숫자 안에서는 null.
        /// </summary>
        public static SoundCue? CountdownCue(double previous, double now)
        {
            int before = previous > 0 ? (int)Math.Ceiling(previous) : 0;
            int after = now > 0 ? (int)Math.Ceiling(now) : 0;
            if (after == before) return null;
            return after == 0 ? SoundCue.CountGo : SoundCue.CountTick;
        }

        /// <summary>고른 뒤 이벤트가 선택 소리를 낼지. 시간 초과(고른 것 없음)는 소리를 내지 않는다.</summary>
        public static bool IsPick(StepAnswered e) => e != null && e.ChoiceId != null;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 버튼·이벤트 연결을 해제한다.</summary>
        public IDisposable Bind(GameFlowController flow, RunController run, DomainEventBus bus)
        {
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;

            _buttons.Clear();
            if (buttonRoot != null)
            {
                foreach (var button in buttonRoot.GetComponentsInChildren<Button>(true))
                {
                    if (IsSkipped(button.transform)) continue;
                    button.onClick.AddListener(OnTap);
                    _buttons.Add(button);
                }
            }

            var subscriptions = new List<IDisposable>
            {
                bus.Subscribe<StepAnswered>(e => { if (IsPick(e)) Press(SoundCue.Choose); }),
                bus.Subscribe<MeterPressed>(_ => Press(SoundCue.Choose)),
                bus.Subscribe<GuardianNotified>(_ => Press(SoundCue.Choose)),
                bus.Subscribe<GaugeWarningRaised>(_ => Play(SoundCue.Warning)),
                bus.Subscribe<GuardianPromptStarted>(_ => Play(SoundCue.Alert)),
                bus.Subscribe<TrackSnackEaten>(_ => Play(SoundCue.Eat)),
                bus.Subscribe<FoodEaten>(_ => Play(SoundCue.Eat)),
            };

            _wasFrozen = false;
            _lastCountdown = 0;
            _wasResult = false;
            _levelUpAt = -1f;

            return new Unbinder(() =>
            {
                foreach (var subscription in subscriptions) subscription.Dispose();
                foreach (var button in _buttons)
                {
                    if (button != null) button.onClick.RemoveListener(OnTap);
                }
                _buttons.Clear();
                _flow = null;
                _run = null;
            });
        }

        private void Update()
        {
            if (_flow == null) return;

            bool frozen = _run.IsWorldFrozen;
            if (frozen && !_wasFrozen) Play(SoundCue.Alert);
            _wasFrozen = frozen;

            double countdown = _run.ResumeCountdownRemaining;
            var countCue = CountdownCue(_lastCountdown, countdown);
            if (countCue != null) Play(countCue.Value);
            _lastCountdown = countdown;

            bool result = _flow.IsShowingResult;
            if (result && !_wasResult)
            {
                Play(SoundCue.Result);
                _levelUpAt = !_flow.IsTutorialResult && _flow.XpGained != null && _flow.LeveledUp
                    ? Time.unscaledTime + levelUpDelaySec
                    : -1f;
            }
            if (!result) _levelUpAt = -1f;
            _wasResult = result;

            if (_levelUpAt >= 0f && Time.unscaledTime >= _levelUpAt)
            {
                _levelUpAt = -1f;
                Play(SoundCue.LevelUp);
            }
        }

        private void OnTap() => Press(SoundCue.Tap);

        private void Press(SoundCue cue)
        {
            if (_pressFrame == Time.frameCount) return;
            _pressFrame = Time.frameCount;
            Play(cue);
        }

        private void Play(SoundCue cue)
        {
            var clip = SoundCues.Of(cues, cue);
            if (clip == null || _source == null) return;
            _source.PlayOneShot(clip, cues.volume);
        }

        private bool IsSkipped(Transform t)
        {
            if (skipButtonsUnder == null) return false;
            foreach (var root in skipButtonsUnder)
            {
                if (root != null && t.IsChildOf(root)) return true;
            }
            return false;
        }
    }
}
