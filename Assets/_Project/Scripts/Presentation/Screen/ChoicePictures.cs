using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 선택지·음식 그림 묶음 에셋. 선택 패널과 튜토리얼 설명 창이 같은 에셋을 가리켜, 그림을 한 곳에서만 바꾸면 된다.
    /// </summary>
    [CreateAssetMenu(menuName = "SugarDash/Choice Pictures", fileName = "ChoicePictures")]
    public sealed class ChoicePictures : ScriptableObject
    {
        public ChoicePictureSet set = new ChoicePictureSet();

        private static readonly ChoicePictureSet Empty = new ChoicePictureSet();

        /// <summary>에셋이 비어 있으면(연결 안 됨) 빈 묶음. 그러면 모든 버튼이 글자만 보인다.</summary>
        public static ChoicePictureSet Of(ChoicePictures asset) => asset != null && asset.set != null ? asset.set : Empty;
    }
}
