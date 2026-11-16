using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public sealed record ExtensionRequestDetailsDto(
      int Id,
      int CSRId,
      string SanctionNumber,
      string ProgramName,
      string Location,
      string RequestedBy,
      DateTime RequestedOn,
      DateTime? SchemeFrom,
      DateTime PreviousEndDate,
      DateTime NewEndDate,
      decimal RequiredTurnover,
      decimal ActualTurnover,
      decimal AchievementPct,
      string DocumentStatus,
      string Reason,
      string Remarks,
      string? ApprovedBy,
      DateTime? ApprovedDate,
      string Status
  )
    {
        public string? RejectedBy { get; init; }
        public DateTime? RejectedDate { get; init; }
    }
    public sealed record ExtensionRequestListDto(
        int Id,
        int CSRId,
        string SanctionNumber,
        string ProgramName,
        string ProgramType,
        string Location,
        string RequestedBy,
        DateTime RequestedOn,
        DateTime? SchemeFrom,
        DateTime? SchemeTo,
        decimal RequiredTurnover,
        decimal ActualTurnover,
        DateTime NewEndDate,
        int ExtraDays,
        decimal AchievementPct,
        string DocumentStatus,
        string Status
    );

}
