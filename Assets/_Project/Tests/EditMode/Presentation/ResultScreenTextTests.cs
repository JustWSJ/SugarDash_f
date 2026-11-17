using System.Collections.Generic;
using SugarDash.Domain.Metrics;
using NUnit.Framework;

namespace SugarDash.Presentation.Tests
{
    public class ResultScreenTextTests
    {
        private static EventResult Event(bool allCorrect)
        {
            var steps = new Dictionary<MetricStep, bool?>
            {
                [MetricStep.Recognize] = true,
                [MetricStep.Respond] = allCorrect,
                [MetricStep.Dose] = true,
                [MetricStep.Recheck] = true,
            };
            return new EventResult(1, false, steps, allCorrect, true);
        }

        private static RunSummary Summary(RunOutcome outcome, params EventResult[] events) =>
            new RunSummary("run", outcome, events, new HyperEventResult[0], null);

        private static RunSummary WithHyper(RunSummary hypo, params bool[] hyperAllCorrect)
        {
            var hyper = new List<HyperEventResult>();
            for (int i = 0; i < hyperAllCorrect.Length; i++) hyper.Add(new HyperEventResult(10 + i, hyperAllCorrect[i], true));
            return new RunSummary(hypo.RunId, hypo.Outcome, hypo.Events, hyper, null);
        }

        private static RunSummary Type2Summary(bool[] high, bool[] low)
        {
            var hyper = new List<HyperEventResult>();
            for (int i = 0; i < high.Length; i++) hyper.Add(new HyperEventResult(10 + i, high[i], true));
            var lows = new List<LowEventResult>();
            for (int i = 0; i < low.Length; i++) lows.Add(new LowEventResult(20 + i, low[i], true));
            return new RunSummary("run", RunOutcome.Eliminated, new EventResult[0], hyper, null, lows,
                SugarDash.Domain.Config.TrackType.Type2);
        }

        private static readonly string[] Forbidden = { "나쁜", "죽", "생존", "치료" };

        private static string[] Lines(string text) => text.Split('\n');

        [Test]
        public void TutorialTitle_Text()
        {
            Assert.That(ResultScreenText.TutorialTitle, Is.EqualTo("튜토리얼 끝!"));
        }

        [Test]
        public void Type2_SummaryLines()
        {
            var mixed = Lines(ResultScreenText.Summary(Type2Summary(new[] { true, false }, new[] { true })));
            Assert.That(mixed.Length, Is.EqualTo(2));
            Assert.That(mixed[0], Does.Contain("2").And.Contain("1"));
            Assert.That(mixed[1], Does.Contain("1"));

            var lowMixed = Lines(ResultScreenText.Summary(Type2Summary(new[] { true, true }, new[] { true, false, false })));
            Assert.That(lowMixed.Length, Is.EqualTo(2));
            Assert.That(lowMixed[0], Does.Contain("2"));
            Assert.That(lowMixed[1], Does.Contain("3").And.Contain("1"));
            // 높음 줄과 낮음 줄은 서로 다르다.
            Assert.That(lowMixed[0], Is.Not.EqualTo(lowMixed[1]));

            var missed = Lines(ResultScreenText.Summary(Type2Summary(new[] { false, false }, new[] { false })));
            Assert.That(missed.Length, Is.EqualTo(3));
            Assert.That(missed[0], Does.Contain("2"));
            Assert.That(missed[1], Does.Contain("1"));
            Assert.That(missed[2], Is.Not.Empty);
            // 2형 격려는 1형 격려와 다르다(할 일인 "어른에게 알리기"를 말한다).
            var type1Missed = Lines(ResultScreenText.Summary(Summary(RunOutcome.Eliminated, Event(false))));
            Assert.That(missed[2], Is.Not.EqualTo(type1Missed[type1Missed.Length - 1]));
        }

