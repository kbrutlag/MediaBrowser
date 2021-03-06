﻿using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Resolvers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class TVUtils
    /// </summary>
    public static class TVUtils
    {
        /// <summary>
        /// The TVDB API key
        /// </summary>
        public static readonly string TvdbApiKey = "B89CE93890E9419B";
        /// <summary>
        /// The banner URL
        /// </summary>
        public static readonly string BannerUrl = "http://www.thetvdb.com/banners/";

        /// <summary>
        /// A season folder must contain one of these somewhere in the name
        /// </summary>
        private static readonly string[] SeasonFolderNames = new[]
                                                                 {
                                                                     "season",
                                                                     "sæson",
                                                                     "temporada",
                                                                     "saison",
                                                                     "staffel"
                                                                 };

        /// <summary>
        /// Used to detect paths that represent episodes, need to make sure they don't also
        /// match movie titles like "2001 A Space..."
        /// Currently we limit the numbers here to 2 digits to try and avoid this
        /// </summary>
        private static readonly Regex[] EpisodeExpressions = new[]
                                                                 {
                                                                     new Regex(
                                                                         @".*\\[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\[sS](?<seasonnumber>\d{1,4})[x,X]?[eE](?<epnumber>\d{1,3})[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>[^\\]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d{1,3})[^\\]*$",
                                                                         RegexOptions.Compiled)
                                                                 };
        private static readonly Regex[] MultipleEpisodeExpressions = new[]
                                                                 {
                                                                     new Regex(
                                                                         @".*\\[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})((-| - )\d{1,4}[eExX](?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})((-| - )\d{1,4}[xX][eE](?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})((-| - )?[xXeE](?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\[sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3})(-[xE]?[eE]?(?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))((-| - )\d{1,4}[xXeE](?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))((-| - )\d{1,4}[xX][eE](?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))((-| - )?[xXeE](?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>((?![sS]?\d{1,4}[xX]\d{1,3})[^\\])*)?([sS]?(?<seasonnumber>\d{1,4})[xX](?<epnumber>\d{1,3}))(-[xX]?[eE]?(?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>[^\\]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d{1,3})((-| - )?[xXeE](?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled),
                                                                     new Regex(
                                                                         @".*\\(?<seriesname>[^\\]*)[sS](?<seasonnumber>\d{1,4})[xX\.]?[eE](?<epnumber>\d{1,3})(-[xX]?[eE]?(?<endingepnumber>\d{1,3}))+[^\\]*$",
                                                                         RegexOptions.Compiled)
                                                                 };

        /// <summary>
        /// To avoid the following matching movies they are only valid when contained in a folder which has been matched as a being season
        /// </summary>
        private static readonly Regex[] EpisodeExpressionsInASeasonFolder = new[]
                                                                                {
                                                                                    new Regex(
                                                                                        @".*\\(?<epnumber>\d{1,2})\s?-\s?[^\\]*$",
                                                                                        RegexOptions.Compiled),
                                                                                    // 01 - blah.avi, 01-blah.avi
                                                                                    new Regex(
                                                                                        @".*\\(?<epnumber>\d{1,2})[^\d\\]*[^\\]*$",
                                                                                        RegexOptions.Compiled),
                                                                                    // 01.avi, 01.blah.avi "01 - 22 blah.avi" 
                                                                                    new Regex(
                                                                                        @".*\\(?<seasonnumber>\d)(?<epnumber>\d{1,2})[^\d\\]+[^\\]*$",
                                                                                        RegexOptions.Compiled),
                                                                                    // 01.avi, 01.blah.avi
                                                                                    new Regex(
                                                                                        @".*\\\D*\d+(?<epnumber>\d{2})",
                                                                                        RegexOptions.Compiled)
                                                                                    // hell0 - 101 -  hello.avi

                                                                                };

        /// <summary>
        /// Gets the season number from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        public static int? GetSeasonNumberFromPath(string path)
        {
            var filename = Path.GetFileName(path);

            // Look for one of the season folder names
            foreach (var name in SeasonFolderNames)
            {
                var index = filename.IndexOf(name, StringComparison.OrdinalIgnoreCase);

                if (index != -1)
                {
                    return GetSeasonNumberFromPathSubstring(filename.Substring(index + name.Length));
                }
            }

            return null;
        }

        /// <summary>
        /// Extracts the season number from the second half of the Season folder name (everything after "Season", or "Staffel")
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.Nullable{System.Int32}.</returns>
        private static int? GetSeasonNumberFromPathSubstring(string path)
        {
            int numericStart = -1;
            int length = 0;

            // Find out where the numbers start, and then keep going until they end
            for (var i = 0; i < path.Length; i++)
            {
                if (char.IsNumber(path, i))
                {
                    if (numericStart == -1)
                    {
                        numericStart = i;
                    }
                    length++;
                }
                else if (numericStart != -1)
                {
                    break;
                }
            }

            if (numericStart == -1)
            {
                return null;
            }

            return int.Parse(path.Substring(numericStart, length));
        }

        /// <summary>
        /// Determines whether [is season folder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is season folder] [the specified path]; otherwise, <c>false</c>.</returns>
        private static bool IsSeasonFolder(string path)
        {
            // It's a season folder if it's named as such and does not contain any audio files, apart from theme.mp3
            return GetSeasonNumberFromPath(path) != null && !Directory.EnumerateFiles(path).Any(i => EntityResolutionHelper.IsAudioFile(i) && !string.Equals(Path.GetFileNameWithoutExtension(i), BaseItem.ThemeSongFilename));
        }

        /// <summary>
        /// Determines whether [is series folder] [the specified path].
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="fileSystemChildren">The file system children.</param>
        /// <returns><c>true</c> if [is series folder] [the specified path]; otherwise, <c>false</c>.</returns>
        public static bool IsSeriesFolder(string path, IEnumerable<FileSystemInfo> fileSystemChildren)
        {
            // A folder with more than 3 non-season folders in will not becounted as a series
            var nonSeriesFolders = 0;

            foreach (var child in fileSystemChildren)
            {
                var attributes = child.Attributes;

                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }

                if ((attributes & FileAttributes.System) == FileAttributes.System)
                {
                    continue;
                }

                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    if (IsSeasonFolder(child.FullName))
                    {
                        return true;
                    }

                    nonSeriesFolders++;

                    if (nonSeriesFolders >= 3)
                    {
                        return false;
                    }
                }
                else
                {
                    var fullName = child.FullName;

                    if (EntityResolutionHelper.IsVideoFile(fullName) && GetEpisodeNumberFromFile(fullName, false).HasValue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Episodes the number from file.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <param name="isInSeason">if set to <c>true</c> [is in season].</param>
        /// <returns>System.String.</returns>
        public static int? GetEpisodeNumberFromFile(string fullPath, bool isInSeason)
        {
            string fl = fullPath.ToLower();
            foreach (var r in EpisodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                    return ParseEpisodeNumber(m.Groups["epnumber"].Value);
            }
            if (isInSeason)
            {
                var match = EpisodeExpressionsInASeasonFolder.Select(r => r.Match(fl))
                    .FirstOrDefault(m => m.Success);

                if (match != null)
                {
                    return ParseEpisodeNumber(match.Value);
                }
            }

            return null;
        }

        public static int? GetEndingEpisodeNumberFromFile(string fullPath)
        {
            var fl = fullPath.ToLower();
            foreach (var r in MultipleEpisodeExpressions)
            {
                var m = r.Match(fl);
                if (m.Success && !string.IsNullOrEmpty(m.Groups["endingepnumber"].Value))
                    return ParseEpisodeNumber(m.Groups["endingepnumber"].Value);
            }
            return null;
        }

        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        private static int? ParseEpisodeNumber(string val)
        {
            int num;

            if (!string.IsNullOrEmpty(val) && int.TryParse(val, NumberStyles.Integer, UsCulture, out num))
            {
                return num;
            }

            return null;
        }

        /// <summary>
        /// Seasons the number from episode file.
        /// </summary>
        /// <param name="fullPath">The full path.</param>
        /// <returns>System.String.</returns>
        public static int? GetSeasonNumberFromEpisodeFile(string fullPath)
        {
            string fl = fullPath.ToLower();
            foreach (var r in EpisodeExpressions)
            {
                Match m = r.Match(fl);
                if (m.Success)
                {
                    Group g = m.Groups["seasonnumber"];
                    if (g != null)
                    {
                        var val = g.Value;

                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            int num;

                            if (int.TryParse(val, NumberStyles.Integer, UsCulture, out num))
                            {
                                return num;
                            }
                        }
                    }
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the air days.
        /// </summary>
        /// <param name="day">The day.</param>
        /// <returns>List{DayOfWeek}.</returns>
        public static List<DayOfWeek> GetAirDays(string day)
        {
            if (!string.IsNullOrWhiteSpace(day))
            {
                if (day.Equals("Daily", StringComparison.OrdinalIgnoreCase))
                {
                    return new List<DayOfWeek>
                               {
                                   DayOfWeek.Sunday,
                                   DayOfWeek.Monday,
                                   DayOfWeek.Tuesday,
                                   DayOfWeek.Wednesday,
                                   DayOfWeek.Thursday,
                                   DayOfWeek.Friday,
                                   DayOfWeek.Saturday
                               };
                }

                DayOfWeek value;

                if (Enum.TryParse(day, true, out value))
                {
                    return new List<DayOfWeek>
                               {
                                   value
                               };
                }

                return new List<DayOfWeek>();
            }
            return null;
        }
    }
}
