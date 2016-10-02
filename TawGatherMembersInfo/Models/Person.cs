using Neitri;
using NodaTime;
using NodaTime.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;

namespace TawGatherMembersInfo.Models
{
	public partial class Person : IEquatable<Person>
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long PersonId { get; set; }

		//public Dictionary<Unit, string> UnitToPositionNameShort { get; set; } = new Dictionary<Unit, string>();

		[Index(IsUnique = true), StringLength(500), Required]
		public virtual string Name { get; set; } = "unnamed";

		[NotMapped]
		public virtual string RankNameShort => Rank?.NameShort;

		public virtual long SteamId { get; set; }

		[StringLength(1000)]
		public virtual string AvatarImageUrl { get; set; } = "";

		[StringLength(100)]
		public virtual string Status { get; set; } = "unknown"; // active, discharged, etc..

		public virtual DateTime DateJoinedTaw { get; set; }
		public virtual DateTime LastProfileDataUpdatedDate { get; set; }

		[StringLength(10)]
		public virtual string CountryCodeIso3166 { get; set; } = ""; // https://en.wikipedia.org/wiki/ISO_3166-1_alpha-3

		[MaxLength]
		public virtual string BiographyContents { get; set; } = "";

		public virtual DateTime AppliedForTaw { get; set; }
		public virtual DateTime AdmittedToTaw { get; set; }

		public virtual ICollection<PersonRank> Ranks { get; set; } = new List<PersonRank>();

		public virtual ICollection<PersonEvent> Events { get; set; } = new List<PersonEvent>();

		public virtual ICollection<PersonUnit> Units { get; set; }

		public virtual ICollection<PersonCommendation> Commendations { get; set; }

		public virtual ICollection<PersonStatus> Statuses { get; set; }

		[NotMapped]
		public virtual PersonRank Rank => Ranks.OrderByDescending(r => r.ValidFrom).FirstOrDefault();

		[NonSerialized]
		BiographyData biography;

		[NotMapped]
		public BiographyData Biography => biography;

		[NotMapped]
		public Dictionary<Unit, string> UnitToPositionNameShort => Units.ToDictionary(i => i.Unit, i => i.PositionNameShort);

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

		[NotMapped]
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

		[NotMapped]
		public string CountryFlagImageUrl
		{
			get
			{
				return GetCountryFlagImageUrl(CountryCodeIso3166);
			}
		}

		[NotMapped]
		public int DaysInTaw
		{
			get
			{
				return Period.Between(DateJoinedTaw.ToLocalDateTime(), DateTime.UtcNow.ToLocalDateTime(), PeriodUnits.Days).Days;
			}
		}

		[NonSerialized, NotMapped]
		Unit mostImportantIngamecache;

		[NotMapped]
		public Unit MostImportantIngameUnit
		{
			get
			{
				if (mostImportantIngamecache == null)
				{
					var unitsSortedAccordingToInGameImportance = UnitToPositionNameShort
						.OrderByDescending(unitToPositionNameShort =>
						{
							int priority = 0;

							var squatTypeImportance = inGameUnitNamePriority.IndexOf(unitToPositionNameShort.Key.Type.ToLower());
							priority += squatTypeImportance;

							var positionNameShort = unitToPositionNameShort.Value;
							var positionImportance = positionNameShortIngamePriority.IndexOf(positionNameShort);
							priority += 10 * positionImportance;

							return priority;
						});

					mostImportantIngamecache = unitsSortedAccordingToInGameImportance.FirstOrDefault().Key;
				}
				return mostImportantIngamecache;
			}
		}

		[NotMapped]
		public string MostImportantIngameUnitPositionNameShort
		{
			get
			{
				if (UnitToPositionNameShort == null) return string.Empty;
				return UnitToPositionNameShort.GetValue(MostImportantIngameUnit, string.Empty);
			}
		}

		[NotMapped]
		public string MostImportantIngameUnitPositionNameLong
		{
			get
			{
				if (UnitToPositionNameShort == null) return string.Empty;
				return positionNameShortToPositionNameLong.GetValue(MostImportantIngameUnitPositionNameShort, string.Empty);
			}
		}

		[NotMapped, NonSerialized]
		Unit teamSpeakcache;

		/// <summary>
		/// Unit in which you hold position that you put next to your name in teamSpeak
		/// </summary>
		[NotMapped]
		public Unit TeamSpeakUnit
		{
			get
			{
				if (teamSpeakcache == null)
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
					teamSpeakcache = highestPositionUnit;
				}
				return teamSpeakcache;
			}
		}

		/// <summary>
		/// Short name (abbrevation) of position of unit in which you hold position that you put next to your name in teamSpeak
		/// </summary>
		[NotMapped]
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
		[NotMapped]
		public string TeamSpeakUnitPositionNameLong
		{
			get
			{
				return positionNameShortToPositionNameLong.GetValue(TeamSpeakUnitPositionNameShort, "");
			}
		}

		[NotMapped]
		public bool IsTeamSpeakNameGuaranteedToBeCorrect
		{
			get
			{
				return UnitToPositionNameShort.Keys.Any(u =>
				{
					var unit = u;
					var type = unit.Type.ToLower();

					// walk the unit parent chain until we hit battalion or division
					while (type != "battalion" && type != "division" && unit.ParentUnit != null)
					{
						unit = unit.ParentUnit;
						type = unit.Type.ToLower();
					}

					var name = unit.Name.ToLower();
					if (type == "division") return name.Contains("arma ");
					if (type == "battalion") return name.Contains("am1") || name.Contains("am2");
					return false;
				});
			}
		}

		[NotMapped, NonSerialized]
		string teamSpeakName_cache;

		// during one day: this took me 4 hours, trying to find logic/algorithm in something that was made to look good, TODO: needs improving
		// spend many more hours on it afterwards as well
		[NotMapped]
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
							var type = unit.Type.ToLower();
							while (type != "battalion" && type != "division" && unit.ParentUnit != null)
							{
								unit = unit.ParentUnit;
								type = unit.Type.ToLower();
							}
							var name = unit.Name.ToLower();

							var doesNotHaveBattalionIndex = positionNameShortOwnedByDivision.Contains(positionNameShort);

							// dont want to show purely support units, those are made purely for organization purposes ?
							// only if its the only battalion person is in
							if (name.Contains("support") == false || UnitToPositionNameShort.Count == 1)
							{
								if (type == "battalion" && doesNotHaveBattalionIndex) unit = unit.ParentUnit;

								var prefix = unit.TeamSpeakNamePrefix;
								if (prefix.IsNullOrEmpty()) prefix = unit.ParentUnit?.TeamSpeakNamePrefix; // if battalion has not valid prefix, try take one from division
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
			mostImportantIngamecache = null;
			teamSpeakName_cache = null;
			teamSpeakcache = null;
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