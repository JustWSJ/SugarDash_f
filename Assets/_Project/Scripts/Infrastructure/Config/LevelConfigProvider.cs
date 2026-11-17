using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SugarDash.Domain.Config;
using SugarDash.Domain.Level;
using SugarDash.Infrastructure.Config.Dto;

namespace SugarDash.Infrastructure.Config
{
    /// <summary>
    /// level-config.json을 읽어 <see cref="LevelConfig"/>로 바꾸고 불변식(LevelRules.Validate)을 검증한다.
    /// 비어 있는 값과 위반을 모두 모아 한 번에 실패시킨다(ConfigProvider와 같은 방식).
    /// </summary>
    public sealed class LevelConfigProvider
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error, // 오타 난 키를 잡는다
        };

        private readonly string _json;

        public LevelConfigProvider(string json) => _json = json ?? throw new ArgumentNullException(nameof(json));

        public LevelConfig Load()
        {
            LevelConfigDto dto;
            try
            {
                dto = JsonConvert.DeserializeObject<LevelConfigDto>(_json, Settings);
            }
            catch (JsonException e)
            {
                throw new ConfigValidationException(Array.Empty<string>(), new[] { "JSON 형식 오류: " + e.Message });
            }
            if (dto == null) throw new ConfigValidationException(new[] { "(root)" }, Array.Empty<string>());

            var missing = new List<string>();
            if (dto.ConfigVersion == null) missing.Add("configVersion");
            if (dto.Xp == null) missing.Add("xp");
            else
            {
                if (dto.Xp.PerRun == null) missing.Add("xp.perRun");
                if (dto.Xp.ScoreRate == null) missing.Add("xp.scoreRate");
            }

            var steps = new List<LevelStep>();
            if (dto.Levels == null) missing.Add("levels");
            else
            {
                for (int i = 0; i < dto.Levels.Count; i++)
                {
                    var s = dto.Levels[i];
                    if (s == null)
                    {
                        missing.Add($"levels[{i}]");
                        continue;
                    }
                    if (s.Level == null) missing.Add($"levels[{i}].level");
                    if (s.XpTotal == null) missing.Add($"levels[{i}].xpTotal");
                    steps.Add(new LevelStep(s.Level ?? 0, s.XpTotal ?? 0));
                }
            }
            if (missing.Count > 0) throw new ConfigValidationException(missing, Array.Empty<string>());

            var config = new LevelConfig(dto.ConfigVersion, dto.Xp.PerRun.Value, dto.Xp.ScoreRate.Value, steps);
            var violations = LevelRules.Validate(config);
            if (violations.Count > 0) throw new ConfigValidationException(Array.Empty<string>(), violations);
            return config;
        }
    }
}
