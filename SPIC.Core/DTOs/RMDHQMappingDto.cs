using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SPIC.Core.DTOs;

public sealed class LookupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class SaveMappingRequest
{
    public int? Id { get; set; }
    [Range(1, int.MaxValue)] public int EmployeeId { get; set; }
    [Range(1, int.MaxValue)] public int StateId { get; set; }
    [Range(1, int.MaxValue)] public int RegionId { get; set; }
    [Required] public List<int> HeadquartersIds { get; set; } = new();
}

public sealed class MappingView
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int StateId { get; set; }
    public string StateName { get; set; } = "";
    public int RegionId { get; set; }
    public string RegionName { get; set; } = "";
    public string HeadquartersIds { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
