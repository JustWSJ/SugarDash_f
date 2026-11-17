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
        public void BarColor_GreenInRange_RedAtThresholds_YellowHalfway()
        {
            // 픽스처: hypoThreshold 70, barGreenFrom 90, normalRangeMax 180, hyperThreshold 250.
            var builder = new ConfigBuilder { NormalRangeMax = 180 };
            var levels = builder.Build().Glucose.Levels;
            Color Of(double g) => HudPresenter.BarColor(g, levels, builder.BarGreenFrom, Color.red, Color.yellow, Color.green);

            Assert.That(Of(60), Is.EqualTo(Color.red));
            Assert.That(Of(70), Is.EqualTo(Color.red));
            Assert.That(Of(80), Is.EqualTo(Color.yellow));
            Assert.That(Of(90), Is.EqualTo(Color.green));
            Assert.That(Of(180), Is.EqualTo(Color.green));
            Assert.That(Of(215), Is.EqualTo(Color.yellow));
            Assert.That(Of(250), Is.EqualTo(Color.red));
            Assert.That(Of(300), Is.EqualTo(Color.red));
            // 사이 값은 섞인 색이다.
            Assert.That(Of(75).g, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(Of(200).r, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void Paused_NotEmpty()
        {
            Assert.That(HudText.Paused, Is.Not.Empty);
        }
    }
}
