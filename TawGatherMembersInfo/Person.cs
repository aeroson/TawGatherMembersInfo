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

namespace TawGatherMembersInfo
{

    [Serializable]
    public partial class Person : IEquatable<Person>
    {
        public Dictionary<Unit, string> unitToPositionNameShort = new Dictionary<Unit, string>();
        public string name = "unnamed";
        public string rankNameShort = "";
        public long steamId;
        public string avatarImageUrl = "";
        public string status = "unknown"; // active, discharged, etc..
        public int id = 0;
        public DateTime dateJoinedTaw;
		public string biography;

        public string countryCodeIso3166 = "";
        public string CountryName
        {
            get
            {
                return countryCodeIso3166ToCountryName.Get(countryCodeIso3166, "");
            }
            set
            {
                countryCodeIso3166 = countryCodeIso3166ToCountryName.Reverse.Get(value, "");
            }
        }
        public string CountryFlagImageUrl
        {
            get
            {
                return GetCountryFlagImageUrl(countryCodeIso3166);
            }
        }
        public int DaysInTaw
        {
            get
            {
                return Period.Between(dateJoinedTaw.ToLocalDateTime(), DateTime.UtcNow.ToLocalDateTime(), PeriodUnits.Days).Days;
            }
        }
        public string RankNameLong
        {
            get
            {
                return rankNameShortToRankNameLong.Get(rankNameShort, "Unknown rank");
            }
        }
        public string RankImageSmallUrl
        {
            get
            {
                return rankNameShortToRankImageSmall.Get(rankNameShort, "http://i.imgur.com/jcHvcul.png"); // default is recruit image
            }
        }
        public string RankImageBigUrl
        {
            get
            {
                return GetRankImageBigFromRankNameShort(rankNameShort);
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
                    var unitsSortedAccordingToInGameImportance = unitToPositionNameShort
                        .OrderByDescending(unitToPositionNameShort => {
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
                if (unitToPositionNameShort == null) return string.Empty;
                return unitToPositionNameShort.Get(MostImportantIngameUnit, string.Empty);
            }
        }

        public string MostImportantIngameUnitPositionNameLong
        {
            get
            {
                if (unitToPositionNameShort == null) return string.Empty;
                return positionNameShortToPositionNameLong.Get(MostImportantIngameUnitPositionNameShort, string.Empty);
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

                    foreach (var kvp in unitToPositionNameShort)
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
                return unitToPositionNameShort.Get(TeamSpeakUnit, "");
            }
        }

        /// <summary>
        /// Long name of position of unit in which you hold position that you put next to your name in teamSpeak
        /// </summary>
        public string TeamSpeakUnitPositionNameLong
        {
            get
            {
                return positionNameShortToPositionNameLong.Get(TeamSpeakUnitPositionNameShort, "");
            }
        }

        [NonSerialized]
        string teamSpeakName_cache;
        // this took me 4 hours, trying to find logic/algorithm in something that was made to look good, TODO: needs improving
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
                        foreach (var currentUnit in unitToPositionNameShort.Keys)
                        {
                            string newBattalionPrefix = "";

                            var unit = currentUnit;

                            // walk the unit parent chain until we hit batallion or division                              
                            while (unit.type != "Battalion" && unit.type != "Division" && unit.parentUnit != null) unit = unit.parentUnit;

                            var doesNotHaveBattalionIndex = positionNameShortOwnedByDivision.Contains(positionNameShort);

                            if (unit.type == "Division")
                            {
                                if (unit.name == "Arma III") newBattalionPrefix = "AM ";
                            }
                            if (unit.type == "Battalion" && doesNotHaveBattalionIndex == false)
                            {
                                var nameParts = unit.name.Split(' '); // AM1 1st Battalion North American || AM2 2nd Battalion European
                                newBattalionPrefix = nameParts[0];

                                int lastCharAsInt;
                                var isLastCharNumber = int.TryParse(newBattalionPrefix.Last().ToString(), out lastCharAsInt);
                                if (isLastCharNumber)
                                {
                                    newBattalionPrefix = newBattalionPrefix.Substring(0, newBattalionPrefix.Length - 1) + " " + lastCharAsInt;
                                }
                            }

                            if (newBattalionPrefix.Length > battalionPrefix.Length) battalionPrefix = newBattalionPrefix;
                        }
                    }

                    teamSpeakName_cache = name + " [" + (battalionPrefix + positionNameShort).Trim() + "]";
                }


                return teamSpeakName_cache;
            }
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


        public void UpdateInfoFromProfilePage(RoasterFactory roaster)
        {
            string responseText = null;

            var person = this;

            var url = GetPersonProfilePageUrl(person.name);

            do
            {
                if (responseText != null) roaster.Login();

                var request = MyHttpWebRequest.Create(url);
                request.CookieContainer = roaster.cookieContainer;
                request.Method = "GET";

                var response = request.GetResponse();
                responseText = response.GetResponseStream().StreamReadTextToEnd();

            } while (roaster.IsLoggedIn(responseText) == false);

            var html = responseText.HtmlStringToDocument();

            // steam profile id
            var steamProfileLinkPrefix = "http://steamcommunity.com/profiles/";
            var steamProfileLinkElement = html.GetElementbyId("hfSteam");
            if (steamProfileLinkElement != null)
            {
                var steamProfileLink = steamProfileLinkElement.GetAttributeValue("href", steamProfileLinkPrefix + "-1");
                var steamId = long.Parse(steamProfileLink.Substring(steamProfileLinkPrefix.Length));
                person.steamId = steamId;
            }

            // avatar image
            var avatarElement = html.DocumentNode.SelectSingleNode("//*[@class='dossieravatar']/img");
            if (avatarElement != null)
            {
                var avatarImageLink = avatarElement.GetAttributeValue("src", null);
                if (avatarImageLink != null)
                {
                    person.avatarImageUrl = "http://taw.net" + avatarImageLink;
                }
            }

			// bio
			var biographyElement = html.DocumentNode.SelectSingleNode("//*[@id='dossierbio']");
			if (biographyElement != null) {
				var biography = biographyElement.InnerText.Trim();
				var bioTextHeader = "Bio:";
				if (biography.StartsWith(bioTextHeader)) biography = biography.Substring(bioTextHeader.Length);
				person.biography = biography;
			}

			var table = new HtmlTwoColsStringTable(html.DocumentNode.SelectNodes("//*[@class='dossiernexttopicture']/table//tr"));

            // country
            person.CountryName = table.Get("Location:", person.CountryName);
            person.status = table.Get("Status:", person.status).ToLower();
            {
                var joined = table.Get("Joined:", "01-01-0001"); // 10-03-2014  month-day-year // wtf.. americans...
                var joinedParts = joined.Split('-');
                person.dateJoinedTaw = new DateTime(
                    int.Parse(joinedParts[2]),
                    int.Parse(joinedParts[0]),
                    int.Parse(joinedParts[1])
                );
            }

            person.ClearCache();
        }

        public override string ToString()
        {
            return name + " rank:" + rankNameShort + " steamId:" + steamId;
        }

        public bool Equals(Person other)
        {
            if (other == null) return false;
            return name == other.name;
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }
    }

}
