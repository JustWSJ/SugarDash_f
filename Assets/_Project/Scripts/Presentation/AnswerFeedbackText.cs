using System;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 선택 뒤 결과 문구와 재개 카운트다운 숫자. 문구는 무엇을 골랐는지만 말하고 정답·오답은 말하지 않는다.
    /// </summary>
    // [문구 파일] 따옴표 안의 글자만 바꿀 수 있다. 상수 이름, 메서드, 코드 구조는 바꾸지 않고 `// 확정` 줄은 그대로 둔다(docs/team-rules.md 2.3절).
    public static class AnswerFeedbackText
    {
        private const string Notified = "어른에게 알렸어요";

        /// <summary>
        /// 고른 뒤 문구. 2단계(먹을 것 고르기)의 간식은 모두 "○○를 골랐어요"이고, 먹은 것은 3단계 양 문구가 말한다.
        /// doseSnackId는 3단계에서 고른 간식 id(양 문구에 이름을 넣는다). 없으면 이름 없이 말한다.
        /// </summary>
        public static string For(StepAnswered e, string doseSnackId = null)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (e.HelpInstead) return Notified;
            if (e.Step == HypoStep.Respond && IsSnackId(e.ChoiceId)) return Picked(e.ChoiceId);
            if (e.Step == HypoStep.Dose && !string.IsNullOrEmpty(doseSnackId))
            {
                string amount = DoseAmount(e.ChoiceId);
                if (amount != null) return $"{FoodText.WithObjectParticle(doseSnackId)} {amount} {(FoodText.IsDrink(doseSnackId) ? "마셨어요" : "먹었어요")}";
            }

            switch (e.ChoiceId)
            {
                case null: return "시간이 다 됐어요";
                case "meter": return "측정했어요";
                case "keep_running": return "계속 달리기를 골랐어요";
                case "no_sugar": return "먹을 게 없다고 했어요";
                case "under": return "조금 먹었어요";
                case "proper": return "알맞게 먹었어요";
                case "over": return "많이 먹었어요";
                case "recheck": return "다시 쟀어요";
                case "run_again": return "다시 달리기를 골랐어요";
                case "juice": return "주스를 마셨어요";
                case "help": return Notified;
                default: return Picked(e.ChoiceId); // 간식 id
            }
        }

        private static string Picked(string snackId) => $"{FoodText.WithObjectParticle(snackId)} 골랐어요";

        // 간식 id가 아닌 선택지 id들.
        private static bool IsSnackId(string choiceId)
        {
            switch (choiceId)
            {
                case null:
                case "meter":
                case "keep_running":
                case "no_sugar":
                case "under":
                case "proper":
                case "over":
                case "recheck":
                case "run_again":
                case "help":
                    return false;
                default:
                    return true;
            }
        }

        private static string DoseAmount(string choiceId)
        {
            switch (choiceId)
            {
                case "under": return "조금";
                case "proper": return "알맞게";
                case "over": return "많이";
                default: return null;
            }
        }

        public static string ForGuardianNotified() => Notified;

        /// <summary>카운트다운에 보여 줄 숫자(남은 2.01초면 3, 2.0초면 2). 남은 시간이 없으면 null.</summary>
        public static int? CountdownNumber(double remaining) =>
            remaining > 0 ? (int)Math.Ceiling(remaining) : (int?)null;
    }
}
