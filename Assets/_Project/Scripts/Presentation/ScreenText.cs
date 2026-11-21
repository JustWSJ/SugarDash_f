using SugarDash.Domain.Config;
using SugarDash.Domain.Director;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 타이틀, 유형 선택, 설정, 일시정지, 결과 버튼, HUD, 2형 몸 신호의 화면 글자(docs/handoff/screen-text.md).
    /// 화면 코드가 시작할 때 프리팹 글자 칸에 넣는다. 치료·진단을 주장하거나 아이를 탓하는 말은 쓰지 않는다.
    /// </summary>
    // [문구 파일] 따옴표 안의 글자만 바꿀 수 있다. 상수 이름, 메서드, 코드 구조는 바꾸지 않고 `// 확정` 줄은 그대로 둔다(docs/team-rules.md 2.3절).
    public static class ScreenText
    {
        // 타이틀
        public const string GameTitle = "슈가대시";
        public const string Start = "시작";
        public const string Settings = "설정";

        // 유형 선택
        public const string TypeSelectTitle = "어떤 당뇨가 있나요?";
        public const string TypeSelectGuide = "보호자와 함께 골라 주세요";
        public const string TypeSelectNote = "나중에 설정에서 바꿀 수 있어요";

        /// <summary>유형 이름. 타이틀 유형 띠, 유형 선택·설정 버튼이 쓴다. 유형이 없으면 빈 글자.</summary>
        public static string TypeName(TrackType? type)
        {
            if (type == TrackType.Type1) return "1형";
            if (type == TrackType.Type2) return "2형";
            return "";
        }

        // 설정
        public const string SettingsTitle = "설정";
        public const string CurrentType = "지금";
        public const string IntroReplay = "인트로 다시보기";
        public const string Back = "돌아가기";

        // 일시정지 메뉴(멈춤 글자는 HudText.Paused)
        public const string Continue = "계속하기";
        public const string Quit = "그만하기";

        // 결과 화면 버튼
        public const string Retry = "다시하기";
        public const string ResultQuit = "그만하기";

        // 주행 HUD(1형)
        public const string Meter = "측정기";
        public const string GaugeWarning = "혈당 경고!";

        /// <summary>2형 높음 신호일 때 캐릭터 머리 위 말풍선(지친 모습, docs/notice-evidence.md).</summary>
        public const string Tired = "지쳤어요";

        /// <summary>2형 몸 신호 말풍선 글자(docs/notice-evidence.md). 신호가 없으면 빈 글자.</summary>
        public static string Signal(NoticeKind? signal)
        {
            if (signal == NoticeKind.High) return "목말라요";
            if (signal == NoticeKind.Low) return "몸이 떨려요";
            return "";
        }
    }
}
