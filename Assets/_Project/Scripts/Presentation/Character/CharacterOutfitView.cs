using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 장면 id(맵)에 맞춰 캐릭터 의상(체육복, 멜빵바지, 파자마)을 바꾼다. 외형 프리팹 루트에 붙인다.
    /// 의상 오브젝트를 켜고 끄기만 한다. 리그, Animator, 몸 메시, 이동은 건드리지 않는다. 연출만이다.
    /// 누가 Show를 부를지(게임 진행 연결)는 아직 정해지지 않았다. 그전에는 샌드박스 칸으로 시험한다.
    /// </summary>
    // [영역 파일] 담당: 캐릭터
    // 장면 id에 따라 외형 프리팹 안의 의상 오브젝트만 켜고 끄는 연출 컴포넌트.
    public sealed class CharacterOutfitView : MonoBehaviour
    {
        [Serializable]
        private sealed class Outfit
        {
            [Tooltip("의상 이름. 기본 의상 칸과 맞춘다. 예: tracksuit")]
            public string id;
            [Tooltip("이 의상을 입는 장면 id(설정에 적힌 그대로). 예: schoolyard")]
            public List<string> sceneIds = new List<string>();
            [Tooltip("이 의상을 입을 때 켜는 오브젝트. 다른 의상을 입으면 끈다.")]
            public List<GameObject> parts = new List<GameObject>();

            public Outfit() { }

            public Outfit(string id, params string[] sceneIds)
            {
                this.id = id;
                this.sceneIds = new List<string>(sceneIds);
            }
        }

        [Tooltip("의상 목록. 장면 id 하나는 한 의상에만 적는다(여러 곳에 있으면 위쪽이 이긴다).")]
        [SerializeField] private List<Outfit> outfits = new List<Outfit>
        {
            new Outfit("tracksuit", "schoolyard"),
            new Outfit("overalls", "playground", "outing"),
            new Outfit("pajama", "home"),
        };
        [Tooltip("장면이 없는 구간(튜토리얼, 1구간)과 목록에 없는 장면에 입는 의상 id.")]
        [SerializeField] private string defaultOutfitId = "tracksuit";

        [Header("샌드박스(혼자 시험할 때만. 게임이 Show를 부르면 덮어쓴다)")]
        [Tooltip("시작할 때 이 장면 id의 의상을 입는다. 비워 두면 기본 의상. Play 중에 바꾸면 바로 갈아입는다. 예: home")]
        [SerializeField] private string sandboxSceneId;

        /// <summary>지금 입은 의상 id. 아직 아무것도 입히지 않았으면 null.</summary>
        public string CurrentOutfitId { get; private set; }

        /// <summary>장면 id에 맞는 의상만 켜고 나머지 의상은 끈다. 의상이 그대로면 아무것도 하지 않는다. null은 장면 없는 구간.</summary>
        public void Show(string sceneId)
        {
            _shown = true;
            var target = OutfitOf(sceneId);
            string targetId = target != null ? target.id : null;
            if (CurrentOutfitId != null && targetId == CurrentOutfitId) return;

            // 먼저 모두 끄고 입을 의상을 켠다. 두 의상이 같은 오브젝트를 같이 쓰면 켜진 채로 남는다.
            for (int i = 0; i < outfits.Count; i++)
            {
                if (outfits[i] != target) SetPartsActive(outfits[i], false);
            }
            if (target != null) SetPartsActive(target, true);
            CurrentOutfitId = targetId;
        }

        private bool _shown;
        private string _lastSandboxSceneId;

        private void Start()
        {
            // 게임이 먼저 장면을 알려 줬으면(시작 순서는 정해져 있지 않다) 덮어쓰지 않는다.
            _lastSandboxSceneId = sandboxSceneId;
            if (_shown) return;
            Show(string.IsNullOrEmpty(sandboxSceneId) ? null : sandboxSceneId);
        }

        // Play 중에 Inspector에서 샌드박스 장면 id를 바꾸면 바로 갈아입는다. 편집 중(Play 아님)에는 프리팹을 건드리지 않는다.
        private void OnValidate()
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            if (sandboxSceneId == _lastSandboxSceneId) return;
            _lastSandboxSceneId = sandboxSceneId;
            Show(string.IsNullOrEmpty(sandboxSceneId) ? null : sandboxSceneId);
        }

        // 장면 id를 적은 첫 의상. 없으면 기본 의상. 기본 의상 id도 목록에 없으면 null(모든 의상을 끈다).
        private Outfit OutfitOf(string sceneId)
        {
            if (!string.IsNullOrEmpty(sceneId))
            {
                for (int i = 0; i < outfits.Count; i++)
                {
                    if (outfits[i] != null && outfits[i].sceneIds != null && outfits[i].sceneIds.Contains(sceneId)) return outfits[i];
                }
            }
            for (int i = 0; i < outfits.Count; i++)
            {
                if (outfits[i] != null && outfits[i].id == defaultOutfitId) return outfits[i];
            }
            return null;
        }

        private static void SetPartsActive(Outfit outfit, bool active)
        {
            if (outfit == null || outfit.parts == null) return;
            for (int i = 0; i < outfit.parts.Count; i++)
            {
                var part = outfit.parts[i];
                if (part != null && part.activeSelf != active) part.SetActive(active);
            }
        }
    }
}
