using System;
using System.Collections.Generic;

namespace SugarDash.Domain.Level
{
    /// <summary>
    /// 플레이어 레벨 설정(docs/level.md). 두 유형이 같이 쓴다. 값은 게임 표현값이며 프로젝트 담당자가 정한다.
    /// </summary>
    public sealed class LevelConfig
    {
        public LevelConfig(string configVersion, double xpPerRun, double xpScoreRate, IReadOnlyList<LevelStep> levels)
        {
            ConfigVersion = configVersion;
            XpPerRun = xpPerRun;
            XpScoreRate = xpScoreRate;
            Levels = levels ?? throw new ArgumentNullException(nameof(levels));
        }

        public string ConfigVersion { get; }
        /// <summary>기록한 판이 끝날 때마다 받는 기본 경험치.</summary>
        public double XpPerRun { get; }
        /// <summary>그 판 점수에 곱해 더하는 경험치 비율.</summary>
        public double XpScoreRate { get; }
        /// <summary>레벨 2부터 차례로. 레벨 1은 경험치 0에서 시작하므로 적지 않는다.</summary>
        public IReadOnlyList<LevelStep> Levels { get; }
    }

    /// <summary>한 레벨에 오르는 데 필요한 누적 경험치.</summary>
    public sealed class LevelStep
    {
        public LevelStep(int level, int xpTotal)
        {
            Level = level;
            XpTotal = xpTotal;
        }

        public int Level { get; }
        /// <summary>이 레벨이 되는 누적 경험치.</summary>
        public int XpTotal { get; }
    }
}
