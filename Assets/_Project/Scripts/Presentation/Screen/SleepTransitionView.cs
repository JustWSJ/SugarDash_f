using System;
using SugarDash.Domain.Events;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 수면 전환. 하루가 넘어가 다음 날 첫 장면에 들어가면 화면을 바로 밤색(남색)으로 덮었다가 밝힌다.
    /// 덮개 가운데에 달이 떠 있다가, 달이 내려가며 사라진 뒤 해가 아래에서 올라오고, 해가 잠깐 머문 뒤에 덮개가 걷힌다.
    /// 바로 검게 하는 까닭: 같은 순간 SceneLook이 다음 날 맵으로 바꾸므로, 서서히 어두워지면 바뀐 맵이 먼저 보인다.
    /// 연출만이다. RunController를 읽지 않고 세계·혈당을 멈추지 않는다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class SleepTransitionView : MonoBehaviour
    {
        [Tooltip("전체 화면 덮개 이미지(raycastTarget 끔). 비워 두면 아무것도 바꾸지 않는다.")]
        [SerializeField] private Image fade;
        [Tooltip("덮개 색. 투명도는 코드가 정한다.")]
        [SerializeField] private Color fadeColor = new Color(0.16f, 0.2f, 0.33f);
        [Tooltip("덮개 위 달 그림. 처음 holdSec 동안 보이고, 바뀌는 시간의 앞 절반에 내려가며 사라진다.")]
        [SerializeField] private Image moon;
        [Tooltip("덮개 위 해 그림. 바뀌는 시간의 뒤 절반에 아래에서 올라오고, sunHoldSec 동안 머문 뒤 덮개와 함께 걷힌다.")]
        [SerializeField] private Image sun;
        [Tooltip("달이 지고 해가 뜨는 동안 움직이는 거리(화면 단위).")]
        [SerializeField] private float swapTravel = 80f;
        [Tooltip("달만 떠 있는 시간(초).")]
        [SerializeField] private float holdSec = 0.5f;
        [Tooltip("달이 지고 해가 뜨는 시간(초). 덮개는 계속 어둡다.")]
        [SerializeField] private float swapSec = 0.6f;
        [Tooltip("해가 다 뜬 뒤 덮개를 걷기 전까지 머무는 시간(초).")]
        [SerializeField] private float sunHoldSec = 0.6f;
        [Tooltip("덮개가 걷히는 시간(초).")]
        [SerializeField] private float fadeInSec = 0.8f;

        private int _lastDayIndex;
        private bool _fading;
        private float _elapsed;

        /// <summary>하루 번호가 올랐을 때만 암전한다. 새 주행(하루 번호가 0으로 돌아감)은 암전하지 않는다.</summary>
        public static bool ShouldFade(int lastDayIndex, int dayIndex) => dayIndex > lastDayIndex;

        /// <summary>
        /// 암전 시작부터 elapsed초 뒤의 검은 화면 투명도. hold 동안 1, fadeIn 동안 1→0, 그 뒤 0.
        /// </summary>
        public static float AlphaAt(float elapsed, float hold, float fadeIn)
        {
            if (elapsed < hold) return 1f;
            if (elapsed < hold + fadeIn) return 1f - (elapsed - hold) / fadeIn;
            return 0f;
        }

        /// <summary>
        /// 달의 몫(0~1). hold 동안 1, swap 동안 1→0(달과 해가 서로 바뀜), 그 뒤 0. 해의 몫은 1−이 값.
        /// </summary>
        public static float MoonShare(float elapsed, float hold, float swap)
        {
            if (elapsed < hold) return 1f;
            if (!(swap > 0f)) return 0f;
            return Mathf.Clamp01(1f - (elapsed - hold) / swap);
        }

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 연결을 해제한다.</summary>
        public IDisposable Bind(DomainEventBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            var subscription = bus.Subscribe<SceneEntered>(OnSceneEntered);
            return new Unbinder(() =>
            {
                subscription.Dispose();
                Stop();
                _lastDayIndex = 0;
            });
        }

        private void OnSceneEntered(SceneEntered e)
        {
            if (ShouldFade(_lastDayIndex, e.DayIndex))
            {
                _fading = true;
                _elapsed = 0f;
                SetAlpha(1f, 1f);
            }
            else if (e.DayIndex < _lastDayIndex)
            {
                Stop(); // 새 주행
            }
            _lastDayIndex = e.DayIndex;
        }

        private void Update()
        {
            if (!_fading) return;
            // 일시정지(timeScale 0) 중에는 암전도 멈춘다.
            _elapsed += Time.deltaTime;
            // 달 → 바뀜 → 해가 머묾까지 덮개는 어둡고, 그 뒤에 걷힌다.
            float dark = holdSec + swapSec + sunHoldSec;
            SetAlpha(AlphaAt(_elapsed, dark, fadeInSec), MoonShare(_elapsed, holdSec, swapSec));
            if (_elapsed >= dark + fadeInSec) Stop();
        }

        private void Stop()
        {
            _fading = false;
            SetAlpha(0f, 1f);
        }

        private void SetAlpha(float alpha, float moonShare)
        {
            if (fade == null) return;
            fade.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            // 겹치지 않게 차례로: 앞 절반에 달이 내려가며 사라지고, 뒤 절반에 해가 아래에서 올라오며 나타난다.
            float moonVis = Mathf.Clamp01(2f * moonShare - 1f);
            float sunVis = Mathf.Clamp01(1f - 2f * moonShare);
            Show(moon, alpha * moonVis, -(1f - moonVis) * swapTravel);
            Show(sun, alpha * sunVis, -(1f - sunVis) * swapTravel);
        }

        private static void Show(Image image, float alpha, float y)
        {
            if (image == null) return;
            var color = image.color;
            color.a = alpha;
            image.color = color;
            image.enabled = alpha > 0f;
            var rect = image.rectTransform;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }
    }
}
