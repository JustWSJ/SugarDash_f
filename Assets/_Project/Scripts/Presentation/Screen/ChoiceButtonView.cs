using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>
    /// 선택 패널 버튼 하나의 글자와 그림 칸. HypoChoicePanel이 버튼 템플릿을 복제한 뒤 채운다.
    /// 그림이 없으면 그림 칸을 숨겨 글자만 보인다. 양 고르기는 간식 그림(icon) 옆에 양 그림(amount)을 하나 더 둔다.
    /// </summary>
    // [빈 틀] 담당: 화면
    public sealed class ChoiceButtonView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [Tooltip("그림 칸. 카드는 가운데 위, 넓은 버튼은 왼쪽.")]
        [SerializeField] private Image icon;
        [Tooltip("양 그림 칸(넓은 버튼만). 양 고르기에서만 쓴다.")]
        [SerializeField] private Image amount;

        /// <summary>그림 칸을 보일지. 그림이 있을 때만.</summary>
        public static bool ShowsPicture(Sprite sprite) => sprite != null;

        public void Set(string text, Sprite iconSprite, Sprite amountSprite = null)
        {
            if (label != null) label.text = text;
            Fill(icon, iconSprite);
            Fill(amount, amountSprite);
        }

        private static void Fill(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            bool show = ShowsPicture(sprite);
            if (image.gameObject.activeSelf != show) image.gameObject.SetActive(show);
        }
    }
}
