using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Happy_Apps_Core;
using Happy_Apps_Core.Database;
using Happy_Apps_Core.Translation;
using Happy_Reader.Database;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace Happy_Reader.ViewModel
{
    public class InformationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private DateTime OldTranslationsTime = DateTime.UtcNow.AddMonths(-2);

        public string About { get; } = $"{StaticHelpers.ClientName} {StaticHelpers.ClientVersion}";
        public string DatabaseDate { get; private set; }
        public string UserDatabaseSize { get; private set; }
        public string VnDatabaseSize { get; private set; }
        public string VnImagesSize { get; private set; }
        public string TranslationsData { get; private set; }
        public string LogsSize { get; private set; }
        public string RecordedTime { get; private set; }
        public string ApproxVndbTime { get; private set; }
        public string ApproxOverallTime { get; private set; }
        public HappyReaderDatabase UserDatabase { get; private set; }
        public EntryGame ExportGame { get; set; } = EntryGame.None;

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
            VnImagesSize = $"VNDB Images Size: {GetFileSizeStringForFolder(StaticMethods.Settings.CoreSettings.ImageFolderPath)}";
            SetLogsSize();
            SetUserDatabaseData(userGameData);
            SetTimeSpentData(userGameData);
            OnPropertyChanged(null);
        }

        public void SetLogsSize()
        {
            LogsSize = $"Logs Folder Size: {GetFileSizeStringForFolder(StaticHelpers.LogsFolder)}";
            OnPropertyChanged(nameof(LogsSize));
        }

        private string GetFileSizeStringForFolder(string directory)
        {
            try
            {
                if (!Directory.Exists(directory)) return "Folder not found.";
                var directoryInfo = new DirectoryInfo(directory);
                var sizeBytes = directoryInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(i => i.Length);
                return GetSizeStringFromBytes(sizeBytes);
            }
            catch (IOException ex)
            {
                return $"Failed to access folder {ex}";
            }
            catch (Exception ex)
            {
                return $"Failed: {ex}";
            }
        }

        private static string GetSizeStringFromBytes(long bytes)
        {
            var mb = bytes / 1024d / 1024d;
            return mb > 10_000 ? $"{mb / 1024d:0.00} GB" : $"{bytes / 1024d / 1024d:0} MB";
        }

        private void SetTimeSpentData(HappyReaderDatabase userGameData)
        {
            var recordedTime = new TimeSpan();
            var approxVnTime = new TimeSpan();
            var approxOverallTime = new TimeSpan();
            foreach (var gameGroup in userGameData.UserGames.GroupBy(u => u.HasVN ? u.VNID : null))
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
            UserDatabaseSize = $"User Database Size: {GetFileSizeStringForDb(userGameData.Connection)}";
            var cachedTranslations = userGameData.Translations.Count();
            var cachedTranslationsOld = userGameData.Translations.Count(t => t.Timestamp < OldTranslationsTime);
            TranslationsData = $"Cached Translations: {cachedTranslations}, 2+ Months Old: {cachedTranslationsOld}";
            var mostUsedTranslation = userGameData.Translations.OrderByDescending(t => t.Count).FirstOrDefault();
            if (mostUsedTranslation != null) TranslationsData += $", Most Used: {mostUsedTranslation.Input}>{mostUsedTranslation.Output} ({mostUsedTranslation.Count} times)";
        }

        private static string GetFileSizeStringForDb(System.Data.Common.DbConnection connection)
        {
            var dataSource = GetSourceFromDatabase(connection);
            try
            {
                if (!File.Exists(dataSource)) return "File not found.";
                var sizeBytes = new FileInfo(dataSource).Length;
                return GetSizeStringFromBytes(sizeBytes);
            }
            catch (IOException ex)
            {
                return $"Failed to access file {ex}";
            }
            catch (Exception ex)
            {
                return $"Failed: {ex}";
            }
        }

        private static string GetSourceFromDatabase(System.Data.Common.DbConnection connection)
        {
            var connectionString = connection.ConnectionString;
            var parameters = connectionString.Split(';').Select(s =>
            {
                var parts = s.Split('=');
                return new[] { parts[0].ToLowerInvariant(), parts[1] };
            }).ToList();
            // ReSharper disable StringLiteralTypo
            var sourcePart = parameters.FirstOrDefault(p => p[0].Equals("attachdbfilename")) ??
                             parameters.FirstOrDefault(p => p[0].Equals("datasource")) ??
                             parameters.FirstOrDefault(p => p[0].Equals("data source"));
            // ReSharper restore StringLiteralTypo
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

        public void DeletedCachedTranslations(bool deleteAll)
        {
            if (deleteAll) UserDatabase.DeleteAllCachedTranslations();
            else UserDatabase.DeleteCachedTranslationsOlderThan(OldTranslationsTime);
            SetUserDatabaseData(UserDatabase);
            OnPropertyChanged(null);
        }

        public void ExportCachedTranslations()
        {
            var translations = StaticMethods.Data.Translations.AsEnumerable();
            if (!ExportGame?.Equals(EntryGame.None) ?? false) translations = translations.Where(e => new EntryGame(e.GameId, e.IsUserGame, false).Equals(ExportGame));
            var fileName = $"{nameof(Happy_Reader)}_TranslationExport";
            if (!ExportGame?.Equals(EntryGame.None) ?? false)
            {
                var gameName = ExportGame.GetGameNameOnly();
                foreach (var c in Path.GetInvalidPathChars()) gameName = gameName.Replace(c.ToString(), "");
                gameName = gameName.Replace(".", "");
                fileName += $"_{gameName}";
            }
            fileName += ".sqlite";
            var dialog = new SaveFileDialog() { AddExtension = true, DefaultExt = ".sqlite", FileName = fileName };
            var result = dialog.ShowDialog();
            if (result != true) return;
            GetTranslationExportDb(translations, dialog.FileName);
        }

        private HappyReaderDatabase GetTranslationExportDb(IEnumerable<CachedTranslation> translations, string dbFile)
        {
            var export = new HappyReaderDatabase(dbFile, true);
            if (!ExportGame?.Equals(EntryGame.None) ?? false)
            {
                foreach (var translation in translations) export.Translations.UpsertLater(translation);
                var userGameIds = translations.Where(t => t.IsUserGame).Select(t => (long)t.GameId.Value).Distinct().ToList();
                foreach (var game in StaticMethods.Data.UserGames.Where(g => userGameIds.Contains(g.Id))) export.UserGames.UpsertLater(game);
                export.SaveChanges();
            }
            else
            {
                foreach (var translation in StaticMethods.Data.Translations) export.Translations.UpsertLater(translation);
                foreach (var game in StaticMethods.Data.UserGames) export.UserGames.UpsertLater(game);
                export.SaveChanges();
            }

            return export;
        }

        public void ImportCachedTranslations()
        {
            var dialog = new OpenFileDialog() { AddExtension = true, DefaultExt = ".sqlite" };
            var result = dialog.ShowDialog();
            if (result != true) return;
            var import = new HappyReaderDatabase(dialog.FileName, true);
            var translations = import.Translations.AsEnumerable();
            //key is import user game id, value is local user game id
            var userGameMap = new Dictionary<long, long>();
            //todo continue
        }
    }
}