        [Test]
        public void Type2_NoSignalLineOnlyWhenNoEvents()
        {
            // 신호가 한 번도 없던 판은 한 줄(1형의 "혈당이 낮아지지 않았어요"와 짝)만 있고 격려는 없다.
            var none = ResultScreenText.Summary(Type2Summary(new bool[0], new bool[0]));
            Assert.That(none, Is.Not.Empty);
            Assert.That(none, Does.Not.Contain("\n"));

            var onlyLow = Lines(ResultScreenText.Summary(Type2Summary(new bool[0], new[] { false, true })));
            Assert.That(onlyLow.Length, Is.EqualTo(1), "높을 때 줄은 없다");
            Assert.That(onlyLow[0], Does.Contain("2").And.Contain("1"));
        }

        [Test]
        public void Title_Eliminated()
        {
            var title = ResultScreenText.Title(Summary(RunOutcome.Eliminated));
            Assert.That(title, Is.Not.Empty);
            Assert.That(title, Is.Not.EqualTo(ResultScreenText.Title(null)));
            foreach (var word in Forbidden) Assert.That(title, Does.Not.Contain(word), title);
        }

        [Test]
        public void MissingOrUnfinishedResult_ShowsLoadFailure_WithoutSummary()
        {
            var eliminated = ResultScreenText.Title(Summary(RunOutcome.Eliminated));
            foreach (var result in new[] { null, Summary(RunOutcome.Unfinished, Event(true)) })
            {
                Assert.That(ResultScreenText.Title(result), Is.Not.Empty);
                Assert.That(ResultScreenText.Title(result), Is.Not.EqualTo(eliminated));
                Assert.That(ResultScreenText.Summary(result), Is.Empty);
            }
        }

        [Test]
        public void Summary_CountsAllCorrectEvents()
        {
            var result = Summary(RunOutcome.Eliminated, Event(true), Event(false));

            var lines = Lines(ResultScreenText.Summary(result));
            Assert.That(lines.Length, Is.EqualTo(1));
            Assert.That(lines[0], Does.Contain("2").And.Contain("1"));
        }

        [Test]
        public void Summary_WithNoAllCorrectEvent_Encourages()
        {
            var result = Summary(RunOutcome.Eliminated, Event(false), Event(false));

            var lines = Lines(ResultScreenText.Summary(result));
            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[0], Does.Contain("2"));
            Assert.That(lines[1], Is.Not.Empty);
        }

