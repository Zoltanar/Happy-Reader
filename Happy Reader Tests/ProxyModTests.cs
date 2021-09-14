using System.Collections.Generic;
using System.Linq;
using Happy_Apps_Core.Database;
using Happy_Reader;
using Happy_Reader.Database;
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
		private const string Name2 = "モルガン";
		private const string Name2T = "Morgan";
		private const string Suffix1 = "さん";
		private const string Suffix1T = "-san";
		private const string Suffix2 = "たち";
		private const string Suffix2T = "-tachi";

		private static readonly Translator Translator;
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
			Translator = new Translator(testDatabase, translatorSettings);
			Translation.Translator = Translator;
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
			newEntries = newEntries.Except(entries, Entry.ClashComparer).ToList();
			testDatabase.AddEntries(newEntries);
		}

		private static Entry GetNameEntry(string input, string output)
		{
			var nameEntry = new Entry
			{
				Input = input,
				Output = output,
				RoleString = "m",
				UserId = User.Id,
				Type = EntryType.Name
			};
			nameEntry.SetGameId(Game.GameId, Game.IsUserGame);
			return nameEntry;
		}

		[TestMethod]
		public void mSuff1()
		{
			TranslateAndAssert(
				$"私は{Name1}{Suffix1}です。",
				$"I am {Name1T}{Suffix1T}.");
		}

		[TestMethod]
		public void m1Suff1_m1Suff2()
		{
			TranslateAndAssert(
				$"私は{Name1}{Suffix1}ですそれともあなたは{Name1}{Suffix2}。",
				$"I am {Name1T}{Suffix1T} and you are {Name1T}{Suffix2T}.");
		}

		[TestMethod]
		public void m1Suff1_m2Suff1()
		{
			TranslateAndAssert(
				$"私は{Name1}{Suffix1}ですそれともあなたは{Name2}{Suffix1}。",
				$"I am {Name1T}{Suffix1T} and you are {Name2T}{Suffix1T}.");
		}

		[TestMethod]
		public void m1Suff1_m1Suff1()
		{
			TranslateAndAssert(
				$"私は{Name1}{Suffix1}ですそれともあなたは{Name1}{Suffix1}。",
				$"I am {Name1T}{Suffix1T} and you are {Name1T}{Suffix1T}.");
		}

		[TestMethod]
		public void m1Dotm2Suff1()
		{
			TranslateAndAssert(
				$"私は{Name1}・{Name2}{Suffix1}です。",
				$"I am {Name1T} {Name2T}{Suffix1T}.");
		}

		[TestMethod]
		public void m2Dotm1Suff1()
		{
			TranslateAndAssert(
				$"私は{Name2}・{Name1}{Suffix1}です。",
				$"I am {Name2T} {Name1T}{Suffix1T}.");
		}

		[TestMethod]
		public void m1Dotm2Suff1_m1Suff1()
		{
			TranslateAndAssert(
				$"私は{Name1}・{Name2}{Suffix1}ですそれともあなたは{Name1}{Suffix1}。",
				$"I am {Name1T} {Name2T}{Suffix1T} and you are {Name1T}{Suffix1T}.");
		}

		[TestMethod]
		public void m2Dotm1Suff1_m1Suff1()
		{
			TranslateAndAssert(
				$"私は{Name2}・{Name1}{Suffix1}ですそれともあなたは{Name1}{Suffix1}。",
				$"I am {Name2T} {Name1T}{Suffix1T} and you are {Name1T}{Suffix1T}.");
		}

		private void TranslateAndAssert(string input, string expectedOutput)
		{
			var translation = Translator.Translate(User, Game, input, false, false);
			Assert.AreEqual(expectedOutput, translation.Output);
		}
	}
}
