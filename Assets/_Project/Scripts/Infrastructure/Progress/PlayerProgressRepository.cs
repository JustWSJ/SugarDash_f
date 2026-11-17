using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SugarDash.Domain.Ports;
using SugarDash.Infrastructure.Progress.Dto;

namespace SugarDash.Infrastructure.Progress
{
    /// <summary>
    /// <see cref="IPlayerProgressStore"/> 구현. 파일 하나에 누적 경험치를 덮어쓴다.
    /// 임시 파일에 쓴 뒤 교체(atomic write)해 쓰는 도중 종료돼도 기존 파일이 깨지지 않는다.
    /// 형식: <c>{"schemaVersion":1,"totalXp":120}</c>
    /// </summary>
    public sealed class PlayerProgressRepository : IPlayerProgressStore
    {
        /// <summary>파일 형식을 바꾸면 올린다.</summary>
        public const int SchemaVersion = 1;

        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.None,
        };

        private readonly string _path;

        /// <param name="path">파일 경로. Bootstrap이 <c>Application.persistentDataPath</c> 아래 경로를 넘긴다.</param>
        public PlayerProgressRepository(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("경로가 비었다", nameof(path));
            _path = path;
        }

        public int LoadTotalXp() => Math.Max(0, TryRead()?.TotalXp ?? 0);

        public void SaveTotalXp(int totalXp) => Update(dto => dto.TotalXp = Math.Max(0, totalXp));

        /// <summary>파일을 읽는다. 없거나 깨졌거나 스키마가 다르면 null.</summary>
        private PlayerProgressDto TryRead()
        {
            try
            {
                if (!File.Exists(_path)) return null;

                var dto = JsonConvert.DeserializeObject<PlayerProgressDto>(File.ReadAllText(_path, Utf8NoBom), Settings);
                return dto != null && dto.SchemaVersion == SchemaVersion ? dto : null;
            }
            catch (Exception e) when (e is JsonException || e is IOException || e is UnauthorizedAccessException)
            {
                // 깨진 파일은 기록이 없는 것으로 본다. 다음 Save가 새 파일로 덮어쓴다.
                return null;
            }
        }

        /// <summary>지금 파일을 읽어 다른 필드는 두고 한 곳만 바꾼 뒤 원자적으로 쓴다. 읽을 수 없으면 빈 DTO에서 시작한다.</summary>
        private void Update(Action<PlayerProgressDto> change)
        {
            string tmp = _path + ".tmp";
            try
            {
                string dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                var dto = TryRead() ?? new PlayerProgressDto();
                dto.SchemaVersion = SchemaVersion;
                change(dto);
                File.WriteAllText(tmp, JsonConvert.SerializeObject(dto, Settings), Utf8NoBom);

                if (File.Exists(_path)) File.Replace(tmp, _path, null);
                else File.Move(tmp, _path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // 저장 값만 잃고 게임은 계속한다(BestScoreRepository와 같은 방침).
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}
