using System.Collections.Generic;

namespace TawGatherMembersInfo.Models
{
	public partial class Person
	{
		/// <summary>
		/// If unit name contains this string it has higher priority.
		/// Higher index means higher priority.
		/// </summary>
		public readonly static List<string> inGameUnitNamePriority = new List<string>() { "division", "battalion", "squad" };

		#region Position name short rank name long, priority order of rank name short

		/// <summary>
		/// Smaller index is smaller priority
		/// </summary>
		public readonly static List<string> positionNameShortTeamSpeakNamePriorityOrder = new List<string>()
			{
				"FL",
				"ST",
				"DI",
				"TI",
				"SL",
				"PL",
				"TS",
				"SA",
				"FS",
				"SO",
				"XO",
				"CO",
				"PIO",
				"DO",
				"SUL",
				"DC",
			};

		/// <summary>
		/// Smaller index is smaller priority
		/// </summary>
		public readonly static List<string> positionNameShortIngamePriority = new List<string>()
			{
				"FL",
				"SL",
				"PL",
				"FS",
			};

		/*
[23:42:29.769][E] cannot find positionNameShortToPositionNameLong.Reverse[Operations Commander]
[23:42:29.769][E] cannot find positionNameShortToPositionNameLong.Reverse[Operations Lt. Commander]
[23:42:29.770][E] cannot find positionNameShortToPositionNameLong.Reverse[Operations Division Commander]
[23:42:29.771][E] cannot find positionNameShortToPositionNameLong.Reverse[Operations Division Commander]
[23:42:29.771][E] cannot find positionNameShortToPositionNameLong.Reverse[Operations Member]
[23:42:29.774][E] cannot find positionNameShortToPositionNameLong.Reverse[Operations Corps. Commander]
[23:42:29.775][E] cannot find positionNameShortToPositionNameLong.Reverse[Operations Corps. Lt. Commander]
[23:42:29.781][E] cannot find positionNameShortToPositionNameLong.Reverse[Public Information Administrator]
[23:42:29.784][E] cannot find positionNameShortToPositionNameLong.Reverse[Operations Member]
		*/

		public readonly static BiDictionary<string, string> positionNameShortToPositionNameLong = new BiDictionary<string, string>
			{
                // Support staff doesnt use these, they just have one and the same behind their names

				// new 2016-09-09
				{"PIA", "Public Information Administrator" },
				{"HDI", "Head Drill Instructor" },

				// old, not seen in AM
				{"CIC", "Commander-in-Chief"},
				{"AC", "Army Commander"},
				{"ALC", "Army Lt. Commander"},
				{"CC", "Corps. Commander"},
				{"CLC", "Corps. Lt. Commander"},
				{"VC", "Vanguard Commander"},
				{"VLC", "Vanguard Lt. Commander"},
				{"SC", "Support Commander"},
				{"SLC", "Support Lt. Commander"},
				{"SDC", "Support Division Commander"},
				{"SOM", "Support Operations Member"},
				{"SDO", "Support Division Officer"},
				{"SCC", "Support Corps. Commander"},
				{"SCLC", "Support Corps. Lt. Commander"},
				{"ISC", "Information Security Commander"},
				{"ISLC", "Information Security Lt. Commander"},
				{"TSC", "Treasury Support Commander"},
				{"TSLC", "Treasury Support Lt. Commander"},
				{"TAW", "Board Member"},
				{"SUL", "Spin-Up Leader"},

				// seen in AM
				{"DC", "Division Commander"},
				{"DO", "Division Officer"},
				{"PIO", "Public Information Officer"},
				{"CO", "Commanding Officer"},
				{"XO", "Executive Officer"},
				{"SO", "Staff Officer"},
				{"FS", "Field Specialist"},
				{"SA", "Server Administrator"},
				{"TS", "Training Specialist"},
				{"PL", "Platoon Leader"},
				{"SL", "Squad Leader"},
				{"TI", "Training Instructor"},
				{"DI", "Drill Instructor"},
				{"ST", "Server Technician"},
				{"FL", "Fire Team Leader"},
			};

		/// <summary>
		/// Positions that do not have division number behind them in teamSpeakName
		/// </summary>
		public readonly static HashSet<string> positionNameShortOwnedByDivision = new HashSet<string>
			{
				"DC",
				"SUL",
				"DO",
				"PIO",
			};

		#endregion Position name short rank name long, priority order of rank name short

		public static string GetCountryFlagImageUrl(string countryCodeTwoLetter)
		{
			if (countryCodeTwoLetter.Length > 2) countryCodeTwoLetter = countryCodeTwoLetter.Substring(0, 2);
			if (countryCodeTwoLetter.Length == 2) return @"http://i1028.photobucket.com/albums/y345/judgernaut/TS_flags/" + countryCodeTwoLetter + ".png";
			return "";
		}
	}
}