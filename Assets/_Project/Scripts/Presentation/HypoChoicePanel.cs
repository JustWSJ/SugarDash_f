using System;
using System.Collections.Generic;
using SugarDash.App;
using SugarDash.Domain.Config;
using SugarDash.Domain.Events;
using SugarDash.Domain.Hypo;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 2~4단계 선택지, 고혈당 알리기, 오류 뒤 "어른에게 알리기" 대기 화면. 단계가 바뀔 때마다 버튼 템플릿을 복제해 다시 만든다.
    /// 버튼은 만들 때의 단계를 기억해 명령과 함께 넘긴다(연타로 다음 단계에 잘못 들어가지 않게).
    /// 선택 뒤 결과 문구 창 동안에는 무엇을 골랐는지 문구만 보여 주고 버튼은 없다. 창이 끝나면 지금 단계 화면을 그린다.
    /// 재개 카운트다운은 패널을 숨기고 HudPresenter가 숫자로 보여 준다.
    /// 2단계의 간식은 그림 자리가 있는 카드(2칸)로, 나머지 선택지는 넓은 버튼으로, "어른에게 알리기"는 맨 아래 다른 색 버튼으로 놓는다.
    /// 맨 위 남은 시간 막대는 단계의 제한 시간 중 남은 만큼 줄어든다(보여 주기만 한다).
    /// </summary>
    public sealed class HypoChoicePanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text recheckText;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Button buttonTemplate;
        [Tooltip("간식 카드를 놓을 칸(Grid Layout Group). buttonContainer 안 맨 위에 둔다. 비워 두면 간식도 넓은 버튼으로 놓는다.")]
        [SerializeField] private Transform foodContainer;
        [Tooltip("간식 카드 하나의 본(그림 자리 + 이름). 꺼 둔다.")]
        [SerializeField] private Button foodTemplate;
        [Tooltip("\"어른에게 알리기\" 버튼 바탕색")]
        [SerializeField] private Color helpColor = new Color(0.75f, 0.85f, 0.95f);
        [Tooltip("선택지 그림 에셋(ChoicePictures). 비우거나 칸이 비면 그 버튼은 글자만 보인다.")]
        [SerializeField] private ChoicePictures pictureAsset;
        [Tooltip("남은 시간 막대의 채움. 바탕을 꽉 채우게 두면 오른쪽 끝(anchorMax.x)을 남은 비율로 줄인다. 시간 초과가 없는 때는 막대 묶음을 숨긴다.")]
        [SerializeField] private RectTransform timeFill;
        [SerializeField] private GameObject timeBar;

        private readonly List<Button> _buttons = new List<Button>();
        private RunController _run;
        private IReadOnlyList<SnackConfig> _snacks;
        private HypoPhase? _shown;
        private bool _shownFeedback;
        // 마지막 선택의 결과 문구. 문구 창 동안 제목에 보여 준다.
        private string _feedback = "";
        private System.Random _random;

        /// <summary>GameBootstrapper가 연결한다. 반환값을 Dispose하면 구독을 해제한다.</summary>
        public IDisposable Bind(RunController run, DomainEventBus bus, IReadOnlyList<SnackConfig> snacks,
            int seed)
        {
            _random = new System.Random(seed);
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _snacks = snacks ?? throw new ArgumentNullException(nameof(snacks));

            buttonTemplate.gameObject.SetActive(false);
            recheckText.text = "";
            root.SetActive(false);

            var subscriptions = new List<IDisposable>
            {
                bus.Subscribe<HypoEventTriggered>(_ => recheckText.text = ""),
                bus.Subscribe<StepAnswered>(e => _feedback = AnswerFeedbackText.For(e, _run?.DoseSnackId)),
                bus.Subscribe<PracticeRetried>(_ => _feedback = TutorialText.TryAgain),
                bus.Subscribe<GuardianNotified>(_ => _feedback = AnswerFeedbackText.ForGuardianNotified()),
                bus.Subscribe<RecheckFastForward>(e => recheckText.text = ChoicePanelText.RecheckResult(e.GlucoseAfter)),
            };

            return new Unbinder(() =>
            {
                foreach (var subscription in subscriptions) subscription.Dispose();
                _run = null;
            });
        }

        private void Update()
        {
            if (_run == null) return;

            UpdateTimeBar(_run.DecisionTimeLeftFraction);
            // 다시 잰 혈당 줄은 글이 있을 때만 자리를 차지한다.
            bool hasRecheck = recheckText.text.Length > 0;
            if (recheckText.gameObject.activeSelf != hasRecheck) recheckText.gameObject.SetActive(hasRecheck);
            var phase = _run.Phase;
            bool feedback = _run.IsShowingAnswerFeedback;
            if (phase == _shown && feedback == _shownFeedback) return;

            _shown = phase;
            _shownFeedback = feedback;
            Rebuild(phase, feedback);
        }

        private void Rebuild(HypoPhase phase, bool feedback)
        {
            foreach (var button in _buttons) Destroy(button.gameObject);
            _buttons.Clear();
            if (foodContainer != null) foodContainer.gameObject.SetActive(false);

            if (feedback)
            {
                Show(_feedback);
                return;
            }

            switch (phase)
            {
                case HypoPhase.Respond:
                    bool emptyBag = _run.IsEmptyBag;
                    Show(emptyBag ? ChoicePanelText.EmptyBagTitle : ChoicePanelText.RespondTitle);
                    // 간식은 카드 칸에, 나머지는 그 아래 넓은 버튼으로. 각 묶음 안에서 순서를 섞는다.
                    var snacks = new List<Choice>();
                    foreach (var snack in ChoiceOrder.RespondSnacks(_snacks, _random, emptyBag))
                    {
                        string id = snack.Id;
                        snacks.Add(new Choice(FoodText.Name(id), () => _run.ChooseSnack(id), Pictures.Food(id)));
                    }
                    AddFoods(ChoiceOrder.Shuffle(snacks, _random));
                    var respond = new List<Choice>();
                    respond.Add(Pick(ChoicePanelText.KeepRunning, ChoicePicture.KeepRunning, () => _run.ChooseKeepRunning()));
                    // 빈 간식 가방에서는 "먹을 게 없어요"를 빼 "어른에게 알리기"와 뜻이 겹치지 않게 한다.
                    if (!emptyBag) respond.Add(Pick(ChoicePanelText.NoSugar, ChoicePicture.NoSugar, () => _run.ChooseNoSugar()));
                    AddShuffled(respond);
                    AddHelp(phase);
                    break;
                case HypoPhase.Dose:
                    Show(ChoicePanelText.DoseTitle(_run.DoseSnackId));
                    // 양 그림은 고른 간식 그림 옆에 채움 칸으로 보여 준다(숫자 없음).
                    var doseFood = Pictures.Food(_run.DoseSnackId);
                    AddShuffled(new List<Choice>
                    {
                        new Choice(ChoicePanelText.DoseUnder, () => _run.ChooseDose(DoseLevel.Under), doseFood, Pictures.Dose(DoseLevel.Under)),
                        new Choice(ChoicePanelText.DoseProper, () => _run.ChooseDose(DoseLevel.Proper), doseFood, Pictures.Dose(DoseLevel.Proper)),
                        new Choice(ChoicePanelText.DoseOver, () => _run.ChooseDose(DoseLevel.Over), doseFood, Pictures.Dose(DoseLevel.Over)),
                    });
                    AddHelp(phase);
                    break;
                case HypoPhase.Remeasure:
                    Show(ChoicePanelText.NextActionTitle);
                    AddShuffled(new List<Choice>
                    {
                        Pick(ChoicePanelText.Recheck, ChoicePicture.Recheck, () => _run.ChooseRecheck()),
                        Pick(ChoicePanelText.RunAgain, ChoicePicture.RunAgain, () => _run.ChooseRunAgain(phase)),
                    });
                    AddHelp(phase);
                    break;
                case HypoPhase.Notify:
                    Show(ChoicePanelText.NextActionTitle);
                    Add(Pick(ChoicePanelText.RunAgain, ChoicePicture.RunAgain, () => _run.ChooseRunAgain(phase)));
                    AddHelp(phase);
                    break;
                case HypoPhase.HyperNotify:
                    if (_run.TrackType == TrackType.Type2)
                    {
                        Show(ChoicePanelText.Type2TellTitle);
                        AddShuffled(new List<Choice>
                        {
                            Pick(ChoicePanelText.KeepRunning, ChoicePicture.KeepRunning, () => _run.ChooseKeepRunning()),
                            new Choice(ChoicePanelText.Juice, () => _run.ChooseJuice(), Pictures.Food("juice")),
                        });
                    }
                    else
                    {
                        Show(ChoicePanelText.HighTitle);
                        Add(Pick(ChoicePanelText.KeepRunning, ChoicePicture.KeepRunning, () => _run.ChooseKeepRunning()));
                    }
                    AddHelp(phase);
                    break;
                case HypoPhase.LowNotify:
                    Show(ChoicePanelText.Type2TellTitle);
                    Add(Pick(ChoicePanelText.KeepRunning, ChoicePicture.KeepRunning, () => _run.ChooseKeepRunning()));
                    AddHelp(phase);
                    break;
                case HypoPhase.GuardianPrompt:
                    Show(ChoicePanelText.GuardianPromptTitle);
                    Add(Pick(ChoicePanelText.RunAgain, ChoicePicture.RunAgain, () => _run.ChooseRunAgain(phase)));
                    AddHelp(phase);
                    break;
                default:
                    root.SetActive(false);
                    break;
            }
        }

        private void Show(string title)
        {
            titleText.text = title;
            root.SetActive(true);
        }

        /// <summary>버튼 하나: 글자, 누르면 할 일, 그림(없으면 null), 양 그림(양 고르기만).</summary>
        private readonly struct Choice
        {
            public Choice(string label, Action action, Sprite picture, Sprite amount = null)
            {
                Label = label;
                Action = action;
                Picture = picture;
                Amount = amount;
            }

            public string Label { get; }
            public Action Action { get; }
            public Sprite Picture { get; }
            public Sprite Amount { get; }
        }

        private ChoicePictureSet Pictures => ChoicePictures.Of(pictureAsset);

        private Choice Pick(string label, ChoicePicture picture, Action action) => new Choice(label, action, Pictures.Of(picture));

        private void AddShuffled(List<Choice> choices)
        {
            foreach (var choice in ChoiceOrder.Shuffle(choices, _random)) Add(choice);
        }

        private void AddHelp(HypoPhase phase)
        {
            var button = Add(Pick(ChoicePanelText.TellAdult, ChoicePicture.TellAdult, () => _run.PressHelp(phase)));
            if (button.image != null) button.image.color = helpColor;
        }

        // 간식 카드. 카드 칸이 없으면 넓은 버튼으로 놓는다.
        private void AddFoods(IEnumerable<Choice> foods)
        {
            foreach (var food in foods)
            {
                if (foodContainer == null || foodTemplate == null)
                {
                    Add(food);
                    continue;
                }
                foodContainer.gameObject.SetActive(true);
                Make(foodTemplate, foodContainer, food);
            }
        }

        /// <summary>남은 시간 막대. null이면 막대를 숨긴다.</summary>
        private void UpdateTimeBar(double? left)
        {
            if (timeBar != null && timeBar.activeSelf != left.HasValue) timeBar.SetActive(left.HasValue);
            if (timeFill != null && left.HasValue)
                timeFill.anchorMax = new Vector2(TimeBarFill(left.Value), timeFill.anchorMax.y);
        }

        /// <summary>막대 채움 비율(0~1).</summary>
        public static float TimeBarFill(double left) => Mathf.Clamp01((float)left);

        private Button Add(Choice choice) => Make(buttonTemplate, buttonContainer, choice);

        private Button Make(Button template, Transform parent, Choice choice)
        {
            var button = Instantiate(template, parent);
            button.gameObject.SetActive(true);
            // 그림 칸이 있는 템플릿이면 그림까지, 없으면 글자만 채운다.
            var view = button.GetComponent<ChoiceButtonView>();
            if (view != null) view.Set(choice.Label, choice.Picture, choice.Amount);
            else button.GetComponentInChildren<Text>().text = choice.Label;
            var onClick = choice.Action;
            button.onClick.AddListener(() =>
            {
                if (_run != null) onClick();
            });
            _buttons.Add(button);
            return button;
        }
    }
}
