using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Happy_Apps_Core;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;
using System.Diagnostics;
using System.Reflection;

namespace Happy_Apps_Core_Tests
{
    [TestClass]
    public class VndbApiTests
    {
        private readonly VndbConnection _conn = new VndbConnection((message, severity) => Debug.WriteLine($"Severity: {severity}\t {message}"), null, null);
        private readonly MethodInfo _tryQueryNoReplyMethod;

        public VndbApiTests()
        {
            _conn.Login(ClientName, ClientVersion, printCertificates: false);
            _tryQueryNoReplyMethod = typeof(VndbConnection).GetMethod("TryQueryNoReply", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private async Task TryQueryNoReply(string query)
        {
            var result = await (Task<bool>)_tryQueryNoReplyMethod.Invoke(_conn, new object[] { query });
            if (!result) throw new Exception("Query Failed");
            Debug.Print(_conn.LastResponse.JsonPayload);
        }

        [TestMethod]
        public async Task GetVN()
        {
            string multiVNQuery = $"get vn basic,details,tags,stats (id = 1) {{{MaxResultsString}}}";
            await TryQueryNoReply(multiVNQuery);
            var vnRoot = JsonConvert.DeserializeObject<ResultsRoot<VNItem>>(_conn.LastResponse.JsonPayload);
            var vn = vnRoot.Items.Single();
            //basic
            Assert.AreEqual(1, vn.ID, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
            Assert.AreEqual("みんなでニャンニャン", vn.Original, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
            //details
            Assert.AreEqual("https://s.vndb.org/cv/39/20339.jpg", vn.Image, "Failed at details, Payload: " + _conn.LastResponse.JsonPayload);
            //tags
            Assert.IsTrue(vn.Tags.Any(x => x.ID == 2237), "Failed at tags, Payload: " + _conn.LastResponse.JsonPayload);
            //stats
            Assert.IsTrue(vn.Popularity > 0, "Failed at stats, Payload: " + _conn.LastResponse.JsonPayload);
            Assert.IsTrue(vn.Rating > 0, "Failed at stats, Payload: " + _conn.LastResponse.JsonPayload);
        }

        [TestMethod]
        public async Task GetRelease()
        {
            string developerQuery = $"get release basic,producers (vn = 1) {{{MaxResultsString}}}";
            await TryQueryNoReply(developerQuery);
            var relRoot = JsonConvert.DeserializeObject<ResultsRoot<ReleaseItem>>(_conn.LastResponse.JsonPayload);
            var releaseItems = relRoot.Items.OrderBy(x => x.ID);
            var release = releaseItems.First();
            //basic
            Assert.AreEqual(1, release.ID, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
            Assert.AreEqual("みんなでニャンニャン DVD-ROM版 限定版", release.Original, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
            //producers
            var producer = release.Producers.First();
            Assert.AreEqual(1, producer.ID, "Failed at producers, Payload: " + _conn.LastResponse.JsonPayload);
            Assert.AreEqual("闇雲通信", producer.Original, "Failed at producers, Payload: " + _conn.LastResponse.JsonPayload);
        }

        [TestMethod]
        public async Task GetProducer()
        {
            string producerQuery = "get producer basic (id = 1)";
            await TryQueryNoReply(producerQuery);
            var root = JsonConvert.DeserializeObject<ResultsRoot<ProducerItem>>(_conn.LastResponse.JsonPayload);
            ProducerItem producer = root.Items.Single();
            //basic
            Assert.AreEqual(1, producer.ID, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
            Assert.AreEqual("闇雲通信", producer.Original, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
        }

        [TestMethod]
        public async Task GetCharacter()
        {
            string charsForVNQuery = $"get character traits,vns (vn = 1) {{{MaxResultsString}}}";
            await TryQueryNoReply(charsForVNQuery);
            var charRoot = JsonConvert.DeserializeObject<ResultsRoot<CharacterItem>>(_conn.LastResponse.JsonPayload);
            var characters = charRoot.Items.OrderBy(x => x.ID);
            var character = characters.First();
            Assert.AreEqual(1783, character.ID, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
            //traits
            Assert.IsTrue(character.Traits.Any(x => x.ID == 7), "Failed at traits, Payload: " + _conn.LastResponse.JsonPayload);
            //vns
            Assert.IsTrue(character.VNs.First().ID == 1, "Failed at vns, Payload: " + _conn.LastResponse.JsonPayload);
        }

        [TestMethod]
        public async Task GetUser()
        {
            string userQuery = "get user basic (id = 1) ";
            await TryQueryNoReply(userQuery);
            var userRoot = JsonConvert.DeserializeObject<ResultsRoot<UserItem>>(_conn.LastResponse.JsonPayload);
            var user = userRoot.Items.Single();
            //basic
            Assert.AreEqual("multi", user.Username, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
            userQuery = "get user basic (username = \"multi\") ";
            await TryQueryNoReply(userQuery);
            userRoot = JsonConvert.DeserializeObject<ResultsRoot<UserItem>>(_conn.LastResponse.JsonPayload);
            user = userRoot.Items.Single();
            //basic
            Assert.AreEqual(1, user.ID, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
        }
    }

    [TestClass]
    public class VndbApiLoginTests
    {
        private readonly VndbConnection _conn = new VndbConnection((message, severity) => Debug.WriteLine($"Severity: {severity}\t {message}"), null, null);

        [TestMethod]
        public void LoginWithoutCredentials()
        {
            _conn.Login(ClientName, ClientVersion, printCertificates: false);
            Assert.AreEqual(_conn.LogIn == VndbConnection.LogInStatus.Yes, _conn.LogIn, _conn.LastResponse.JsonPayload);
        }

        [TestMethod, Ignore]
        public void LoginWithCredentials()
        {
            _conn.Login(ClientName, ClientVersion, "hacTest", "hacTest".ToCharArray(), printCertificates: false);
            Assert.AreEqual(_conn.LogIn == VndbConnection.LogInStatus.YesWithPassword, _conn.LogIn, _conn.LastResponse.JsonPayload);
        }

    }
}
