using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public sealed record SchemeExtensionInfoDto(
      int CSRId, string ProgramName, string SanctionNumber,
      DateTime SchemeFrom, DateTime SchemeTo,
      decimal RequiredTurnover, decimal ActualTurnover);

    public sealed record CreateExtensionRequestDto(
        DateTime NewEndDate, string Reason, string Remarks);

    public sealed record ExtensionRequestReceiptDto(
        int RequestId, DateTime RequestDate, DateTime PreviousEndDate,
        DateTime NewEndDate, string Reason, string Remarks);
}
