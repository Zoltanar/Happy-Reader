using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using DatabaseDumpReader.DumpItems;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Newtonsoft.Json;

namespace DatabaseDumpReader
{
	public class DumpReader
	{
		public int UserId { get; }
		public string DumpFolder { get; }
		public string OutputFilePath { get; }
		public VisualNovelDatabase Database { get; }
		public SuggestionScorer SuggestionScorer { get; }
		public Dictionary<int, Release> Releases { get; } = new Dictionary<int, Release>();
		public Dictionary<int, List<string>> LangReleases { get; } = new Dictionary<int, List<string>>();
		public Dictionary<int, List<int>> ProducerReleases { get; } = new Dictionary<int, List<int>>();
		public Dictionary<int, List<Release>> VnReleases { get; } = new Dictionary<int, List<Release>>();
		public Dictionary<int, DumpAnime> Animes { get; } = new Dictionary<int, DumpAnime>();
		public Dictionary<int, List<DumpVnAnime>> VnAnimes { get; } = new Dictionary<int, List<DumpVnAnime>>();
		public Dictionary<string, DumpScreen> Images { get; } = new Dictionary<string, DumpScreen>();
		public Dictionary<int, List<DumpVnScreen>> VnScreens { get; } = new Dictionary<int, List<DumpVnScreen>>();
		public Dictionary<int, List<DumpRelation>> VnRelations { get; } = new Dictionary<int, List<DumpRelation>>();
		public Dictionary<int, UserVN.LabelKind> UserLabels { get; } = new Dictionary<int, UserVN.LabelKind>();
		public Dictionary<int, UserVn> UserVns { get; } = new Dictionary<int, UserVn>();
		public List<VnTag> VnTags { get; } = new List<VnTag>();
		public Dictionary<int, List<DumpVote>> Votes { get; private set; }

		public DumpReader(string dumpFolder, string dbFilePath, int userId)
		{
			if (!Directory.Exists(dumpFolder)) throw new IOException($"Dump folder does not exist: '{dumpFolder}'");
			var dbFile = new FileInfo(dbFilePath);
			if (!dbFile.Exists) throw new IOException($"Original database does not exist: '{dbFile}'");
			var dbDirectory = Path.GetDirectoryName(dbFile.FullName) ?? throw new ArgumentNullException(nameof(Path.GetDirectoryName));
			var backupPath = Path.Combine(dbDirectory, $"{Path.GetFileNameWithoutExtension(dbFile.FullName)}-DRB{DateTime.Now:yyyyMMdd-HHmmss}{dbFile.Extension}");
			StaticHelpers.Logger.ToFile($"Backing up database to {backupPath}");
			File.Copy(dbFile.FullName, backupPath);
			UserId = userId;
			DumpFolder = dumpFolder;
			OutputFilePath = dbFile.FullName;
			Database = new VisualNovelDatabase(OutputFilePath, false);
			StaticHelpers.LocalDatabase = Database;
			Database.DeleteForDump();
			DumpFiles.Load();
			SuggestionScorer = new SuggestionScorer(
				StaticHelpers.CSettings.GetTagScoreDictionary(),
				StaticHelpers.CSettings.GetTraitScoreDictionary(),
				Database);
		}

		public void Run(DateTime dumpDate)
		{
			StaticHelpers.Logger.ToFile("Starting Dump Reader...");
			Console.ForegroundColor = ConsoleColor.DarkYellow;
			StaticHelpers.Logger.ToFile("Loading Tag/Trait Dump files...");
			DumpFiles.Load();
			var votesFilePath = FindVotesFile();
			Load<ListedProducer>((i, t) => Database.Producers.Add(i, false, true, t), "db\\producers");
			LoadReleases();
			LoadAndResolveTags();
			LoadStaff();
			LoadCharacters();
			var votesUngrouped = new List<DumpVote>();
			Load<DumpVote>((i, t) => votesUngrouped.Add(i), votesFilePath, false);
			Votes = votesUngrouped.GroupBy(vote => vote.VNId).ToDictionary(g => g.Key, g => g.ToList());
			LoadAnimeScreensRelations();
			LoadUserVn();
			Load<ListedVN>((i, t) =>
			{
				ResolveOtherForVn(i);
				Database.VisualNovels.Add(i, false, true, t);
				ResolveUserVnForVn(i, t);
			}, "db\\vn");
			SaveLatestDumpUpdate(dumpDate);
			Console.ResetColor();
			StaticHelpers.Logger.ToFile("Completed.");
		}

