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
		public string RecordedTime { get; private set; }
		public string ApproxVndbTime { get; private set; }
		public string ApproxOverallTime { get; private set; }

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void Initialise(VisualNovelDatabase vnData, HappyReaderDatabase userGameData)
		{
			var databaseDate = vnData.GetLatestDumpUpdate();
			DatabaseDate = $"Database Dump Date: {databaseDate?.ToString("yyyy-MM-dd")}";
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
			RecordedTime = $"Time playing as recorded by app: {recordedTime.TotalHours:0.00} hours.";
			ApproxVndbTime = $"Time playing as approximated by VN length time (excludes titles outside VNDB): {approxVnTime.TotalHours:0.00} hours.";
			ApproxOverallTime = $"Time playing as approximated by recorded times and VN length time: {approxOverallTime.TotalHours:0.00} hours.";
			OnPropertyChanged(null);
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
	}
}
