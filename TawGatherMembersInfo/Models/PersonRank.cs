using Neitri;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TawGatherMembersInfo.Models
{
	public class PersonRank
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long PersonRankId { get; set; }

		[Index]
		public long TawId { get; set; }

		[MaxLength(10)]
		public virtual string NameShort { get; set; }

		[Index]
		public virtual DateTime ValidFrom { get; set; }

		public virtual Person Person { get; set; }

		public virtual Person PromotedBy { get; set; }

		[NotMapped]
		public string NameLong
		{
			get
			{
				return rankNameShortToRankNameLong.GetValue(NameShort, "Unknown rank");
			}
			set
			{
				NameShort = rankNameShortToRankNameLong.Reverse.GetValue(value, "Unknown");
			}
		}

		[NotMapped]
		public string ImageSmallUrl
		{
			get
			{
				return rankNameShortToRankImageSmall.GetValue(NameShort, "http://i.imgur.com/jcHvcul.png"); // default is recruit image
			}
		}

		[NotMapped]
		public string ImageBigUrl
		{
			get
			{
				return GetRankImageBigFromRankNameShort(NameShort);
			}
		}

		#region Ranks short name, long name, small image, big image

		/*
		public enum RankNameShort
		{
			Unknown,
			GEN5,
			GEN,
			MGN,
			BGN,
			MAJ,
			CPT,
			2LT,
			1LT,
			LCP,
			SGT,
			REC,
			GSG,
			SSG,
			1SG,
			LTC,
			CPL,
			SGM,
			PFC,
			COL,
			MSG,
			MGS,
			LGN,
		}
		*/

		/// <summary>
		/// Smaller index is smaller rank
		/// </summary>
		static List<string> rankNameShortOrder = new List<string>()
			{
				"",
				"REC",
				"PFC",
				"LCP",
				"CPL",
				"SGT",
				"SSG",
				"GSG",
				"MSG",
				"1SG",
				"MGS",
				"SGM",
				"2LT",
				"1LT",
				"CPT",
				"MAJ",
				"LTC",
				"COL",
			};

		static Dictionary<string, string> rankNameShortToRankImageSmall = new Dictionary<string, string>()
			{
				{"REC", "http://i.imgur.com/jcHvcul.png"},
				{"PFC", "http://i.imgur.com/PGymqkT.png"},
				{"LCP", "http://i.imgur.com/RHkwVzX.png"},
				{"CPL", "http://i.imgur.com/eaA3y9R.png"},
				{"SGT", "http://i.imgur.com/LhxaoN6.png"},
				{"SSG", "http://i.imgur.com/fegTyol.png"},
				{"GSG", "http://i.imgur.com/DK3XX6j.png"},
				{"MSG", "http://i.imgur.com/WieCvYJ.png"},
				{"1SG", "http://i.imgur.com/DKbJkyc.png"},
				{"MGS", "http://i.imgur.com/6Qnsfk6.png"},
				{"SGM", "http://i.imgur.com/ygNvDIP.png"},
				{"2LT", "http://i.imgur.com/CtANpCr.png"},
				{"1LT", "http://i.imgur.com/vm68HEM.png"},
				{"CPT", "http://i.imgur.com/MFhFQ83.png"},
				{"MAJ", "http://i.imgur.com/OLUbVSj.png"},
				{"LTC", "http://i.imgur.com/queFoBA.png"},
				{"COL", "http://i.imgur.com/OHZq8eU.png"},
			};

		static string GetRankImageBigFromRankNameShort(string rankNameShort)
		{
			var rankIndex = rankNameShortOrder.IndexOf(rankNameShort);
			// http://taw.net/Utility/ranks/rank_22.png is REC
			// http://taw.net/Utility/ranks/rank_21.png is PFC
			// http://taw.net/Utility/ranks/rank_20.png is LCP
			return "http://taw.net/Utility/ranks/rank_" + (22 - rankIndex) + ".png";
		}

		#endregion Ranks short name, long name, small image, big image

		public static BiDictionary<string, string> rankNameShortToRankNameLong = new BiDictionary<string, string>()
			{
				{"REC", "Recruit"},
				{"PFC", "First Class"},
				{"LCP", "Lance Corporal"},
				{"CPL", "Corporal"},
				{"SGT", "Sergeant"},
				{"SSG", "Staff Sergeant"},
				{"GSG", "Gunnery Sergeant"},
				{"MSG", "Master Sergeant"},
				{"1SG", "First Sergeant"},
				{"MGS", "Master Gunnery Sergeant"},
				{"SGM", "Sergeant Major"},
				{"2LT", "Second Lieutenant"},
				{"1LT", "First Lieutenant"},
				{"CPT", "Captain"},
				{"MAJ", "Major"},
				{"LTC", "Lieutenant Colonel"},
				{"COL", "Colonel"},
			};
	}
}