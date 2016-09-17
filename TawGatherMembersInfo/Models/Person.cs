using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using HtmlAgilityPack;
using NodaTime;
using NodaTime.Extensions;

using Neitri;
using System.Runtime.Serialization;
using Neitri.WebCrawling;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TawGatherMembersInfo.Models
{

    [Serializable]
    public partial class Person : IEquatable<Person>
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public Dictionary<Unit, string> UnitToPositionNameShort { get; set; } = new Dictionary<Unit, string>();

        [Index(IsUnique = true), StringLength(200)]
        public virtual string Name { get; set; } = "unnamed";
        public virtual string RankNameShort { get; set; } = "";
        public virtual long SteamId { get; set; }
        public virtual string AvatarImageUrl { get; set; } = "";
        public virtual string Status { get; set; } = "unknown"; // active, discharged, etc..
        public virtual DateTime DateJoinedTaw { get; set; }
        public virtual DateTime LastProfileDataUpdatedDate { get; set; }
        public virtual string CountryCodeIso3166 { get; set; } = "";
        public virtual ICollection<PersonToEvent> Attended { get; set; } = new HashSet<PersonToEvent>();
        //public virtual ICollection<PersonToUnit> Units { get; set; } = new HashSet<PersonToUnit>();

        public virtual string BiographyContents { get; set; } = "";
        [NonSerialized]
        BiographyData biography;
        public BiographyData Biography => biography;

        public class BiographyData
        {
            Person person;
            public BiographyData(Person person)
            {
                this.person = person;
            }
            public string GetData(params string[] names)
            {
                foreach (var name in names)
                {
                    var data = GetData(name);
                    if (data.IsNullOrEmpty() == false) return data;
                }
                return null;
            }
            public string GetData(string name)
            {
                if (person.BiographyContents.IsNullOrEmpty()) return null;

                name = name.Trim();
                while (name.EndsWith(":")) name = name.RemoveFromEnd(1).Trim();

                var biography = person.BiographyContents.ToLower();

                var start = name;
                var startIndex = biography.IndexOf(start);
                if (startIndex != -1)
                {
                    var endIndex = biography.IndexOf("\r\n", startIndex);
                    if (endIndex == -1) endIndex = biography.IndexOf("\n\r", startIndex);
                    if (endIndex == -1) endIndex = biography.IndexOf("\n", startIndex);

                    var length = biography.Length - (startIndex + start.Length);
                    if (endIndex != -1) length = endIndex - (startIndex + start.Length);

                    var data = biography.Substring(startIndex + start.Length, length).Trim();

                    while (data.StartsWith(":") || data.StartsWith("=")) data = data.RemoveFromBegin(1).Trim();

                    return data;
                }

                return null;
            }
        }

        public string CountryName
        {
            get
            {
                return countryCodeIso3166ToCountryName.GetValue(CountryCodeIso3166, "");
            }
            set
            {
                CountryCodeIso3166 = countryCodeIso3166ToCountryName.Reverse.GetValue(value, "");
            }
        }
        public string CountryFlagImageUrl
        {
            get
            {
                return GetCountryFlagImageUrl(CountryCodeIso3166);
            }
        }
        public int DaysInTaw
        {
            get
            {
                return Period.Between(DateJoinedTaw.ToLocalDateTime(), DateTime.UtcNow.ToLocalDateTime(), PeriodUnits.Days).Days;
            }
        }
        public string RankNameLong
        {
            get
            {
                return rankNameShortToRankNameLong.GetValue(RankNameShort, "Unknown rank");
            }
        }
        public string RankImageSmallUrl
        {
            get
            {
                return rankNameShortToRankImageSmall.GetValue(RankNameShort, "http://i.imgur.com/jcHvcul.png"); // default is recruit image
            }
        }
        public string RankImageBigUrl
        {
            get
            {
                return GetRankImageBigFromRankNameShort(RankNameShort);
            }
        }



        [NonSerialized]
        Unit mostImportantIngameUnit_cache;
        public Unit MostImportantIngameUnit
        {
            get
            {
                if (mostImportantIngameUnit_cache == null)
                {
                    var unitsSortedAccordingToInGameImportance = UnitToPositionNameShort
                        .OrderByDescending(unitToPositionNameShort =>
                        {
                            int priority = 0;

                            var squatTypeImportance = inGameUnitNamePriority.IndexOf(unitToPositionNameShort.Key.type.ToLower());
                            priority += squatTypeImportance;

                            var positionNameShort = unitToPositionNameShort.Value;
                            var positionImportance = positionNameShortIngamePriority.IndexOf(positionNameShort);
                            priority += 10 * positionImportance;

                            return priority;
                        });

                    mostImportantIngameUnit_cache = unitsSortedAccordingToInGameImportance.FirstOrDefault().Key;
                }
                return mostImportantIngameUnit_cache;
            }
        }

        public string MostImportantIngameUnitPositionNameShort
        {
            get
            {
                if (UnitToPositionNameShort == null) return string.Empty;
                return UnitToPositionNameShort.GetValue(MostImportantIngameUnit, string.Empty);
            }
        }

        public string MostImportantIngameUnitPositionNameLong
        {
            get
            {
                if (UnitToPositionNameShort == null) return string.Empty;
                return positionNameShortToPositionNameLong.GetValue(MostImportantIngameUnitPositionNameShort, string.Empty);
            }
        }

        [NonSerialized]
        Unit teamSpeakUnit_cache;
        /// <summary>
        /// Unit in which you hold position that you put next to your name in teamSpeak
        /// </summary>
        public Unit TeamSpeakUnit
        {
            get
            {
                if (teamSpeakUnit_cache == null)
                {
                    Unit highestPositionUnit = null;
                    int highestPositionPriority = int.MinValue;

                    foreach (var kvp in UnitToPositionNameShort)
                    {
                        var positionNameShort = kvp.Value;
                        var unit = kvp.Key;

                        var positionPriority = positionNameShortTeamSpeakNamePriorityOrder.IndexOf(positionNameShort);
                        if (positionPriority > highestPositionPriority)
                        {
                            highestPositionPriority = positionPriority;
                            highestPositionUnit = unit;
                        }
                    }
                    teamSpeakUnit_cache = highestPositionUnit;
                }
                return teamSpeakUnit_cache;
            }
        }

        /// <summary>
        /// Short name (abbrevation) of position of unit in which you hold position that you put next to your name in teamSpeak
        /// </summary>
        public string TeamSpeakUnitPositionNameShort
        {
            get
            {
                if (TeamSpeakUnit == null) return "";
                var ret = UnitToPositionNameShort.GetValue(TeamSpeakUnit, "");
                if (ret.IsNullOrEmpty()) ret = ""; //TODO: bug UnitToPositionNameShort should not contain null values
                return ret;
            }
        }

        /// <summary>
        /// Long name of position of unit in which you hold position that you put next to your name in teamSpeak
        /// </summary>
        public string TeamSpeakUnitPositionNameLong
        {
            get
            {
                return positionNameShortToPositionNameLong.GetValue(TeamSpeakUnitPositionNameShort, "");
            }
        }

        public bool IsTeamSpeakNameGuaranteedToBeCorrect
        {
            get
            {
                return UnitToPositionNameShort.Keys.Any(u =>
                {
                    var unit = u;
                    var type = unit.type.ToLower();

                    // walk the unit parent chain until we hit battalion or division             
                    while (type != "battalion" && type != "division" && unit.parentUnit != null)
                    {
                        unit = unit.parentUnit;
                        type = unit.type.ToLower();
                    }

                    var name = unit.name.ToLower();
                    if (type == "division") return name.Contains("arma ");
                    if (type == "battalion") return name.Contains("am1") || name.Contains("am2");
                    return false;
                });
            }
        }

        [NonSerialized]
        string teamSpeakName_cache;
        // during one day: this took me 4 hours, trying to find logic/algorithm in something that was made to look good, TODO: needs improving
        // spend many more hours on it afterwards as well
        public string TeamSpeakName
        {
            get
            {
                if (teamSpeakName_cache == null)
                {
                    string battalionPrefix = "";
                    var positionNameShort = this.TeamSpeakUnitPositionNameShort;

                    // find battalion name short
                    {
                        foreach (var currentUnit in UnitToPositionNameShort.Keys)
                        {
                            string newBattalionPrefix = "";

                            // walk the unit parent chain until we hit battalion or division             
                            var unit = currentUnit;
                            var type = unit.type.ToLower();
                            while (type != "battalion" && type != "division" && unit.parentUnit != null)
                            {
                                unit = unit.parentUnit;
                                type = unit.type.ToLower();
                            }
                            var name = unit.name.ToLower();


                            var doesNotHaveBattalionIndex = positionNameShortOwnedByDivision.Contains(positionNameShort);

                            // dont want to show purely support units, those are made purely for organization purposes ?
                            // only if its the only battalion person is in
                            if (name.Contains("support") == false || UnitToPositionNameShort.Count == 1)
                            {
                                if (type == "battalion" && doesNotHaveBattalionIndex) unit = unit.parentUnit;

                                var prefix = unit.TeamSpeakNamePrefix;
                                if (prefix.IsNullOrEmpty()) prefix = unit.parentUnit?.TeamSpeakNamePrefix; // if battalion has not valid prefix, try take one from division
                                if (prefix.IsNullOrEmpty() == false) newBattalionPrefix = prefix;
                            }

                            // take the longest prefix we found
                            if (newBattalionPrefix.Length > battalionPrefix.Length) battalionPrefix = newBattalionPrefix;
                        }
                    }

                    battalionPrefix = battalionPrefix.Trim();
                    positionNameShort = positionNameShort.Trim();
                    if (positionNameShort.Length > 0)
                    {
                        if (battalionPrefix.IsNullOrEmpty())
                        {
                            teamSpeakName_cache = Name + " [" + positionNameShort + "]";
                        }
                        else
                        {
                            // separating space is already in battalion prefix, no need to have additiopnal space before position
                            if (battalionPrefix.Contains(" ") == false) battalionPrefix += " ";
                            teamSpeakName_cache = Name + " [" + battalionPrefix + positionNameShort + "]";
                        }
                    }
                    else
                    {
                        // we have no position, show only battalion
                        teamSpeakName_cache = Name + " [" + battalionPrefix + "]";
                    }
                }


                return teamSpeakName_cache;
            }
        }

        public Person()
        {
            Init();
        }

        [OnDeserializing]
        void Init(StreamingContext c)
        {
            Init();
        }

        void Init()
        {
            biography = new BiographyData(this);
        }

        public static string GetPersonProfilePageUrl(string personName)
        {
            return @"http://taw.net/member/" + personName + @".aspx";
        }

        public void ClearCache()
        {
            mostImportantIngameUnit_cache = null;
            teamSpeakName_cache = null;
            teamSpeakUnit_cache = null;
        }


        public void UpdateInfoFromProfilePage(LoggedInSession roaster)
        {
            Log.Trace("updating profile for " + this.Name + " start");

            string responseText = null;

            var person = this;

            var url = GetPersonProfilePageUrl(person.Name);

            do
            {
                if (responseText != null) roaster.Login();

                var request = MyHttpWebRequest.Create(url);
                request.CookieContainer = roaster.cookieContainer;
                request.Method = "GET";

                var response = request.GetResponse();
                responseText = response.ResponseText;

            } while (roaster.IsLoggedIn(responseText) == false);

            var html = responseText.ToHtmlDocument();

            // steam profile id
            var steamProfileLinkPrefix = "http://steamcommunity.com/profiles/";
            var steamProfileLinkElement = html.GetElementbyId("hfSteam");
            if (steamProfileLinkElement != null)
            {
                var steamProfileLink = steamProfileLinkElement.GetAttributeValue("href", steamProfileLinkPrefix + "-1");
                var steamId = long.Parse(steamProfileLink.Substring(steamProfileLinkPrefix.Length));
                person.SteamId = steamId;
            }

            // avatar image
            var avatarElement = html.DocumentNode.SelectSingleNode("//*[@class='dossieravatar']/img");
            if (avatarElement != null)
            {
                var avatarImageLink = avatarElement.GetAttributeValue("src", null);
                if (avatarImageLink != null)
                {
                    person.AvatarImageUrl = "http://taw.net" + avatarImageLink;
                }
            }

            // bio
            var biographyElement = html.DocumentNode.SelectSingleNode("//*[@id='dossierbio']");
            if (biographyElement != null)
            {
                var biography = biographyElement.InnerText.Trim();
                var bioTextHeader = "Bio:";
                if (biography.StartsWith(bioTextHeader)) biography = biography.Substring(bioTextHeader.Length);
                person.BiographyContents = biography;
            }

            var table = new HtmlTwoColsStringTable(html.DocumentNode.SelectNodes("//*[@class='dossiernexttopicture']/table//tr"));

            // country
            person.CountryName = table.GetValue("Location:", person.CountryName);
            person.Status = table.GetValue("Status:", person.Status).ToLower();
            {
                var joined = table.GetValue("Joined:", "01-01-0001"); // 10-03-2014  month-day-year // wtf.. americans...
                var joinedParts = joined.Split('-');
                person.DateJoinedTaw = new DateTime(
                    int.Parse(joinedParts[2]),
                    int.Parse(joinedParts[0]),
                    int.Parse(joinedParts[1])
                );
            }

            person.LastProfileDataUpdatedDate = DateTime.UtcNow;
            person.ClearCache();

            Log.Trace("updating profile for " + this.Name + " end");
        }

        public override string ToString()
        {
            return Name + " rank:" + RankNameShort + " steamId:" + SteamId;
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as Person);
        }
        public bool Equals(Person other)
        {
            if (other == null) return false;
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

}
