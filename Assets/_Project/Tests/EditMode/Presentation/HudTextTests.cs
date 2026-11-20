using NUnit.Framework;
using SugarDash.Domain.Tests;
using UnityEngine;

namespace SugarDash.Presentation.Tests
{
    public class HudTextTests
    {
        [Test]
        public void Measured_ShowsRoundedNumber()
        {
            var text = HudText.Measured(63.4);
            Assert.That(text, Does.Contain("63"));
            Assert.That(text, Does.Not.Contain("."));
        }

        [Test]
        public void Score_ShowsNumber()
        {
            Assert.That(HudText.Score(1234), Does.Contain("1234"));
        }

        [Test]
        public void ChoiceTimeBar_ClampsFraction()
        {
            Assert.That(HypoChoicePanel.TimeBarFill(0.25), Is.EqualTo(0.25f));
            Assert.That(HypoChoicePanel.TimeBarFill(1.5), Is.EqualTo(1f));
            Assert.That(HypoChoicePanel.TimeBarFill(-1), Is.EqualTo(0f));
        }

        [Test]
        public void BarColor_GreenInRange_YellowNearRange_RedPastThresholds()
        {
            // 픽스처: hypoThreshold 70, barGreenFrom 90, normalRangeMax 180, hyperThreshold 250.
            var builder = new ConfigBuilder { NormalRangeMax = 180 };
            var levels = builder.Build().Glucose.Levels;
            Color Of(double g) => HudPresenter.BarColor(g, levels, builder.BarGreenFrom, Color.red, Color.yellow, Color.green);

            Assert.That(Of(60), Is.EqualTo(Color.red));
            Assert.That(Of(69.9), Is.EqualTo(Color.red));
            Assert.That(Of(70), Is.EqualTo(Color.yellow));
            Assert.That(Of(89.9), Is.EqualTo(Color.yellow));
            Assert.That(Of(90), Is.EqualTo(Color.green));
            Assert.That(Of(180), Is.EqualTo(Color.green));
            // 정상 범위를 넘으면 바로 초록이 아니다(높은데 좋아 보이지 않게).
            Assert.That(Of(180.1), Is.EqualTo(Color.yellow));
            Assert.That(Of(250), Is.EqualTo(Color.yellow));
            Assert.That(Of(250.1), Is.EqualTo(Color.red));
            Assert.That(Of(300), Is.EqualTo(Color.red));
        }

        [Test]
        public void Paused_NotEmpty()
        {
            Assert.That(HudText.Paused, Is.Not.Empty);
        }
    }
}
