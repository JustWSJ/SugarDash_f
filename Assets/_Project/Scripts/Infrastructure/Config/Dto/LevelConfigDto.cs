using System.Collections.Generic;
using UnityEngine.Scripting;

namespace SugarDash.Infrastructure.Config.Dto
{
    // level-config.json에 대응하는 DTO. 값이 없음(PLACEHOLDER)은 nullable로 표현한다.
    [Preserve]
    public sealed class LevelConfigDto
    {
        public string ConfigVersion { get; set; }
        public LevelXpDto Xp { get; set; }
        public List<LevelStepDto> Levels { get; set; }
    }

    [Preserve]
    public sealed class LevelXpDto
    {
        public double? PerRun { get; set; }
        public double? ScoreRate { get; set; }
    }

    [Preserve]
    public sealed class LevelStepDto
    {
        public int? Level { get; set; }
        public int? XpTotal { get; set; }
    }
}
