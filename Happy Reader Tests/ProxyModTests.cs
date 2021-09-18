using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;
using Happy_Reader;
using Happy_Reader.Database;
using Happy_Reader.TranslationEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Happy_Reader_Tests
{
	/// <summary>
	/// These tests rely on existing entries, on top of the ones added below, and will not succeed for fresh databases.
	/// </summary>
	[TestClass]
	public class ProxyModTests
	{
		private const string Name1 = "卓也";
		private const string Name1T = "Takuya";
		private const string NameF1 = "アリス";
		private const string NameF1T = "Alice";
		private const string Name2 = "モルガン";
		private const string Name2T = "Morgan";
		private const string Name3 = "クリス";
		private const string Name3T = "Chris";
		private const string Name4 = "ロボト";
		private const string Name4T = "Robot";
		private const string Suffix1 = "さん";
		private const string Suffix1T = "-san";
		private const string Suffix2 = "たち";
		private const string Suffix2T = "-tachi";
		
		private static readonly EntryGame Game;
		private static readonly User User = new User { Id = -2, Username = "Test" };

		static ProxyModTests()
		{
			var testTranslator = new TestTranslator();
			var translatorSettings = new TranslatorSettings();
			translatorSettings.Translators.Add(testTranslator);
			translatorSettings.SelectedTranslator = testTranslator;
			var testDatabase = StaticMethods.Data = new HappyReaderDatabase(StaticMethods.ReaderDatabaseFile, true);
			Game = new EntryGame(-2, true, true);
			PopulateEntries(testDatabase);
			Translator.Instance = new Translator(testDatabase, translatorSettings);
		}

		private static void PopulateEntries(HappyReaderDatabase testDatabase)
		{
			foreach (var entry in StaticMethods.Data.Entries)
			{
				entry.InitGameId();
			}
			var entries = testDatabase.Entries.Where(e => e?.GameData?.Equals(Game) ?? false).ToList();
			var newEntries = new List<Entry>();
			newEntries.Add(GetNameEntry(Name1, Name1T));
			newEntries.Add(GetNameEntry(Name2, Name2T));
			newEntries.Add(GetNameEntry(Name3, Name3T));
			newEntries.Add(GetNameEntry(Name4, Name4T));
			newEntries.Add(GetNameEntry(NameF1, NameF1T, "m.f"));
			newEntries = newEntries.Except(entries, Entry.ClashComparer).ToList();
			testDatabase.AddEntries(newEntries);
		}

		private static Entry GetNameEntry(string input, string output, string role = "m")
		{
			var nameEntry = new Entry
			{
				Input = input,
				Output = output,
				RoleString = role,
				UserId = User.Id,
				Type = EntryType.Name
			};
			nameEntry.SetGameId(Game.GameId, Game.IsUserGame);
			return nameEntry;
		}

		[TestMethod]
		public void NameSuffix1()
		{
			TranslateAndAssert(
				$"私は{Name1}{Suffix1}です。",
				$"I am {Name1T}{Suffix1T}.");
		}

		[TestMethod]
		public void Name1Suffix1_Name2Suffix2()
		{
			TranslateAndAssert(
				$"私は{Name1}{Suffix1}ですそれともあなたは{Name2}{Suffix2}。",
				$"I am {Name1T}{Suffix1T} and you are {Name2T}{Suffix2T}.");
		}

		[TestMethod]
		public void Name1Suffix1_Name1Suffix2()
		{
			TranslateAndAssert(
				$"私は{Name1}{Suffix1}ですそれともあなたは{Name1}{Suffix2}。",
				$"I am {Name1T}{Suffix1T} and you are {Name1T}{Suffix2T}.");
		}

		[TestMethod]
		public void Name1Suffix1_Name2Suffix1()
		{
			TranslateAndAssert(
				$"私は{Name1}{Suffix1}ですそれともあなたは{Name2}{Suffix1}。",
				$"I am {Name1T}{Suffix1T} and you are {Name2T}{Suffix1T}.");
		}

		[TestMethod]
		public void Name1Suffix1_Name1Suffix1()
		{
			TranslateAndAssert(
				$"私は{Name1}{Suffix1}ですそれともあなたは{Name1}{Suffix1}。",
				$"I am {Name1T}{Suffix1T} and you are {Name1T}{Suffix1T}.");
		}

		[TestMethod]
		public void Name1DotName2Suffix1()
		{
			TranslateAndAssert(
				$"私は{Name1}・{Name2}{Suffix1}です。",
				$"I am {Name1T} {Name2T}{Suffix1T}.");
		}

		[TestMethod]
		public void Name2DotName1Suffix1()
		{
			TranslateAndAssert(
				$"私は{Name2}・{Name1}{Suffix1}です。",
				$"I am {Name2T} {Name1T}{Suffix1T}.");
		}

		[TestMethod]
		public void Name1DotName2Suffix1_Name1Suffix1()
		{
			TranslateAndAssert(
				$"私は{Name1}・{Name2}{Suffix1}ですそれともあなたは{Name1}{Suffix1}。",
				$"I am {Name1T} {Name2T}{Suffix1T} and you are {Name1T}{Suffix1T}.");
		}

		[TestMethod]
		public void Name1DotName2_Name1()
		{
			TranslateAndAssert(
				$"私は{Name1}・{Name2}ですそれともあなたは{Name1}。",
				$"I am {Name1T} {Name2T} and you are {Name1T}.");
		}

		[TestMethod]
		public void Name1DotName2_Name1DotName2()
		{
			//todo: dot proxymod should group matches per contents and assign a proxy id like that.
			TranslateAndAssert(
				$"私は{Name1}・{Name2}ですそれともあなたは{Name1}・{Name2}。",
				$"I am {Name1T} {Name2T} and you are {Name1T} {Name2T}.");
		}

		[TestMethod]
		public void Name1DotName2_Name1DotName3()
		{
			TranslateAndAssert(
				$"私は{Name1}・{Name2}ですそれともあなたは{Name1}・{Name3}。",
				$"I am {Name1T} {Name2T} and you are {Name1T} {Name3T}.");
		}


		[TestMethod]
		public void Name1DotName2_Name1DotName3_Name1DotName2()
		{
			TranslateAndAssert(
				$"私は{Name1}・{Name2}ですそれともあなたは{Name1}・{Name3}、最初は彼は{Name4}。",
				$"I am {Name1T} {Name2T} and you are {Name1T} {Name3T}, finally, he is {Name4T}.");
		}

		[TestMethod]
		public void Name1DotName2_Name3DotName4()
		{
			TranslateAndAssert(
					$"私は{Name1}・{Name2}ですそれともあなたは{Name3}・{Name4}。",
					$"I am {Name1T} {Name2T} and you are {Name3T} {Name4T}.");
		}

		[TestMethod]
		public void Name2DotName1Suffix1_Name1Suffix1()
		{
			TranslateAndAssert(
				$"私は{Name2}・{Name1}{Suffix1}ですそれともあなたは{Name1}{Suffix1}。",
				$"I am {Name2T} {Name1T}{Suffix1T} and you are {Name1T}{Suffix1T}.");
		}


		[TestMethod]
		public void SubRoleTest()
		{
			TranslateAndAssert(
				$"{NameF1}{Suffix1}できたそしてもお腹すいたみたい。",
				$"{NameF1T}{Suffix1T} came and she looked hungry.");
		}

		private void TranslateAndAssert(string input, string expectedOutput)
		{
			var translation = Translator.Instance.Translate(User, Game, input, false, false);
			Assert.AreEqual(expectedOutput, translation.Output);
		}
	}
}
