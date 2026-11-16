namespace SugarDash.Domain.Config
{
    /// <summary>표시 혈당 설정.</summary>
    public sealed class DisplayConfig
    {
        public DisplayConfig(double delaySec, double warningThreshold, double barGreenFrom)
        {
            DelaySec = delaySec;
            WarningThreshold = warningThreshold;
            BarGreenFrom = barGreenFrom;
        }

        /// <summary>표시 혈당이 실제 혈당보다 늦는 시간(게임 초).</summary>
        public double DelaySec { get; }
        /// <summary>1형 게이지 경고 기준. 2형은 게이지 경고가 없어 음의 무한대.</summary>
        public double WarningThreshold { get; }
        /// <summary>
        /// 혈당바가 초록이 되기 시작하는 값. hypoThreshold(빨강)에서 이 값(초록)까지 빨강 → 노랑 → 초록으로 바뀐다.
        /// 위쪽은 normalRangeMax(초록)에서 hyperThreshold(빨강)까지 바뀐다. 게임 표현값이다.
        /// </summary>
        public double BarGreenFrom { get; }
    }
}
