using System.Linq;
using NUnit.Framework;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Director;
using UnityEngine;

namespace SugarDash.Presentation.Tests
{
    public class SkeletonStateTests
    {
        [Test]
        public void CharacterState_Priority()
        {
            Assert.That(CharacterStateView.StateOf(false, null), Is.EqualTo(CharacterState.Normal));
            Assert.That(CharacterStateView.StateOf(true, null), Is.EqualTo(CharacterState.Resting));
            Assert.That(CharacterStateView.StateOf(true, NoticeKind.High), Is.EqualTo(CharacterState.TiredHigh));
            Assert.That(CharacterStateView.StateOf(true, NoticeKind.Low), Is.EqualTo(CharacterState.TiredLow));
            Assert.That(CharacterStateView.StateOf(false, NoticeKind.High), Is.EqualTo(CharacterState.TiredHigh));
            Assert.That(CharacterStateView.StateOf(false, NoticeKind.Low), Is.EqualTo(CharacterState.TiredLow));
        }

        [Test]
        public void GuardianArrival_ProgressClampsToOne()
        {
            Assert.That(GuardianArrivalView.Progress(0f, 1f), Is.EqualTo(0f));
            Assert.That(GuardianArrivalView.Progress(0.5f, 1f), Is.EqualTo(0.5f).Within(1e-6));
            Assert.That(GuardianArrivalView.Progress(2f, 1f), Is.EqualTo(1f));
            Assert.That(GuardianArrivalView.Progress(0f, 0f), Is.EqualTo(1f), "0초면 바로 옆에 선다");
        }

        [Test]
        public void GuardianArrival_WalksUntilArrived()
        {
            Assert.That(GuardianArrivalView.IsWalking(0f, 1f), Is.True);
            Assert.That(GuardianArrivalView.IsWalking(0.99f, 1f), Is.True);
            Assert.That(GuardianArrivalView.IsWalking(1f, 1f), Is.False, "다 오면 선다");
            Assert.That(GuardianArrivalView.IsWalking(0f, 0f), Is.False, "0초면 바로 서 있다");
        }

        [Test]
        public void IntroCuttoon_Arrows()
        {
            // 두 장: 첫 장은 다음 화살표만, 둘째 장은 앞 장 화살표와 다음 화살표(누르면 닫힘).
            Assert.That(IntroCuttoonView.HasBack(0), Is.False);
            Assert.That(IntroCuttoonView.IsLast(0, 2), Is.False);
            Assert.That(IntroCuttoonView.HasBack(1), Is.True);
            Assert.That(IntroCuttoonView.IsLast(1, 2), Is.True);
            Assert.That(IntroCuttoonView.IsLast(0, 1), Is.True, "한 장이면 첫 장에서 닫힌다");
        }

        [Test]
        public void IntroCuttoon_OpensBeforeTypeSelectOnce()
        {
            Assert.That(IntroCuttoonView.ShouldOpen(FlowScreen.TypeSelect, false, false), Is.True, "처음 실행이면 유형 선택 앞에서");
            Assert.That(IntroCuttoonView.ShouldOpen(FlowScreen.TypeSelect, true, false), Is.False, "한 번만");
            Assert.That(IntroCuttoonView.ShouldOpen(FlowScreen.Title, false, false), Is.False, "두 번째 실행부터는 열지 않는다");
            Assert.That(IntroCuttoonView.ShouldOpen(FlowScreen.Title, false, true), Is.True, "매번 보기(확인용)");
            Assert.That(IntroCuttoonView.ShouldOpen(FlowScreen.Running, false, true), Is.False);
        }

        [Test]
        public void IntroCuttoon_ReplaysFromSettings()
        {
            Assert.That(IntroCuttoonView.ShouldReplay(FlowScreen.Title, 1, 0), Is.True, "다시보기를 누르면 타이틀 위에");
            Assert.That(IntroCuttoonView.ShouldReplay(FlowScreen.Title, 1, 1), Is.False, "한 번 누르면 한 번");
            Assert.That(IntroCuttoonView.ShouldReplay(FlowScreen.Running, 1, 0), Is.False);
        }

        [Test]
        public void TutorialPrompt_Visibility()
        {
            foreach (TutorialStage s in System.Enum.GetValues(typeof(TutorialStage)))
            {
                Assert.That(TutorialPromptView.IsPromptVisible(s), Is.EqualTo(s == TutorialStage.Prompt));
                Assert.That(TutorialPromptView.IsRetryVisible(s), Is.EqualTo(s == TutorialStage.Retry));
            }
        }

