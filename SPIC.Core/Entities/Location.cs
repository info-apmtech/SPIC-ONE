using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.Entities
{
	public class Zone
	{
		public int Id { get; set; }
		public string ZoneName { get; set; } = string.Empty;
		public string ZoneCode { get; set; } = string.Empty;
		public bool Status { get; set; }
		public DateTime CreatedAt { get; set; }
		//public string ZoneColorCode { get; set; }
	}
	public class State
	{
		public int Id { get; set; }
		public string StateName { get; set; }
		public string ZoneId { get; set; }
		public bool Status { get; set; }
		public DateTime CreatedAt { get; set; }
	}
	public class District
	{
		public int Id { get; set; }
		public string StateId { get; set; }
		public string DistrictName { get; set; }
		public bool Status { get; set; }
		public DateTime CreatedAt { get; set; }
	}
	public class SubDistrict
	{
		public int Id { get; set; }
		public string StateId { get; set; }
		public string DistrictId { get; set; }
		public string SubDistrictName { get; set; }
		public bool Status { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
