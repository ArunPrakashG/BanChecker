﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BanChecker
{
	public class BanModel
	{
		public Player[] players { get; set; }
	}

	public class Player
	{
		public string SteamId { get; set; }
		public bool CommunityBanned { get; set; }
		public bool VACBanned { get; set; }
		public int NumberOfVACBans { get; set; }
		public int DaysSinceLastBan { get; set; }
		public int NumberOfGameBans { get; set; }
		public string EconomyBan { get; set; }
	}

}
