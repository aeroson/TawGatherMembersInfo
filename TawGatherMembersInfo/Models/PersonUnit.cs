﻿using Neitri;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace TawGatherMembersInfo.Models
{
	public class PersonUnit
	{
		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long PersonUnitId { get; set; }

		/*
		[ForeignKey(nameof(Person))]
		public long Person_PersonId { get; set; }

		public long Unit_UnitId { get; set; }

		*/

		[StringLength(500)]
		public virtual string PositionNameShort { get; set; }

		public virtual Person JoinedBy { get; set; }

		[Index]
		public virtual DateTime Joined { get; set; }

		public virtual Person RemovedBy { get; set; }

		[Index]
		public virtual DateTime Removed { get; set; }

		public virtual Person Person { get; set; }
		public virtual Unit Unit { get; set; }

		[NotMapped]
		public string PositionNameLong => PositionNameShortToPositionNameLong(PositionNameShort);

		public static string PositionNameShortToPositionNameLong(string positionNameShort)
		{
			return positionNameShortToPositionNameLong.GetValue(positionNameShort);
		}

		public static string PositionNameLongToPositionNameShort(string positionNameLong)
		{
			return positionNameShortToPositionNameLong.Reverse.GetValue(positionNameLong);
		}

		/// <summary>
		/// TeamSpeak name position suffix
		/// </summary>
		readonly static BiDictionary<string, string> positionNameShortToPositionNameLong = new BiDictionary<string, string>
			{
                // Support staff doesnt use these, they just have one and the same behind their names

				// new 2016-09-09
				{"PIA", "Public Information Administrator"},
				{"HDI", "Head Drill Instructor"},

				// other, not seen in AM
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

				// unknown short name
				// TODO: find out correct short name
				{"OPS1", "Operations Commander"},
				{"OPS2", "Operations Lt. Commander"},
				{"OPS3", "Operations Division Commander"},
				{"OPS4", "Operations Division Member"},
				{"OPS5", "Operations Member"},
				{"OPS6", "Operations Corps. Commander"},
				{"OPS7", "Operations Corps. Lt. Commander"},
			};

	}
}