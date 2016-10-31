using Neitri;
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
			if (countryCodeTwoLetter.Contains("-")) countryCodeTwoLetter = countryCodeTwoLetter.TakeStringBefore("-");
			return @"http://i1028.photobucket.com/albums/y345/judgernaut/TS_flags/" + countryCodeTwoLetter + ".png";
		}
	}
}