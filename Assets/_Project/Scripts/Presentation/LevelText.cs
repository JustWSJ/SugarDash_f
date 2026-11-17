using System.Collections.Generic;

namespace SugarDash.Presentation
{
    /// <summary>레벨·경험치 글자(docs/level.md). 타이틀의 레벨 표시와 결과 화면이 쓴다.</summary>
    // [문구 파일] 따옴표 안의 글자만 바꿀 수 있다. 상수 이름, 메서드, 코드 구조는 바꾸지 않고 `// 확정` 줄은 그대로 둔다(docs/team-rules.md 2.3절).
    public static class LevelText
    {
        /// <summary>타이틀 왼쪽 위 원 안의 레벨 글자.</summary>
        public static string Level(int level) => $"{level}";

        /// <summary>원 옆 상자의 경험치 글자. 마지막 레벨이면 누적 경험치만.</summary>
        public static string Progress(int totalXp, int? nextLevelXp) =>
            nextLevelXp == null ? $"경험치 {totalXp}" : $"경험치 {totalXp} / {nextLevelXp}";

        /// <summary>결과 화면의 경험치 줄. 레벨이 오르면 줄을 더한다. 경험치가 없으면(튜토리얼, 레벨 꺼짐) 빈 글자.</summary>
        public static string ResultLines(int? xpGained, bool leveledUp, int level)
        {
            if (xpGained == null) return "";
            var lines = new List<string> { $"경험치 +{xpGained}" };
            if (leveledUp) lines.Add($"레벨이 올랐어요! 레벨 {level}");
            return string.Join("\n", lines);
        }
    }
}
