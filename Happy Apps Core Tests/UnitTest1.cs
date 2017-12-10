using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Happy_Apps_Core;
using Newtonsoft.Json;
using static Happy_Apps_Core.StaticHelpers;

namespace Happy_Apps_Core_Tests
{
    [TestClass]
    public class VndbApiTests
    {
        private readonly VndbConnection _conn = new VndbConnection(null, null, null);

        public VndbApiTests()
        {
            _conn.Login(ClientName, ClientVersion);
        }

        [TestMethod]
        public async Task GetVN()
        {
            string multiVNQuery = $"get vn basic,details,tags,stats (id = 1) {{{MaxResultsString}}}";
            var queryResult = await _conn.TryQueryNoReply(multiVNQuery);
            if (!queryResult) throw new Exception("GetVN Query Failed");
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
            var queryResult = await _conn.TryQueryNoReply(developerQuery);
            if (!queryResult) throw new Exception("GetRelease Query Failed");
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
            var producerResult = await _conn.TryQueryNoReply(producerQuery);
            if (!producerResult) throw new Exception("GetProducer Query Failed");
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
            var queryResult = await _conn.TryQueryNoReply(charsForVNQuery);
            if (!queryResult) throw new Exception("GetCharacter Query Failed");
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
            var queryResult = await _conn.TryQueryNoReply(userQuery);
            if (!queryResult) throw new Exception("GetUser Query Failed");
            var userRoot = JsonConvert.DeserializeObject<ResultsRoot<UserItem>>(_conn.LastResponse.JsonPayload);
            var user = userRoot.Items.Single();
            //basic
            Assert.AreEqual("multi", user.Username, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
            userQuery = "get user basic (username = \"multi\") ";
            queryResult = await _conn.TryQueryNoReply(userQuery);
            if (!queryResult) throw new Exception("GetUser Query Failed");
            userRoot = JsonConvert.DeserializeObject<ResultsRoot<UserItem>>(_conn.LastResponse.JsonPayload);
            user = userRoot.Items.Single();
            //basic
            Assert.AreEqual(1, user.ID, "Failed at basic, Payload: " + _conn.LastResponse.JsonPayload);
        }
    }

    [TestClass]
    public class VndbApiLoginTests
    {
        private readonly VndbConnection _conn = new VndbConnection(null, null, null);

        [TestMethod]
        public void LoginWithoutCredentials()
        {
            _conn.Login(ClientName, ClientVersion);
            Assert.AreEqual(VndbConnection.APIStatus.Ready, _conn.Status, _conn.LastResponse.JsonPayload);
        }

        [TestMethod, Ignore]
        public void LoginWithCredentials()
        {
            _conn.Login(ClientName, ClientVersion, "hacTest", "hacTest".ToCharArray());
            Assert.AreEqual(ResponseType.Ok, _conn.LastResponse.Type, _conn.LastResponse.JsonPayload);
        }

    }
}