		private void LoadStaff()
		{   
			Load<StaffItem>((i, t) =>
			{
				Database.StaffItems.Add(i, false, true, t);
			}, "db\\staff");
			Load<StaffAlias>((i, t) =>
			{
				Database.StaffAliases.Add(i, false, true, t);
			}, "db\\staff_alias");
			Load<VnStaff>((i, t) =>
			{
				Database.VnStaffs.Add(i, false, true, t);
			}, "db\\vn_staff");
			Load<VnSeiyuu>((i, t) =>
			{
				Database.VnSeiyuus.Add(i, false, true, t);
			}, "db\\vn_seiyuu");
		}

		private void LoadAnimeScreensRelations()
		{
			Load<DumpAnime>((i, t) => Animes.Add(i.Id, i), "db\\anime");
			Load<DumpVnAnime>((i, t) =>
			{
				if (!VnAnimes.ContainsKey(i.VnId)) VnAnimes[i.VnId] = new List<DumpVnAnime>();
				VnAnimes[i.VnId].Add(i);
			}, "db\\vn_anime");
			Load<DumpScreen>((i, t) => Images.Add(i.Id, i), "db\\images");
			Load<DumpVnScreen>((i, t) =>
			{
				if (!VnScreens.ContainsKey(i.VnId)) VnScreens[i.VnId] = new List<DumpVnScreen>();
				VnScreens[i.VnId].Add(i);
			}, "db\\vn_screenshots");
			Load<DumpRelation>((i, t) =>
			{
				if (!VnRelations.ContainsKey(i.Id)) VnRelations[i.Id] = new List<DumpRelation>();
				VnRelations[i.Id].Add(i);
			}, "db\\vn_relations");
		}

		private void LoadCharacters()
		{
			Load<CharacterVN>((i, t) =>
			{
				if (Database.CharacterVNs.ByKey(i.ListKey,i.Key) != null) return;
				Database.CharacterVNs.Add(i, false, true, t);
			}, "db\\chars_vns");
			Load<DbTrait>((i, t) =>
			{
				Database.Traits.Add(i, false, true, t);
			}, "db\\chars_traits");
			Load<CharacterItem>((i, t) =>
			{
				SuggestionScorer.SetScore(i, Database.Traits[i.ID].Select(trait=> trait.TraitId));
				Database.Characters.Add(i, false, true, t);
			}, "db\\chars");
		}

		private void LoadReleases()
		{
			Load<ProducerRelease>((i, t) =>
			{
				if (!i.Developer) return;
				if (!ProducerReleases.ContainsKey(i.ReleaseId)) ProducerReleases[i.ReleaseId] = new List<int>();
				ProducerReleases[i.ReleaseId].Add(i.ProducerId);
			}, "db\\releases_producers");
			Load<LangRelease>((i, t) =>
			{
				if (!LangReleases.ContainsKey(i.ReleaseId)) LangReleases[i.ReleaseId] = new List<string>();
				LangReleases[i.ReleaseId].Add(i.Lang);
			}, "db\\releases_lang");
			Load<Release>((i, t) =>
			{
				if (i.Type != "trial") Releases[i.ReleaseId] = i;
			}, "db\\releases");
			Load<VnRelease>((i, t) =>
			{
				if (!VnReleases.ContainsKey(i.VnId)) VnReleases[i.VnId] = new List<Release>();
				if (!Releases.TryGetValue(i.ReleaseId, out var release)) return;
				release.Languages = LangReleases[i.ReleaseId];
				ProducerReleases.TryGetValue(i.ReleaseId, out var producerRelease);
				release.Producers = producerRelease ?? new List<int>();
				VnReleases[i.VnId].Add(release);
			}, "db\\releases_vn");
		}

