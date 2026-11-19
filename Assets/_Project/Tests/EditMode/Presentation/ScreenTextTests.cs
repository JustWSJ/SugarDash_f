using NUnit.Framework;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;

namespace SugarDash.Presentation.Tests
{
    public class ScreenTextTests
    {
        private static readonly string[] Forbidden = { "나쁜", "죽", "생존", "치료" };

        [Test]
        public void AllTextsPresent_NoForbiddenWords()
        {
            var all = new[]
            {
                ScreenText.GameTitle, ScreenText.Start, ScreenText.Settings, ScreenText.TypeSelectTitle,
                ScreenText.TypeSelectGuide, ScreenText.TypeSelectNote, ScreenText.SettingsTitle, ScreenText.CurrentType,
                ScreenText.IntroReplay, ScreenText.Back, ScreenText.Continue, ScreenText.Quit, ScreenText.Retry,
                ScreenText.ResultQuit, ScreenText.Meter, ScreenText.GaugeWarning,
                ScreenText.TypeName(TrackType.Type1), ScreenText.TypeName(TrackType.Type2),
                ScreenText.Signal(NoticeKind.High), ScreenText.Signal(NoticeKind.Low),
            };
            foreach (var text in all)
            {
                Assert.That(text, Is.Not.Empty);
                foreach (var word in Forbidden) Assert.That(text, Does.Not.Contain(word), text);
            }
        }

        [Test]
        public void TypeNameAndSignal_EmptyWhenNone()
        {
            Assert.That(ScreenText.TypeName(null), Is.Empty);
            Assert.That(ScreenText.Signal(null), Is.Empty);
            Assert.That(ScreenText.TypeName(TrackType.Type1), Is.Not.EqualTo(ScreenText.TypeName(TrackType.Type2)));
            Assert.That(ScreenText.Signal(NoticeKind.High), Is.Not.EqualTo(ScreenText.Signal(NoticeKind.Low)));
        }
    }
}
