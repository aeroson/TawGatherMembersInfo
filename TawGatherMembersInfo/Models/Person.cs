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
	public partial class Person
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long PersonId { get; set; }

		//public Dictionary<Unit, string> UnitToPositionNameShort { get; set; } = new Dictionary<Unit, string>();

		[Index(IsUnique = true), StringLength(500), Required]
		public virtual string Name { get; set; } = "unnamed";

		public virtual long SteamId { get; set; }

		[StringLength(1000)]
		public virtual string AvatarImageUrl { get; set; } = "";

		[StringLength(100)]
		public virtual string Status { get; set; } = "unknown"; // active, discharged, etc..

		public virtual DateTime DateJoinedTaw { get; set; } // TODO: remove
															//[NotMapped] public virtual DateTime DateJoinedTaw => AdmittedToTaw

		public virtual DateTime LastProfileDataUpdatedDate { get; set; }

		[StringLength(10)]
		public virtual string CountryCodeIso3166 { get; set; } = ""; // https://en.wikipedia.org/wiki/ISO_3166-1_alpha-3

		[MaxLength]
		public virtual string BiographyContents { get; set; } = "";

		public virtual DateTime AppliedForTaw { get; set; }
		public virtual DateTime AdmittedToTaw { get; set; }

		[NoApi]
		public virtual ICollection<PersonRank> Ranks { get; set; } = new List<PersonRank>();

		[NoApi]
		public virtual ICollection<PersonEvent> Events { get; set; } = new List<PersonEvent>();

		[NoApi]
		public virtual ICollection<PersonUnit> Units { get; set; } = new List<PersonUnit>();

		[NoApi]
		public virtual ICollection<PersonCommendation> Commendations { get; set; } = new List<PersonCommendation>();

		[NoApi]
		public virtual ICollection<PersonStatus> Statuses { get; set; } = new List<PersonStatus>();

		[NotMapped, NoApi]
		public IEnumerable<PersonUnit> ActiveUnits
		{
			get
			{
				var utcNow = DateTime.UtcNow;
				return Units.Where(u => u.Removed > utcNow);
			}
		}

		[NotMapped]
		public virtual PersonRank Rank => Ranks.OrderByDescending(r => r.ValidFrom).FirstOrDefault();

		[NonSerialized]
		BiographyData biography;

		[NotMapped, NoApi]
		public BiographyData Biography => biography;

		public class BiographyData
		{
			Person person;

			public BiographyData(Person person)
			{
				this.person = person;
			}

			public override string ToString()
			{
				return person.BiographyContents;
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
				var name = Iso3166.countryNameToIso3166.Reverse.GetValue(CountryCodeIso3166, string.Empty);
				if (name.IsNullOrWhiteSpace()) name = Iso3166.countrySubdivisonNameToIso3166_2.Reverse.GetValue(CountryCodeIso3166, string.Empty);
				return name;
			}
			set
			{
				var iso = Iso3166.countryNameToIso3166.GetValue(value, string.Empty);
				if (iso.IsNullOrWhiteSpace()) iso = Iso3166.countrySubdivisonNameToIso3166_2.GetValue(value, string.Empty);
				CountryCodeIso3166 = iso;
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

		[NotMapped]
		public PersonUnit MostImportantIngameUnit
		{
			get
			{
				var unitsSortedAccordingToInGameImportance = ActiveUnits
					.OrderByDescending(u =>
					{
						int priority = 0;

						var squatTypeImportance = inGameUnitNamePriority.IndexOf(u.Unit.Type.ToLower());
						priority += squatTypeImportance;

						var positionNameShort = u.PositionNameShort;
						var positionImportance = positionNameShortIngamePriority.IndexOf(positionNameShort);
						priority += 10 * positionImportance;

						return priority;
					});

				return unitsSortedAccordingToInGameImportance.FirstOrDefault();
			}
		}

		/// <summary>
		/// Unit in which you hold position that you put next to your name in teamSpeak
		/// </summary>
		[NotMapped]
		public PersonUnit TeamSpeakUnit
		{
			get
			{
				return TeamSpeakPrioritizedUnits.FirstOrDefault();
			}
		}

		/// <summary>
		/// Units sorted by position in unit TeamSpeak priority.
		/// Highest priority is first.
		/// </summary>
		[NotMapped, NoApi]
		public IEnumerable<PersonUnit> TeamSpeakPrioritizedUnits
		{
			get
			{
				return ActiveUnits.OrderBy(u => positionNameShortTeamSpeakNamePriorityOrder.IndexOf(u.PositionNameShort));
			}
		}

		// during one day: this took me 4 hours, trying to find logic/algorithm in something that was made to look good, TODO: needs improving
		// spend many more hours on it afterwards as well
		[NotMapped]
		public string TeamSpeakName
		{
			get
			{
				var teamSpeakName = "";

				string battalionPrefix = "";
				var positionNameShort = this.TeamSpeakUnit?.PositionNameShort;

				// find battalion name short
				{
					foreach (var currentUnit in TeamSpeakPrioritizedUnits)
					{
						string newBattalionPrefix = "";

						// walk the unit parent chain until we hit battalion or division
						var unit = currentUnit.Unit;
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
						if (name.Contains("support") == false || ActiveUnits.Count() == 1)
						{
							if (type == "battalion" && doesNotHaveBattalionIndex) unit = unit.ParentUnit;

							var prefix = unit.TeamSpeakNamePrefix;
							if (prefix.IsNullOrWhiteSpace()) prefix = unit.ParentUnit?.TeamSpeakNamePrefix; // if battalion has not valid prefix, try take one from division
							if (prefix.IsNullOrWhiteSpace() == false) newBattalionPrefix = prefix;
						}

						// take the longest prefix we found
						if (newBattalionPrefix.Length > battalionPrefix.Length) battalionPrefix = newBattalionPrefix;
					}
				}

				battalionPrefix = battalionPrefix?.Trim();
				positionNameShort = positionNameShort?.Trim();
				if (positionNameShort.IsNullOrWhiteSpace())
				{
					// we have no position, show only battalion
					teamSpeakName = Name + " [" + battalionPrefix + "]";
				}
				else
				{
					if (battalionPrefix.IsNullOrWhiteSpace())
					{
						teamSpeakName = Name + " [" + positionNameShort + "]";
					}
					else
					{
						// separating space is already in battalion prefix, no need to have additiopnal space before position
						if (battalionPrefix.Contains(" ") == false) battalionPrefix += " ";
						teamSpeakName = Name + " [" + battalionPrefix + positionNameShort + "]";
					}
				}
				return teamSpeakName;
			}
		}

		public Person()
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

		public override string ToString()
		{
			return Name + " rank:" + Rank?.NameShort + " steamId:" + SteamId;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}
}