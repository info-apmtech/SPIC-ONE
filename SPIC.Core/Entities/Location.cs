using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.Entities
{
	public class Zone
	{
		public int Id { get; set; }
		public required string ZoneName { get; set; }
		public string? ZoneCode { get; set; }
		public string? ZoneColorCode { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
	public class State
	{
		public int Id { get; set; }
		public required string StateName { get; set; }
		public int ZoneId { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class District
	{
		public int Id { get; set; }
		public required string DistrictName { get; set; }
		public int StateId { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
    public class SubDistrict
	{
		public int Id { get; set; }
		public required string SubDistrictName { get; set; }
		public int StateId { get; set; }
		public int DistrictId { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public required string UpdatedBy { get; set; }
    }
}
