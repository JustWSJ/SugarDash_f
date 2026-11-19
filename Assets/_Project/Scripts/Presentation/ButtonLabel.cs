using UnityEngine;
using UnityEngine.UI;

namespace SugarDash.Presentation
{
    /// <summary>버튼 바로 아래 자식의 글자 칸에 문구를 넣는다(배지 같은 더 깊은 자식 글자는 건드리지 않는다).</summary>
    public static class ButtonLabel
    {
        public static void Set(Button button, string text)
        {
            if (button == null) return;
            foreach (Transform child in button.transform)
            {
                var label = child.GetComponent<Text>();
                if (label == null) continue;
                label.text = text;
                return;
            }
        }

        /// <summary>오브젝트 안의 첫 글자 칸에 문구를 넣는다("지금" 배지처럼 글자 하나를 감싼 묶음).</summary>
        public static void SetIn(GameObject target, string text)
        {
            if (target == null) return;
            var label = target.GetComponentInChildren<Text>(true);
            if (label != null) label.text = text;
        }
    }
}
