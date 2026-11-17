using System.Linq;
using NUnit.Framework;
using SugarDash.Domain.Level;

namespace SugarDash.Domain.Tests
{
    public class LevelRulesTests
    {
        // 테스트 전용 레벨 표. 실제 게임 수치가 아니다.
        private static LevelConfig Config(double perRun = 10, double rate = 0.1, params LevelStep[] steps) =>
            new LevelConfig("test", perRun, rate, steps.Length > 0
                ? steps
                : new[] { new LevelStep(2, 20), new LevelStep(3, 60), new LevelStep(4, 120) });

        [Test]
        public void XpForRun_BasePlusScoreRate_Floored()
        {
            var c = Config();
            Assert.That(LevelRules.XpForRun(155, c), Is.EqualTo(25));
            Assert.That(LevelRules.XpForRun(null, c), Is.EqualTo(10), "점수가 없으면 기본만");
            Assert.That(LevelRules.XpForRun(-50, c), Is.EqualTo(10), "음수 점수로 깎지 않는다");
        }

        [Test]
        public void AddXp_NeverDecreases_StopsAtMax()
        {
            Assert.That(LevelRules.AddXp(5, 10), Is.EqualTo(15));
            Assert.That(LevelRules.AddXp(5, -3), Is.EqualTo(5));
            Assert.That(LevelRules.AddXp(int.MaxValue - 1, 10), Is.EqualTo(int.MaxValue));
        }

        [TestCase(0, 1)]
        [TestCase(19, 1)]
        [TestCase(20, 2)]
        [TestCase(59, 2)]
        [TestCase(60, 3)]
        [TestCase(500, 4)]
        public void LevelOf_Boundaries(int xp, int level)
        {
            Assert.That(LevelRules.LevelOf(xp, Config()), Is.EqualTo(level));
        }

        [Test]
        public void LevelStartAndNext()
        {
            var c = Config();
            Assert.That(LevelRules.LevelStartXp(1, c), Is.EqualTo(0));
            Assert.That(LevelRules.NextLevelXp(1, c), Is.EqualTo(20));
            Assert.That(LevelRules.LevelStartXp(3, c), Is.EqualTo(60));
            Assert.That(LevelRules.NextLevelXp(3, c), Is.EqualTo(120));
            Assert.That(LevelRules.NextLevelXp(4, c), Is.Null, "마지막 레벨");
        }

        [Test]
        public void Validate_Valid_NoViolations()
        {
            Assert.That(LevelRules.Validate(Config()), Is.Empty);
        }

        [Test]
        public void Validate_CatchesBadValues()
        {
            var c = Config(-1, double.PositiveInfinity,
                new LevelStep(2, 20),
                new LevelStep(4, 20));

            var v = LevelRules.Validate(c);

            Assert.That(v.Any(x => x.StartsWith("xp.perRun")), Is.True);
            Assert.That(v.Any(x => x.StartsWith("xp.scoreRate")), Is.True);
            Assert.That(v.Any(x => x.StartsWith("levels[1].level")), Is.True, "레벨은 2부터 1씩");
            Assert.That(v.Any(x => x.StartsWith("levels[1].xpTotal")), Is.True, "필요 경험치는 점점 커진다");
        }

        [Test]
        public void Validate_EmptyLevels()
        {
            var c = new LevelConfig("test", 10, 0, new LevelStep[0]);
            Assert.That(LevelRules.Validate(c), Has.Some.StartsWith("levels가 비어"));
        }
    }
}
