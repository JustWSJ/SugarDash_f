using NUnit.Framework;
using SugarDash.App;
using SugarDash.Domain.Config;

namespace SugarDash.Presentation.Tests
{
    public class TutorialTextTests
    {
        // TutorialController의 안내 순서(비공개)와 같게 둔다.
        private static readonly TutorialStep[] Type1Order =
        {
            TutorialStep.MoveLeft, TutorialStep.MoveRight, TutorialStep.Jump, TutorialStep.Slide, TutorialStep.Snack,
            TutorialStep.HypoRecognize, TutorialStep.HypoRespond, TutorialStep.HypoDose, TutorialStep.HypoRemeasure,
            TutorialStep.HypoNotify, TutorialStep.HyperNotify, TutorialStep.Finish,
        };

        private static readonly TutorialStep[] Type2Order =
        {
            TutorialStep.MoveLeft, TutorialStep.MoveRight, TutorialStep.Jump, TutorialStep.Slide, TutorialStep.FoodRow,
            TutorialStep.Rest, TutorialStep.HighNotice, TutorialStep.HighTell, TutorialStep.LowNotice,
            TutorialStep.LowTell, TutorialStep.Finish,
        };

        private static readonly string[] Forbidden = { "나쁜", "죽", "생존", "치료" };

        [Test]
        public void EveryStepHasText_BothTypes()
        {
            foreach (var step in Type1Order) Assert.That(TutorialText.For(step, TrackType.Type1), Is.Not.Empty, step.ToString());
            foreach (var step in Type2Order) Assert.That(TutorialText.For(step, TrackType.Type2), Is.Not.Empty, step.ToString());
        }

        [Test]
        public void FixedTexts_AndShared()
        {
            // 확정 문구만 글자까지 확인한다.
            Assert.That(TutorialText.TryAgain, Is.EqualTo("다시 해봐요"));
            Assert.That(TutorialText.Skip, Is.EqualTo("건너뛰기"));
            Assert.That(TutorialText.SettingsButton, Is.EqualTo("튜토리얼 다시하기"));
            Assert.That(TutorialText.TapHint, Is.Not.Empty);
            Assert.That(TutorialText.For(TutorialStep.Jump, TrackType.Type2), Is.EqualTo(TutorialText.For(TutorialStep.Jump, TrackType.Type1)));
            Assert.That(TutorialText.For(TutorialStep.HighTell, TrackType.Type2), Is.EqualTo(TutorialText.For(TutorialStep.LowTell, TrackType.Type2)));
            Assert.That(TutorialText.For(TutorialStep.HighTell, TrackType.Type2), Does.Contain("어른에게 알리기"));
        }

        [Test]
        public void NoForbiddenWords()
        {
            var all = new System.Collections.Generic.List<string>
            {
                TutorialText.TryAgain, TutorialText.TapHint, TutorialText.Skip, TutorialText.SettingsButton,
                ResultScreenText.TutorialTitle,
            };
            foreach (var step in Type1Order) all.Add(TutorialText.For(step, TrackType.Type1));
            foreach (var step in Type2Order) all.Add(TutorialText.For(step, TrackType.Type2));
            foreach (var text in all)
                foreach (var word in Forbidden) Assert.That(text, Does.Not.Contain(word), text);
        }

        private static readonly string[] FoodIds = { "water", "juice", "soda", "apple", "chicken", "pizza", "burger", "cracker" };

        [Test]
        public void Pages_LastPageIsStepText_AndFoodsKnown()
        {
            foreach (var (order, type) in new[] { (Type1Order, TrackType.Type1), (Type2Order, TrackType.Type2) })
            foreach (var step in order)
            {
                var pages = TutorialText.Pages(step, type);
                Assert.That(pages, Is.Not.Empty, step.ToString());
                Assert.That(pages[pages.Count - 1].Text, Is.EqualTo(TutorialText.For(step, type)), step.ToString());
                foreach (var page in pages)
                {
                    Assert.That(page.Text, Is.Not.Empty);
                    foreach (var word in Forbidden) Assert.That(page.Text, Does.Not.Contain(word), page.Text);
                    foreach (var id in page.FoodIds) Assert.That(FoodIds, Does.Contain(id), page.Text);
                }
            }
        }

        [Test]
        public void Pages_SnackGuides()
        {
            var type1 = TutorialText.Pages(TutorialStep.Snack, TrackType.Type1);
            Assert.That(type1.Count, Is.EqualTo(3));
            Assert.That(type1[0].FoodIds, Is.EquivalentTo(new[] { "juice", "soda" }), "1형 빠른 당");
            Assert.That(type1[1].FoodIds, Is.EquivalentTo(new[] { "apple", "cracker" }), "1형 느린 당");

            var type2 = TutorialText.Pages(TutorialStep.FoodRow, TrackType.Type2);
            Assert.That(type2.Count, Is.EqualTo(5));
            var shown = new System.Collections.Generic.HashSet<string>();
            foreach (var page in type2) shown.UnionWith(page.FoodIds);
            Assert.That(shown, Is.EquivalentTo(FoodIds), "2형은 음식 8종을 모두 설명한다");
            foreach (var page in type2) Assert.That(page.Text, Does.Not.Contain("당이에요"), "2형은 빠른 당·느린 당 말을 쓰지 않는다");

            var respond = TutorialText.Pages(TutorialStep.HypoRespond, TrackType.Type1);
            Assert.That(respond.Count, Is.EqualTo(1));
            Assert.That(respond[0].FoodIds, Is.EquivalentTo(new[] { "juice", "soda" }));
        }

        [Test]
        public void HasNextPage()
        {
            Assert.That(TutorialPromptView.HasNextPage(0, 3), Is.True);
            Assert.That(TutorialPromptView.HasNextPage(1, 3), Is.True);
            Assert.That(TutorialPromptView.HasNextPage(2, 3), Is.False);
            Assert.That(TutorialPromptView.HasNextPage(0, 1), Is.False);
        }

        [Test]
        public void PageButtons()
        {
            Assert.That(TutorialPromptView.HasPrevPage(0, 3), Is.False, "첫 장에는 앞 장 버튼이 없다");
            Assert.That(TutorialPromptView.HasPrevPage(1, 3), Is.True);
            Assert.That(TutorialPromptView.HasPrevPage(2, 3), Is.True);
            Assert.That(TutorialPromptView.HasPrevPage(0, 1), Is.False);
            Assert.That(TutorialPromptView.PageLabel(0, 3), Is.EqualTo("1 / 3"));
            Assert.That(TutorialPromptView.PageLabel(2, 5), Is.EqualTo("3 / 5"));
            Assert.That(TutorialPromptView.PageLabel(0, 1), Is.Empty, "한 장이면 쪽 번호 없음");
        }
    }
}
