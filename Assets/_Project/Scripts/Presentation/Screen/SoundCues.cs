using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>효과음 종류. 정답·오답을 나누지 않는다(고른 뒤 문구처럼 무엇을 골랐는지만 알린다).</summary>
    public enum SoundCue
    {
        Tap,
        Choose,
        Warning,
        Alert,
        CountTick,
        CountGo,
        Result,
        LevelUp,
        Eat,
    }

    /// <summary>
    /// 효과음 묶음 에셋. 칸을 비우면 그 소리는 나지 않는다. 같은 이름의 파일로 바꾸면 그대로 들어간다.
    /// </summary>
    [CreateAssetMenu(menuName = "SugarDash/Sound Cues", fileName = "SoundCues")]
    public sealed class SoundCues : ScriptableObject
    {
        [Tooltip("화면 버튼(시작, 설정, 일시정지, 다시하기, 튜토리얼 ▶◀ 등)")]
        public AudioClip tap;
        [Tooltip("선택 패널에서 고를 때, 측정기를 누를 때")]
        public AudioClip choose;
        [Tooltip("1형 게이지 경고가 뜰 때")]
        public AudioClip warning;
        [Tooltip("세계가 멈추고 선택 패널이 열릴 때, 도움이 필요해요")]
        public AudioClip alert;
        [Tooltip("재개 카운트다운 3, 2, 1")]
        public AudioClip countTick;
        [Tooltip("카운트다운이 끝나고 다시 달릴 때")]
        public AudioClip countGo;
        [Tooltip("결과 화면이 뜰 때")]
        public AudioClip result;
        [Tooltip("레벨이 오른 판에서 레벨 원이 부풀 때")]
        public AudioClip levelUp;
        [Tooltip("주행 중 트랙 음식을 먹을 때(1형 간식, 2형 음식)")]
        public AudioClip eat;

        [Tooltip("소리 크기(0~1)")]
        [Range(0f, 1f)] public float volume = 0.8f;

        /// <summary>그 소리의 파일. 에셋이 없거나 칸이 비면 null(소리 없음).</summary>
        public static AudioClip Of(SoundCues asset, SoundCue cue)
        {
            if (asset == null) return null;
            switch (cue)
            {
                case SoundCue.Tap: return asset.tap;
                case SoundCue.Choose: return asset.choose;
                case SoundCue.Warning: return asset.warning;
                case SoundCue.Alert: return asset.alert;
                case SoundCue.CountTick: return asset.countTick;
                case SoundCue.CountGo: return asset.countGo;
                case SoundCue.Result: return asset.result;
                case SoundCue.LevelUp: return asset.levelUp;
                case SoundCue.Eat: return asset.eat;
                default: return null;
            }
        }
    }
}
