using NUnit.Framework;
using SugarDash.Domain.Hypo;
using UnityEngine;

namespace SugarDash.Presentation.Tests
{
    public class PictureSlotTests
    {
        private static Sprite MakeSprite() =>
            Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.zero);

        [Test]
        public void ChoicePictureSet_FindsFoodById()
        {
            var juice = MakeSprite();
            var set = new ChoicePictureSet();
            set.foods.Add(new FoodPicture { foodId = "juice", sprite = juice });

            Assert.That(set.Food("juice"), Is.SameAs(juice));
            Assert.That(set.Food("apple"), Is.Null, "없는 음식은 그림 없이");
            Assert.That(set.Food(null), Is.Null);
            Assert.That(new ChoicePictureSet { foods = null }.Food("juice"), Is.Null);
        }

        [Test]
        public void ChoicePictureSet_ActionAndDosePictures()
        {
            var set = new ChoicePictureSet
            {
                keepRunning = MakeSprite(), noSugar = MakeSprite(), recheck = MakeSprite(), runAgain = MakeSprite(),
                tellAdult = MakeSprite(), doseUnder = MakeSprite(), doseProper = MakeSprite(), doseOver = MakeSprite(),
            };

            Assert.That(set.Of(ChoicePicture.KeepRunning), Is.SameAs(set.keepRunning));
            Assert.That(set.Of(ChoicePicture.NoSugar), Is.SameAs(set.noSugar));
            Assert.That(set.Of(ChoicePicture.Recheck), Is.SameAs(set.recheck));
            Assert.That(set.Of(ChoicePicture.RunAgain), Is.SameAs(set.runAgain));
            Assert.That(set.Of(ChoicePicture.TellAdult), Is.SameAs(set.tellAdult));
            Assert.That(set.Dose(DoseLevel.Under), Is.SameAs(set.doseUnder));
            Assert.That(set.Dose(DoseLevel.Proper), Is.SameAs(set.doseProper));
            Assert.That(set.Dose(DoseLevel.Over), Is.SameAs(set.doseOver));
            Assert.That(new ChoicePictureSet().Of(ChoicePicture.TellAdult), Is.Null, "빈 칸이면 글자만");
        }

        [Test]
        public void ChoicePictures_NullAssetGivesEmptySet()
        {
            Assert.That(ChoicePictures.Of(null), Is.Not.Null);
            Assert.That(ChoicePictures.Of(null).Food("juice"), Is.Null);
        }

        [Test]
        public void ChoiceButton_ShowsPictureOnlyWithSprite()
        {
            Assert.That(ChoiceButtonView.ShowsPicture(null), Is.False);
            Assert.That(ChoiceButtonView.ShowsPicture(MakeSprite()), Is.True);
        }

        [TestCase(0f, 0f)]
        [TestCase(1f / 32f, 6f)]
        [TestCase(1f / 16f, 0f)]
        [TestCase(3f / 32f, -6f)]
        public void NoticeSignal_ShakeOffset(float time, float expected)
        {
            Assert.That(NoticeSignalView.ShakeOffset(time, 6f, 8f), Is.EqualTo(expected).Within(1e-3f));
        }

        [TestCase(0f, 1f)]
        [TestCase(0.3f, 1f)]
        [TestCase(0.55f, 1.3f)]
        [TestCase(0.8f, 1f)]
        [TestCase(2f, 1f)]
        public void ResultLevelUp_PopScale(float elapsed, float expected)
        {
            Assert.That(ResultScreenView.PopScaleAt(elapsed, 0.3f, 0.5f, 1.3f), Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void ResultLevelUp_PopScale_ZeroDuration()
        {
            Assert.That(ResultScreenView.PopScaleAt(0.5f, 0f, 0f, 1.3f), Is.EqualTo(1f));
        }

        [Test]
        public void LevelUpBadge_NotEmpty()
        {
            Assert.That(LevelText.LevelUpBadge, Is.Not.Empty);
        }
    }
}
