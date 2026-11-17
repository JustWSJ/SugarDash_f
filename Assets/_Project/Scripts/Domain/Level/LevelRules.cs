using System;
using System.Collections.Generic;

namespace SugarDash.Domain.Level
{
    /// <summary>
    /// 경험치와 레벨 계산(docs/level.md). 경험치는 기록한 판이 끝날 때만 더하고 깎지 않는다.
    /// 레벨은 저장하지 않고 누적 경험치와 레벨 표로 매번 계산한다.
    /// </summary>
    public static class LevelRules
    {
        /// <summary>한 판의 경험치: 기본 + 점수 × 비율(내림). 점수가 없거나 음수면 기본만.</summary>
        public static int XpForRun(double? score, LevelConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            double s = score.HasValue && score.Value > 0 ? score.Value : 0;
            double xp = Math.Floor(config.XpPerRun + s * config.XpScoreRate);
            return xp >= int.MaxValue ? int.MaxValue : (int)xp;
        }

        /// <summary>누적 경험치에 더한다. 넘치면 int 최댓값에서 멈춘다.</summary>
        public static int AddXp(int totalXp, int gained)
        {
            long sum = (long)Math.Max(0, totalXp) + Math.Max(0, gained);
            return sum >= int.MaxValue ? int.MaxValue : (int)sum;
        }

        /// <summary>누적 경험치의 레벨. 레벨 1에서 시작해 필요 경험치를 넘은 단계마다 하나씩 오른다.</summary>
        public static int LevelOf(int totalXp, LevelConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            int level = 1;
            foreach (var step in config.Levels)
            {
                if (totalXp < step.XpTotal) break;
                level = step.Level;
            }
            return level;
        }

        /// <summary>지금 레벨이 시작된 누적 경험치(레벨 1은 0).</summary>
        public static int LevelStartXp(int level, LevelConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            int start = 0;
            foreach (var step in config.Levels)
            {
                if (step.Level > level) break;
                start = step.XpTotal;
            }
            return start;
        }

        /// <summary>다음 레벨이 되는 누적 경험치. 마지막 레벨이면 null.</summary>
        public static int? NextLevelXp(int level, LevelConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            foreach (var step in config.Levels)
                if (step.Level > level) return step.XpTotal;
            return null;
        }

        /// <summary>
        /// 레벨 설정의 불변식. 경험치 값은 0 이상의 유한수, 레벨은 2부터 1씩, 필요 경험치는 0보다 크고 점점 커진다.
        /// </summary>
        public static List<string> Validate(LevelConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            var violations = new List<string>();
            if (!(config.XpPerRun >= 0) || double.IsInfinity(config.XpPerRun))
                violations.Add($"xp.perRun은 0 이상의 유한수여야 한다 ({config.XpPerRun})");
            if (!(config.XpScoreRate >= 0) || double.IsInfinity(config.XpScoreRate))
                violations.Add($"xp.scoreRate는 0 이상의 유한수여야 한다 ({config.XpScoreRate})");
            if (config.Levels.Count == 0) violations.Add("levels가 비어 있다");

            int prevXp = 0;
            for (int i = 0; i < config.Levels.Count; i++)
            {
                var step = config.Levels[i];
                if (step == null)
                {
                    violations.Add($"levels[{i}]가 비어 있다");
                    continue;
                }
                if (step.Level != i + 2) violations.Add($"levels[{i}].level은 {i + 2}이어야 한다 ({step.Level})");
                if (step.XpTotal <= prevXp)
                    violations.Add($"levels[{i}].xpTotal은 앞 레벨({prevXp})보다 커야 한다 ({step.XpTotal})");
                prevXp = Math.Max(prevXp, step.XpTotal);
            }
            return violations;
        }
    }
}
