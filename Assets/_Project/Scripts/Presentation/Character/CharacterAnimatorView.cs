using System;
using SugarDash.App;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 플레이어 상태를 캐릭터 애니메이터 파라미터로 전달한다. 외형 프리팹 루트에 붙인다.
    /// PlayerController가 외형을 만든 직후 Init을 부른다. 상태를 읽기만 하고 바꾸지 않는다.
    /// </summary>
    // [영역 파일] 담당: 캐릭터
    public sealed class CharacterAnimatorView : MonoBehaviour
    {
        [Tooltip("비워 두면 자식에서 찾는다.")]
        [SerializeField] private Animator animator;
        [Tooltip("장면에 맞춰 의상을 바꾸는 컴포넌트. 비워 두면 같은 오브젝트에서 찾는다. 없으면 의상을 바꾸지 않는다.")]
        [SerializeField] private CharacterOutfitView outfit;

        private static readonly int IsRunningId = Animator.StringToHash("IsRunning");
        private static readonly int IsAirborneId = Animator.StringToHash("IsAirborne");
        private static readonly int IsSlidingId = Animator.StringToHash("IsSliding");
        private static readonly int SlideStateId = Animator.StringToHash("Slide");

        private RunController _run;
        private PlayerController _player;
        private int _lastSlideCount;
        private bool _wasSliding;
        private string _lastSceneId;
        private bool _outfitApplied;

        public void Init(RunController run, PlayerController player)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _lastSlideCount = player.SlideCount;
            if (outfit == null) outfit = GetComponent<CharacterOutfitView>();
            _outfitApplied = false;
        }

        private void LateUpdate()
        {
            if (_run != null) UpdateOutfit();
            if (_run == null || animator == null) return;

            // TODO(캐릭터): 옆 이동·지친 상태 파라미터가 생기면 여기서 전달한다.
            bool running = _run.State == RunState.Running;
            bool frozen = _run.IsWorldFrozen;
            bool airborne = running && _player.IsAirborne;
            bool sliding = running && _player.IsSliding;

            // 대처 패널 등으로 세계가 멈추면 땅에서는 Idle, 공중이거나 슬라이드 중이면 그 자세로 멈춘다.
            animator.SetBool(IsRunningId, running && !frozen);
            animator.SetBool(IsAirborneId, airborne);
            animator.SetBool(IsSlidingId, sliding);
            animator.speed = frozen && (airborne || sliding) ? 0f : 1f;

            // 슬라이드 중에 다시 누르면 IsSliding이 계속 켜져 있어 상태가 바뀌지 않는다. 동작을 처음부터 다시 튼다.
            if (sliding && _wasSliding && _player.SlideCount != _lastSlideCount) animator.Play(SlideStateId, 0, 0f);
            _lastSlideCount = _player.SlideCount;
            _wasSliding = sliding;
        }

        // 지금 장면 id를 의상 컴포넌트에 넘긴다. 장면이 실제로 바뀐 프레임에만 부른다.
        // 장면 없는 구간(튜토리얼, 1구간)과 판 밖에서는 null이라 기본 의상을 입는다. 장면 id를 읽기만 한다.
        private void UpdateOutfit()
        {
            if (outfit == null) return;
            string sceneId = _run.CurrentScene?.Id;
            if (_outfitApplied && sceneId == _lastSceneId) return;
            _lastSceneId = sceneId;
            _outfitApplied = true;
            outfit.Show(sceneId);
        }
    }
}
