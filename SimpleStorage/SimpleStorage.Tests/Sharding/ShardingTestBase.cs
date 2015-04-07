using System.Threading.Tasks;
using NUnit.Framework;

namespace SimpleStorage.Tests.Sharding
{
    [TestFixture]
    public class ShardingTestBase
    {
        private const bool RunServersFromTests = true;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            if (RunServersFromTests)
            {
                Program.ShouldRunInfinitely = true;
                var masterTask = new Task(() => Program.Main(new[] { "-p", "16000", "--sp", "16001,16002" }));
                masterTask.Start();
                var slave1Task = new Task(() => Program.Main(new[] { "-p", "16001", "--sp", "16000,16002" }));
                slave1Task.Start();
                var slave2Task = new Task(() => Program.Main(new[] { "-p", "16002", "--sp", "16000,16001" }));
                slave2Task.Start();
            }
        }
    }
}
