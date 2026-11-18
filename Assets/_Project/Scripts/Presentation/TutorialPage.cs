using System;
using System.Collections.Generic;

namespace SugarDash.Presentation
{
    /// <summary>튜토리얼 설명 창 한 장: 문구와 그림 칸에 나란히 보여 줄 음식 id(없으면 빈 목록).</summary>
    public sealed class TutorialPage
    {
        public TutorialPage(string text, params string[] foodIds)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            FoodIds = foodIds ?? Array.Empty<string>();
        }

        public string Text { get; }
        public IReadOnlyList<string> FoodIds { get; }
    }
}
