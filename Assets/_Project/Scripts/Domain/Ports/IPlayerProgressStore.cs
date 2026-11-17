namespace SugarDash.Domain.Ports
{
    /// <summary>플레이어 진행 저장 포트. 누적 경험치를 다룬다. 레벨은 저장하지 않는다.</summary>
    public interface IPlayerProgressStore
    {
        /// <summary>누적 경험치. 기록이 없거나 읽을 수 없으면 0.</summary>
        int LoadTotalXp();

        /// <summary>누적 경험치를 저장한다. 디스크 오류는 호출자에게 올리지 않는다.</summary>
        void SaveTotalXp(int totalXp);
    }
}