		private void ResolveUserVnForVn(ListedVN vn, SQLiteTransaction transaction)
		{
			if (!UserVns.TryGetValue(vn.VNID, out var dumpUserVn)) return;
			var userVn = new UserVN
			{
				UserId = UserId,
				VNID = vn.VNID,
				ULNote = dumpUserVn.Notes,
				Added = dumpUserVn.Added,
				Labels = dumpUserVn.Labels.ToHashSet()
			};
			if (Votes.ContainsKey(vn.VNID))
			{
				var vote = Votes[vn.VNID].FirstOrDefault(v => v.UserId == UserId);
				if (vote != null)
				{
					userVn.Vote = vote.Vote;
					userVn.VoteAdded = vote.Date;
				}
			}
			Database.UserVisualNovels.Add(userVn, false, true, transaction);
		}

		private void LoadUserVn()
		{
			Load<UserLabel>((i, t) =>
			{
				if (i.UserId != UserId) return;
				UserLabels.Add(i.LabelId, i.Label);
			}, "db\\ulist_labels");
			Load<UserVn>((i, t) =>
			{
				if (i.UserId != UserId) return;
				UserVns.Add(i.VnId, i);
			}, "db\\ulist_vns");
			Load<UserVnLabel>((i, t) =>
			{
				if (i.UserId != UserId) return;
				UserVns[i.VnId].Labels.Add(UserLabels[i.LabelId]);
			}, "db\\ulist_vns_labels");
		}

		private string FindVotesFile()
		{
			var file = new DirectoryInfo(DumpFolder).EnumerateFiles("vndb-votes-*", SearchOption.TopDirectoryOnly).FirstOrDefault();
			if (file == null) throw new ArgumentNullException(nameof(file), "Votes file was not found.");
			return file.FullName;
		}

		private void LoadAndResolveTags()
		{
			Load<VnTag>((i, t) =>
			{
				if (i.Ignore) return;
				VnTags.Add(i);
			}, "db\\tags_vn");
			StaticHelpers.Logger.ToFile("Resolving Tags...");
			var groupedTags = VnTags.GroupBy(t => (t.TagId, t.VnId)).ToArray();
			WrapInTransaction(trans =>
			{
				foreach (var group in groupedTags)
				{
					var spoilerTags = group.Where(t => t.Spoiler.HasValue).ToList();
					var dbTag = new DbTag
					{
						TagId = group.Key.TagId,
						VNID = group.Key.VnId,
						Score = group.Average(t => t.Vote),
						// ReSharper disable once PossibleInvalidOperationException
						Spoiler = spoilerTags.Any() ? (int)Math.Round(spoilerTags.Average(t => t.Spoiler.Value)) : 0
					};
					dbTag.SetCategory();
					Database.Tags.Add(dbTag, false, true, trans);
				}
			});
		}

		private void Load<T>(Action<T, SQLiteTransaction> addToList, string filePath, bool useHeaderFile = true) where T : IDumpItem, new()
		{
			StaticHelpers.Logger.ToFile($"Loading for {typeof(T).Name}...");
			var lines = File.ReadAllLines(Path.Combine(DumpFolder, filePath));
			new T().SetDumpHeaders((useHeaderFile ? File.ReadAllLines(Path.Combine(DumpFolder, filePath + ".header")).Single() : string.Empty).Split('\t'));
			WrapInTransaction(trans =>
			{
				foreach (var line in lines)
				{
					Debug.Assert(line != null, nameof(line) + " != null");
					var parts = line.Split('\t');
					var item = new T();
					item.LoadFromStringParts(parts);
					addToList(item, trans);
				}
			});
		}

		private void WrapInTransaction(Action<SQLiteTransaction> action, [CallerMemberName] string caller = null)
		{
			Database.Connection.Open();
			SQLiteTransaction transaction = null;
			try
			{
				transaction = Database.Connection.BeginTransaction();
				action(transaction);
				transaction.Commit();
			}
			catch (Exception ex)
			{
				transaction?.Rollback();
				StaticHelpers.Logger.ToFile(ex, caller);
				throw;
			}
			finally
			{
				Database.Connection.Close();
			}
		}