        [Test]
        public void TutorialPrompt_AcceptsTapAfterDelay()
        {
            Assert.That(TutorialPromptView.AcceptsTap(0f, 0.5f), Is.False);
            Assert.That(TutorialPromptView.AcceptsTap(0.49f, 0.5f), Is.False);
            Assert.That(TutorialPromptView.AcceptsTap(0.5f, 0.5f), Is.True);
            Assert.That(TutorialPromptView.AcceptsTap(2f, 0.5f), Is.True);
            Assert.That(TutorialPromptView.AcceptsTap(0f, 0f), Is.True);
        }

        [Test]
        public void TutorialPrompt_ShownAtRestartsWhenPromptReturns()
        {
            // Prompt: 처음 보인 시각을 잡고 지킨다.
            float shown = TutorialPromptView.NextShownAt(true, -1f, 1f);
            Assert.That(shown, Is.EqualTo(1f));
            shown = TutorialPromptView.NextShownAt(true, shown, 3f);
            Assert.That(shown, Is.EqualTo(1f));

            // Prompt → Trying → Prompt: 다시 뜬 시각부터 지연을 센다.
            shown = TutorialPromptView.NextShownAt(TutorialPromptView.IsPromptVisible(TutorialStage.Trying), shown, 4f);
            Assert.That(shown, Is.EqualTo(-1f));
            shown = TutorialPromptView.NextShownAt(TutorialPromptView.IsPromptVisible(TutorialStage.Prompt), shown, 10f);
            Assert.That(shown, Is.EqualTo(10f));
            Assert.That(TutorialPromptView.AcceptsTap(10.2f - shown, 0.5f), Is.False);

            // Prompt → Retry → Prompt도 같다.
            shown = TutorialPromptView.NextShownAt(TutorialPromptView.IsPromptVisible(TutorialStage.Retry), shown, 11f);
            Assert.That(shown, Is.EqualTo(-1f));
            shown = TutorialPromptView.NextShownAt(TutorialPromptView.IsPromptVisible(TutorialStage.Prompt), shown, 20f);
            Assert.That(shown, Is.EqualTo(20f));
        }

        [Test]
        public void TutorialPrompt_SwipeDirectionPerStep()
        {
            Assert.That(TutorialPromptView.SwipeDirection(TutorialStep.MoveLeft), Is.EqualTo(Vector2.left));
            Assert.That(TutorialPromptView.SwipeDirection(TutorialStep.MoveRight), Is.EqualTo(Vector2.right));
            Assert.That(TutorialPromptView.SwipeDirection(TutorialStep.Jump), Is.EqualTo(Vector2.up));
            Assert.That(TutorialPromptView.SwipeDirection(TutorialStep.Slide), Is.EqualTo(Vector2.down));
            Assert.That(TutorialPromptView.SwipeDirection(TutorialStep.Snack), Is.EqualTo(Vector2.zero));
            Assert.That(TutorialPromptView.SwipeDirection(null), Is.EqualTo(Vector2.zero));
        }

        [TestCase(0f, -1f, 0f)]
        [TestCase(0.12f, -0.47f, 0.5f)]
        [TestCase(0.42f, 0.5f, 1f)]
        [TestCase(0.72f, 0.96f, 0.5f)]
        [TestCase(0.84f, 1f, 0f)]
        [TestCase(1.2f, -1f, 0f)]
        public void TutorialPrompt_SwipeFrame_MovesThenFades(float time, float offset, float alpha)
        {
            TutorialPromptView.SwipeFrame(time, 1.2f, out float o, out float a);
            Assert.That(o, Is.EqualTo(offset).Within(0.01f));
            Assert.That(a, Is.EqualTo(alpha).Within(0.01f));
        }

        [Test]
        public void ScreenVisibility_PerFlowScreen()
        {
            foreach (FlowScreen s in System.Enum.GetValues(typeof(FlowScreen)))
            {
                Assert.That(TitleScreenView.IsVisible(s), Is.EqualTo(s == FlowScreen.Title || s == FlowScreen.Settings), "설정 창은 타이틀 위에 뜬다");
                Assert.That(TypeSelectView.IsVisible(s), Is.EqualTo(s == FlowScreen.TypeSelect));
                Assert.That(SettingsView.IsVisible(s), Is.EqualTo(s == FlowScreen.Settings));
            }
        }

