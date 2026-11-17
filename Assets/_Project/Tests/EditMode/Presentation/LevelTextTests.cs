using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class LevelTextTests
    {
        [Test]
        public void ResultLines()
        {
            Assert.That(LevelText.ResultLines(null, false, 1), Is.Empty, "경험치가 없으면 줄이 없다");
            Assert.That(LevelText.ResultLines(12, false, 1), Is.EqualTo("경험치 +12"));
            Assert.That(LevelText.ResultLines(12, true, 3), Is.EqualTo("경험치 +12\n레벨이 올랐어요! 레벨 3"));
        }

        [Test]
        public void BadgeTexts()
        {
            Assert.That(LevelText.Level(2), Is.EqualTo("2"));
            Assert.That(LevelText.Progress(25, 60), Is.EqualTo("경험치 25 / 60"));
            Assert.That(LevelText.Progress(300, null), Is.EqualTo("경험치 300"), "마지막 레벨");
        }

        [TestCase(20, 20, 60, 0f)]
        [TestCase(40, 20, 60, 0.5f)]
        [TestCase(59, 20, 60, 0.975f)]
        [TestCase(300, 200, null, 1f)]
        public void XpFillAmount(int total, int start, int? next, float expected)
        {
            Assert.That(TitleScreenView.XpFillAmount(total, start, next), Is.EqualTo(expected).Within(1e-4));
        }
    }
}