		private void ResolveOtherForVn(ListedVN vn)
		{
			if (Votes.ContainsKey(vn.VNID))
			{
				var votes = Votes[vn.VNID];
				vn.VoteCount = votes.Count;
				if (votes.Count > 0) vn.Rating = votes.Average(v => v.Vote);
			}
			ResolveRelease();
			ResolveRelations();
			ResolveAnime();
			ResolveScreens();
			SuggestionScorer.SetScore(vn, false);

			void ResolveScreens()
			{
				if (!string.IsNullOrWhiteSpace(vn.ImageId) && Images.TryGetValue(vn.ImageId, out var cover)) vn.ImageNSFW = cover.Nsfw;
				var screensObject = VnScreens.TryGetValue(vn.VNID, out var screens)
					? screens.Select(r => r.ToScreenItem(Images)).Where(i => i != null).ToArray()
					: Array.Empty<ScreenItem>();
				vn.SetScreens(JsonConvert.SerializeObject(screensObject), screensObject);
			}
			void ResolveAnime()
			{
				var animeObject = VnAnimes.TryGetValue(vn.VNID, out var animes)
					? animes.Select(r => r.ToAnimeItem(Animes)).ToArray()
					: Array.Empty<AnimeItem>();
				vn.SetAnime(JsonConvert.SerializeObject(animeObject), animeObject);
			}
			void ResolveRelations()
			{
				var relationsObject = VnRelations.TryGetValue(vn.VNID, out var relations)
					? relations.Select(r => r.ToRelationItem()).ToArray()
					: Array.Empty<RelationsItem>();
				vn.SetRelations(JsonConvert.SerializeObject(relationsObject), relationsObject);
			}
			void ResolveRelease()
			{
				if (!VnReleases.ContainsKey(vn.VNID)) return;
				var releases = VnReleases[vn.VNID].OrderBy(r => r.Released).ToList();
				var release = releases.FirstOrDefault();
				if (release == null) return;
				vn.SetReleaseDate(StringToDateString(release.Released));
				var languages = new VNLanguages(release.Languages.ToArray(),
					releases.SelectMany(r => r.Languages).Distinct().ToArray());
				vn.Languages = JsonConvert.SerializeObject(languages);
				vn.ProducerID = releases.SelectMany(r => r.Producers).FirstOrDefault(p => Database.Producers[p] != null);
				vn.ReleaseLink = release.Website;
			}
		}

		private static string StringToDateString(string released)
		{
			if (string.IsNullOrWhiteSpace(released)) return "";
			if (released.StartsWith("0000")) { }
			var day = released.Substring(6, 2);
			var month = released.Substring(4, 2);
			var year = released.Substring(0, 4);
			if (year == "9999") return "";
			if (month == "99") return year;
			return day == "99" ? $"{year}-{month}" : $"{year}-{month}-{day}";
		}

		public const string LatestDumpUpdateKey = @"LatestDumpUpdate";
		public const string DateFormat = @"yyyy-MM-dd";

		public static DateTime? GetLatestDumpUpdate(string databaseFile)
		{
			if (!File.Exists(databaseFile)) return null;
			var database = new VisualNovelDatabase(databaseFile, false);
			database.TableDetails.Load(true);
			var datePair = database.TableDetails[LatestDumpUpdateKey];
			if (datePair is null || string.IsNullOrWhiteSpace(datePair.Value)) return null;
			return DateTime.ParseExact(datePair.Value, DateFormat, CultureInfo.InvariantCulture);
		}

		public void SaveLatestDumpUpdate(DateTime updateDate)
		{
			var tableDetail = new TableDetail { Key = LatestDumpUpdateKey, Value = updateDate.ToString(DateFormat, CultureInfo.InvariantCulture) };
			Database.TableDetails.Upsert(tableDetail, true);
		}
	}
}