        [Test]
        public void Summary_AddsHyperLine_CountingImmediateNotify()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated, Event(true)), true, false);

            var lines = Lines(ResultScreenText.Summary(result));
            Assert.That(lines.Length, Is.EqualTo(2));
            Assert.That(lines[0], Does.Contain("1"));
            Assert.That(lines[1], Does.Contain("2").And.Contain("1"));
        }

        [Test]
        public void Summary_WithHyperButNoImmediateNotify_SaysCountOnly()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated), false);

            var lines = Lines(ResultScreenText.Summary(result));
            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines[1], Does.Contain("1"));
            Assert.That(lines[2], Is.Not.Empty);
        }

        [Test]
        public void Summary_WithBothMissed_AddsEncouragementOnceAtBottom()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated, Event(false)), false);

            var lines = Lines(ResultScreenText.Summary(result));
            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines[0], Does.Contain("1"));
            Assert.That(lines[1], Does.Contain("1"));
            Assert.That(lines[2], Is.Not.Empty);
            Assert.That(lines[2], Is.Not.EqualTo(lines[0]));
        }

        [Test]
        public void Summary_WithHypoMissedButHyperNotified_EncouragesAtBottom()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated, Event(false)), true);

            var lines = Lines(ResultScreenText.Summary(result));
            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines[2], Is.Not.Empty);
            // 고혈당 줄은 모두 알렸을 때와 놓쳤을 때 글이 다르다.
            var hyperMissed = Lines(ResultScreenText.Summary(WithHyper(Summary(RunOutcome.Eliminated, Event(false)), false)));
            Assert.That(lines[1], Is.Not.EqualTo(hyperMissed[1]));
        }

        [Test]
        public void Summary_WhenEveryEventWentWell_SaysAllInsteadOfCount()
        {
            var result = WithHyper(Summary(RunOutcome.Eliminated, Event(true), Event(true)), true, true);
            var partial = WithHyper(Summary(RunOutcome.Eliminated, Event(true), Event(false)), true, false);

            var lines = Lines(ResultScreenText.Summary(result));
            var partialLines = Lines(ResultScreenText.Summary(partial));
            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines[0], Does.Contain("2"));
            Assert.That(lines[1], Does.Contain("2"));
            // 모두 해낸 줄은 일부만 해낸 줄과 글이 다르고, 맨 아래는 격려 대신 칭찬이다.
            Assert.That(lines[0], Is.Not.EqualTo(partialLines[0]));
            Assert.That(lines[1], Is.Not.EqualTo(partialLines[1]));
            Assert.That(lines[2], Is.EqualTo(ResultScreenText.Closing(result)));
            Assert.That(ResultScreenText.IsPraise(result), Is.True);
            Assert.That(ResultScreenText.IsPraise(partial), Is.False);
            Assert.That(ResultScreenText.Closing(partial), Is.Null, "일부만 해냈으면 마무리 줄이 없다");
        }

        [Test]
        public void Summary_WithoutEvents_SaysNoHypo()
        {
            var text = ResultScreenText.Summary(Summary(RunOutcome.Eliminated));
            Assert.That(text, Is.Not.Empty);
            Assert.That(text, Does.Not.Contain("\n"));
        }

        [Test]
        public void Praise_NeedsAtLeastOneEvent()
        {
            Assert.That(ResultScreenText.IsPraise(Summary(RunOutcome.Eliminated)), Is.False, "대처할 일이 없던 판");
            Assert.That(ResultScreenText.Closing(Summary(RunOutcome.Eliminated)), Is.Null);
            Assert.That(ResultScreenText.IsPraise(Type2Summary(new[] { true }, new bool[0])), Is.True);
            Assert.That(ResultScreenText.Closing(Type2Summary(new[] { false }, new bool[0])), Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ScoreCardTexts()
        {
            Assert.That(ResultScreenText.ScoreValue(123.9), Is.EqualTo("123"));
            Assert.That(ResultScreenText.BestChip(200, 150, false), Is.EqualTo("최고 200"));
            Assert.That(ResultScreenText.BestChip(320.5, 300.7, true), Is.EqualTo("이전 최고 점수 300"));
            Assert.That(ResultScreenText.BestChip(320, null, true), Is.Empty, "처음 기록이면 이전 점수가 없다");
            Assert.That(ResultScreenText.BestChip(null, null, false), Is.Empty);
        }

        [Test]
        public void ColorClosing_ColorsOnlyLastLine()
        {
            var colored = ResultScreenView.ColorClosing("a\nb", "b", UnityEngine.Color.red);
            Assert.That(colored, Is.EqualTo("a\n<color=#FF0000>b</color>"));
            Assert.That(ResultScreenView.ColorClosing("a", null, UnityEngine.Color.red), Is.EqualTo("a"));
        }

        [Test]
        public void Summary_HasNoForbiddenWords()
        {
            var texts = new[]
            {
                ResultScreenText.Summary(Summary(RunOutcome.Eliminated, Event(false))),
                ResultScreenText.Summary(WithHyper(Summary(RunOutcome.Eliminated, Event(true)), true)),
                ResultScreenText.Summary(WithHyper(Summary(RunOutcome.Eliminated, Event(true)), false)),
                ResultScreenText.Summary(Type2Summary(new[] { false }, new[] { true, false })),
            };
            foreach (var text in texts)
                foreach (var word in Forbidden) Assert.That(text, Does.Not.Contain(word), text);
        }
    }
}