        [Test]
        public void TutorialSkip_VisibleWheneverSkippableGuidedRun()
        {
            Assert.That(TutorialPromptView.IsSkipVisible(FlowScreen.Running, false, true, true), Is.True, "설명 창이 없어도 보인다");
            Assert.That(TutorialPromptView.IsSkipVisible(FlowScreen.Running, false, true, false), Is.False, "처음 튜토리얼은 건너뛸 수 없다");
            Assert.That(TutorialPromptView.IsSkipVisible(FlowScreen.Running, false, false, true), Is.False, "안내 구간이 끝났다");
            Assert.That(TutorialPromptView.IsSkipVisible(FlowScreen.Running, true, true, true), Is.False, "일시정지 메뉴");
            Assert.That(TutorialPromptView.IsSkipVisible(FlowScreen.Title, false, true, true), Is.False);
        }

        [Test]
        public void TitleTypeLabel()
        {
            Assert.That(TitleScreenView.TypeLabel(TrackType.Type1), Is.EqualTo("1형"));
            Assert.That(TitleScreenView.TypeLabel(TrackType.Type2), Is.EqualTo("2형"));
            Assert.That(TitleScreenView.TypeLabel(null), Is.EqualTo(""));
        }

        [Test]
        public void NoticeSignal_Labels()
        {
            Assert.That(NoticeSignalView.LabelOf(null), Is.EqualTo(""));
            Assert.That(NoticeSignalView.LabelOf(NoticeKind.High), Is.Not.Empty);
            Assert.That(NoticeSignalView.LabelOf(NoticeKind.Low), Is.Not.Empty);
            Assert.That(NoticeSignalView.LabelOf(NoticeKind.High), Is.Not.EqualTo(NoticeSignalView.LabelOf(NoticeKind.Low)));
        }

        [Test]
        public void SceneLook_GroundColor_DistinctPerScene()
        {
            var colors = new[] { "schoolyard", "playground", "outing", "home" }.Select(SceneLook.GroundColorOf).ToList();

            Assert.That(colors.Distinct().Count(), Is.EqualTo(4));
            Assert.That(colors, Has.None.EqualTo(Color.gray));
            Assert.That(SceneLook.GroundColorOf("nope"), Is.EqualTo(Color.gray));
            Assert.That(SceneLook.GroundColorOf(null), Is.EqualTo(Color.gray));
        }

        [Test]
        public void SleepTransition_ShouldFade_OnlyWhenDayAdvances()
        {
            Assert.That(SleepTransitionView.ShouldFade(0, 0), Is.False);
            Assert.That(SleepTransitionView.ShouldFade(0, 1), Is.True);
            Assert.That(SleepTransitionView.ShouldFade(1, 1), Is.False);
            Assert.That(SleepTransitionView.ShouldFade(2, 0), Is.False, "새 주행");
        }

        [TestCase(0f, 1f)]
        [TestCase(0.1f, 1f)]
        [TestCase(0.2f, 1f)]
        [TestCase(0.5f, 0.5f)]
        [TestCase(0.8f, 0f)]
        [TestCase(5f, 0f)]
        public void SleepTransition_AlphaCurve_StartsBlack(float elapsed, float expected)
        {
            Assert.That(SleepTransitionView.AlphaAt(elapsed, 0.2f, 0.6f), Is.EqualTo(expected).Within(1e-5f));
        }

        [Test]
        public void SleepTransition_AlphaCurve_ZeroLengths()
        {
            Assert.That(SleepTransitionView.AlphaAt(0f, 0f, 0f), Is.EqualTo(0f));
            Assert.That(SleepTransitionView.AlphaAt(0f, 0f, 0.6f), Is.EqualTo(1f));
        }

        [TestCase(0f, 1f)]
        [TestCase(0.5f, 1f)]
        [TestCase(0.8f, 0.5f)]
        [TestCase(1.1f, 0f)]
        [TestCase(1.7f, 0f)]
        public void SleepTransition_MoonSwapsToSunAfterHold(float elapsed, float moon)
        {
            Assert.That(SleepTransitionView.MoonShare(elapsed, 0.5f, 0.6f), Is.EqualTo(moon).Within(1e-4f));
        }

        [Test]
        public void SleepTransition_MoonShare_ZeroFade()
        {
            Assert.That(SleepTransitionView.MoonShare(0f, 0f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void TutorialLaneHighlight_StripGeometry()
        {
            Assert.That(TutorialLaneHighlightView.StripCenter(-1, 2f, 10f), Is.EqualTo(new Vector3(-2f, 0.02f, 5f)));
            Assert.That(TutorialLaneHighlightView.StripCenter(1, 1.5f, 8f), Is.EqualTo(new Vector3(1.5f, 0.02f, 4f)));
            Assert.That(TutorialLaneHighlightView.StripLength(10f), Is.EqualTo(10f));
            Assert.That(TutorialLaneHighlightView.StripLength(-3f), Is.EqualTo(0f));
        }
    }
}
