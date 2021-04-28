using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Reader.Database;
using JetBrains.Annotations;

namespace Happy_Reader.ViewModel
{
	public class InformationViewModel : INotifyPropertyChanged
	{
		public string About { get; } = $"{StaticHelpers.ClientName} {StaticHelpers.ClientVersion}";

		public string DatabaseDate { get; private set; }
		public string UserDatabaseSize { get; private set; }
		public string VnDatabaseSize { get; private set; }
		public string TranslationsData { get; private set; }
		public string RecordedTime { get; private set; }
		public string ApproxVndbTime { get; private set; }
		public string ApproxOverallTime { get; private set; }
		public HappyReaderDatabase UserDatabase { get; private set; }
		private DateTime OldTranslationsTime = DateTime.UtcNow.AddMonths(-2);

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Initialise(VisualNovelDatabase vnData, HappyReaderDatabase userGameData)
		{
			UserDatabase = userGameData;
			var databaseDate = vnData.GetLatestDumpUpdate();
			DatabaseDate = $"Database Dump Date: {databaseDate?.ToString("yyyy-MM-dd")}";
			VnDatabaseSize = $"VN Database Size: {GetFileSizeStringForDb(vnData.Connection)}";
			SetUserDatabaseData(userGameData);
			SetTimeSpentData(userGameData);
			OnPropertyChanged(null);
		}

		private void SetTimeSpentData(HappyReaderDatabase userGameData)
		{
			var recordedTime = new TimeSpan();
			var approxVnTime = new TimeSpan();
			var approxOverallTime = new TimeSpan();
			foreach (var gameGroup in userGameData.UserGames.GroupBy(u => u.VNID))
			{
				if (gameGroup.Key == null || gameGroup.Count() == 1)
				{
					foreach (var game in gameGroup)
					{
						recordedTime = recordedTime.Add(game.TimeOpen);
						if (!game.HasVN || !HasPlayed(game.VN, out var ratio))
						{
							approxOverallTime = approxOverallTime.Add(game.TimeOpen);
							continue;
						}

						var approxTime = GetApproxTime(game.VN.LengthTime, ratio);
						var timeSpan = TimeSpan.FromHours(approxTime);
						approxVnTime = approxVnTime.Add(timeSpan);
						var timeToAdd = game.TimeOpen.TotalHours < approxTime * 0.25d ? timeSpan : game.TimeOpen;
						approxOverallTime = approxOverallTime.Add(timeToAdd);
					}
				}
				else
				{
					var timeForVn = new TimeSpan();
					TimeSpan? vnTime = null;
					foreach (var game in gameGroup)
					{
						recordedTime = recordedTime.Add(game.TimeOpen);
						timeForVn = timeForVn.Add(game.TimeOpen);
						HasPlayed(game.VN, out var ratio);
						var approxTime = GetApproxTime(game.VN.LengthTime, ratio);
						vnTime ??= TimeSpan.FromHours(approxTime);
					}

					Debug.Assert(vnTime != null, nameof(vnTime) + " != null");
					var timeToAdd = timeForVn.TotalHours < vnTime.Value.TotalHours * 0.25d ? vnTime : timeForVn;
					approxOverallTime = approxOverallTime.Add(timeToAdd.Value);
				}
			}

			RecordedTime = $"Time playing as recorded by app: {recordedTime.TotalHours:0} hours.";
			ApproxVndbTime =
				$"Time playing as approximated by VN length time (excludes titles outside VNDB): {approxVnTime.TotalHours:0} hours.";
			ApproxOverallTime =
				$"Time playing as approximated by recorded times and VN length time: {approxOverallTime.TotalHours:0} hours.";
		}

		private void SetUserDatabaseData(HappyReaderDatabase userGameData)
		{
			UserDatabaseSize = $"User Database Size: {GetFileSizeStringForDb(userGameData.Database.Connection)}";
			var cachedTranslations = userGameData.SqliteTranslations.Count();
			var cachedTranslationsOld = userGameData.SqliteTranslations.Count(t => t.Timestamp < OldTranslationsTime);
			TranslationsData = $"Cached Translations: {cachedTranslations}, 2+ Months Old: {cachedTranslationsOld}";
			var mostUsedTranslation = userGameData.SqliteTranslations.OrderByDescending(t => t.Count).FirstOrDefault();
			if (mostUsedTranslation != null) TranslationsData += $", Most Used: {mostUsedTranslation.Input}>{mostUsedTranslation.Output} ({mostUsedTranslation.Count} times)";
		}

		private static string GetFileSizeStringForDb(System.Data.Common.DbConnection connection)
		{
			var dataSource = GetSourceFromDatabase(connection);
			try
			{
				if (!System.IO.File.Exists(dataSource)) return "File not found.";
				var sizeBytes = new System.IO.FileInfo(dataSource).Length;
				return $"{sizeBytes / 1024d / 1024d:0} MB";
			}
			catch (System.IO.IOException ex)
			{
				return $"Failed to access file {ex}";
			}
		}

		private static string GetSourceFromDatabase(System.Data.Common.DbConnection connection)
		{
			var connectionString = connection.ConnectionString;
			var parameters = connectionString.Split(';').Select(s =>
			{
				var parts = s.Split('=');
				if(parts.Length != 2) { }
				return new[] {parts[0].ToLowerInvariant(), parts[1]};
			});
			var sourcePart = parameters.FirstOrDefault(p => p[0].Equals("attachdbfilename")) ??
			                 parameters.FirstOrDefault(p => p[0].Equals("datasource")) ??
			                 parameters.FirstOrDefault(p => p[0].Equals("data source"));
			var source = sourcePart?[1] ?? connection.DataSource;
			return source;
		}

		private static double GetApproxTime(LengthFilterEnum? length, double ratio)
		{
			return length switch
			{
				LengthFilterEnum.NA => 5 * ratio,
				LengthFilterEnum.UnderTwoHours => 1 * ratio,
				LengthFilterEnum.TwoToTenHours => 6 * ratio,
				LengthFilterEnum.TenToThirtyHours => 20 * ratio,
				LengthFilterEnum.ThirtyToFiftyHours => 40 * ratio,
				LengthFilterEnum.OverFiftyHours => 60 * ratio,
				null => 0,
				_ => throw new ArgumentOutOfRangeException(nameof(length), length, null)
			};
		}

		private static bool HasPlayed(ListedVN userGameVN, out double ratio)
		{
			switch (userGameVN?.UserVN?.PriorityLabel)
			{
				case UserVN.LabelKind.Playing:
				case UserVN.LabelKind.Dropped:
					ratio = 0.25;
					break;
				case UserVN.LabelKind.Stalled:
					ratio = 0.5;
					break;
				case UserVN.LabelKind.Finished:
					ratio = 1;
					break;
				default:
					ratio = 0;
					return false;
			}
			return true;
		}
		
		public void DeletedCachedTranslations()
		{
			UserDatabase.DeleteCachedTranslationsOlderThan(OldTranslationsTime);
			SetUserDatabaseData(UserDatabase);
			OnPropertyChanged(null);
		}
	}
}
