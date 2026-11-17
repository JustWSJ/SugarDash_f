using System;
using System.IO;
using NUnit.Framework;
using SugarDash.Infrastructure.Progress;

namespace SugarDash.Infrastructure.Tests
{
    public class PlayerProgressRepositoryTests
    {
        private string _dir;
        private string _path;
        private PlayerProgressRepository _repo;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "SugarDashProgressTests-" + Guid.NewGuid().ToString("N"));
            _path = Path.Combine(_dir, "player-progress.json");
            _repo = new PlayerProgressRepository(_path);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void NoFile_ZeroXp()
        {
            Assert.That(_repo.LoadTotalXp(), Is.EqualTo(0));
        }

        [Test]
        public void Save_RoundTrips()
        {
            _repo.SaveTotalXp(120);
            _repo.SaveTotalXp(150);

            var again = new PlayerProgressRepository(_path);
            Assert.That(again.LoadTotalXp(), Is.EqualTo(150));
            Assert.That(File.Exists(_path + ".tmp"), Is.False, "임시 파일은 교체 뒤 남지 않는다");
        }

        [TestCase("not json")]
        [TestCase("{\"schemaVersion\":99,\"totalXp\":50}")]
        [TestCase("{\"schemaVersion\":1,\"totalXp\":-5}")]
        public void BrokenOrOtherSchema_ZeroXp(string content)
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_path, content);

            Assert.That(_repo.LoadTotalXp(), Is.EqualTo(0));
        }
    }
}
