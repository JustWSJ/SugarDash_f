using NUnit.Framework;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using UnityEngine;

namespace SugarDash.Presentation.Tests
{
    public sealed class SoundCueTests
    {
        [Test]
        public void Countdown_StartsWithTick()
        {
            Assert.AreEqual(SoundCue.CountTick, SoundCueView.CountdownCue(0, 3.0));
        }

        [Test]
        public void Countdown_TicksOnEachNewSecond()
        {
            Assert.AreEqual(SoundCue.CountTick, SoundCueView.CountdownCue(2.01, 1.99));
            Assert.AreEqual(SoundCue.CountTick, SoundCueView.CountdownCue(1.01, 0.99));
        }

        [Test]
        public void Countdown_SilentInsideSameSecond()
        {
            Assert.IsNull(SoundCueView.CountdownCue(2.9, 2.5));
            Assert.IsNull(SoundCueView.CountdownCue(0, 0));
        }

        [Test]
        public void Countdown_GoWhenFinished()
        {
            Assert.AreEqual(SoundCue.CountGo, SoundCueView.CountdownCue(0.02, 0));
        }

        [Test]
        public void Countdown_LongerRestartTicks()
        {
            Assert.AreEqual(SoundCue.CountTick, SoundCueView.CountdownCue(0.5, 3.0));
        }

        [Test]
        public void Pick_TimeoutIsSilent()
        {
            var timeout = new StepAnswered(1, HypoStep.Respond, null, false, HypoErrorCode.RsTimeout, false, 10, false, 0, 0);
            var juice = new StepAnswered(1, HypoStep.Respond, "juice", true, HypoErrorCode.None, false, 2, false, 0, 0);
            Assert.IsFalse(SoundCueView.IsPick(timeout));
            Assert.IsTrue(SoundCueView.IsPick(juice));
            Assert.IsFalse(SoundCueView.IsPick(null));
        }

        [Test]
        public void Clip_EmptySlotsAreSilent()
        {
            Assert.IsNull(SoundCues.Of(null, SoundCue.Tap));
            var asset = ScriptableObject.CreateInstance<SoundCues>();
            try
            {
                Assert.IsNull(SoundCues.Of(asset, SoundCue.Result));
                Assert.IsNull(SoundCues.Of(asset, SoundCue.Eat));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }
    }
}
