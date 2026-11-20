using System;
using System.Collections.Generic;
using SugarDash.Domain.Hypo;
using UnityEngine;

namespace SugarDash.Presentation
{
    /// <summary>선택지 그림의 종류. 음식은 음식 id로 따로 찾는다.</summary>
    public enum ChoicePicture
    {
        KeepRunning,
        NoSugar,
        Recheck,
        RunAgain,
        TellAdult,
    }

    /// <summary>음식 id 하나와 그 그림.</summary>
    [Serializable]
    public sealed class FoodPicture
    {
        [Tooltip("음식 id: water, juice, soda, apple, chicken, pizza, burger, cracker")]
        public string foodId;
        public Sprite sprite;
    }

    /// <summary>
    /// 선택 패널의 그림 묶음(Inspector에서 채움). 간식 카드, 넓은 버튼 왼쪽 그림, 양 그림에 쓴다.
    /// 빈 칸은 null을 돌려주고, 그러면 버튼은 그림 없이 글자만 보인다. 그림은 보여 주기만 하고 판정과 관계없다.
    /// </summary>
    [Serializable]
    public sealed class ChoicePictureSet
    {
        [Tooltip("음식 id별 그림. 간식 카드와 2형 \"주스 마시기\" 버튼에 쓴다.")]
        public List<FoodPicture> foods = new List<FoodPicture>();
        public Sprite keepRunning;
        public Sprite noSugar;
        [Tooltip("다시 재기(측정기)")]
        public Sprite recheck;
        public Sprite runAgain;
        public Sprite tellAdult;
        [Tooltip("양 고르기: 적게. 숫자(그램, 개수) 없이 채움 칸으로만 그린다.")]
        public Sprite doseUnder;
        [Tooltip("양 고르기: 알맞게")]
        public Sprite doseProper;
        [Tooltip("양 고르기: 많이")]
        public Sprite doseOver;

        /// <summary>음식 id의 그림. 없으면 null.</summary>
        public Sprite Food(string foodId)
        {
            if (foods == null || string.IsNullOrEmpty(foodId)) return null;
            foreach (var food in foods)
            {
                if (food != null && food.foodId == foodId) return food.sprite;
            }
            return null;
        }

        public Sprite Of(ChoicePicture picture)
        {
            switch (picture)
            {
                case ChoicePicture.KeepRunning: return keepRunning;
                case ChoicePicture.NoSugar: return noSugar;
                case ChoicePicture.Recheck: return recheck;
                case ChoicePicture.RunAgain: return runAgain;
                case ChoicePicture.TellAdult: return tellAdult;
                default: return null;
            }
        }

        public Sprite Dose(DoseLevel level)
        {
            switch (level)
            {
                case DoseLevel.Under: return doseUnder;
                case DoseLevel.Proper: return doseProper;
                case DoseLevel.Over: return doseOver;
                default: return null;
            }
        }
    }
}
