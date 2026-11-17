using System.IO;
using NUnit.Framework;
using SugarDash.Domain.Config;
using SugarDash.Infrastructure.Config;
using UnityEngine;

namespace SugarDash.Infrastructure.Tests
{
    public class LevelConfigProviderTests
    {
        private const string BuildPath = "_Project/Config/level-config.json";
        private const string DevPath = "_Project/Config/Dev/level-config.dev.json";

        [Test]
        public void DevFile_Loads()
        {
            var c = new LevelConfigProvider(File.ReadAllText(Path.Combine(Application.dataPath, DevPath))).Load();

            Assert.That(c.Levels, Is.Not.Empty);
            Assert.That(c.ConfigVersion, Does.StartWith("dev-not-real"));
        }

        /// <summary>빌드 파일은 값이 정해지기 전에는 빈 값만 있어야 한다. 정한 값은 규칙에 맞아야 한다.</summary>
        [Test]
        public void BuildFile_LoadsOrHasOnlyMissingValues()
        {
            string json = File.ReadAllText(Path.Combine(Application.dataPath, BuildPath));
            try
            {
                new LevelConfigProvider(json).Load();
            }
            catch (ConfigValidationException e)
            {
                Assert.That(e.Violations, Is.Empty, string.Join(" | ", e.Violations));
                Assert.That(e.MissingPaths, Is.Not.Empty);
            }
        }

        [Test]
        public void NullValues_ReportedAsMissing()
        {
            const string json = @"{ ""configVersion"": ""t"", ""xp"": { ""perRun"": null, ""scoreRate"": 0 },
  ""levels"": [ { ""level"": 2, ""xpTotal"": null } ] }";

            var e = Assert.Throws<ConfigValidationException>(() => new LevelConfigProvider(json).Load());

            Assert.That(e.MissingPaths, Is.EquivalentTo(new[] { "xp.perRun", "levels[0].xpTotal" }));
        }

        [Test]
        public void Violation_FailsLoad()
        {
            const string json = @"{ ""configVersion"": ""t"", ""xp"": { ""perRun"": 10, ""scoreRate"": 0 },
  ""levels"": [ { ""level"": 2, ""xpTotal"": 20 }, { ""level"": 3, ""xpTotal"": 10 } ] }";

            var e = Assert.Throws<ConfigValidationException>(() => new LevelConfigProvider(json).Load());

            Assert.That(e.Violations, Has.Some.StartsWith("levels[1].xpTotal"));
        }

        [Test]
        public void UnknownKey_Fails()
        {
            const string json = @"{ ""configVersion"": ""t"", ""xp"": { ""perRun"": 10, ""scoreRate"": 0, ""typo"": 1 }, ""levels"": [] }";

            Assert.Throws<ConfigValidationException>(() => new LevelConfigProvider(json).Load());
        }
    }
}
