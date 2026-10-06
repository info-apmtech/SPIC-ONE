using System;

namespace SPIC.Core.DTOs
{
    // MD Portal - Program Master page (Program Type / Program Master / State Mapping).

    public class ProgramTypeSaveRequest
    {
        public string Name { get; set; } = "";
    }

    public class ProgramMasterListDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int ProgramTypeId { get; set; }
        public string ProgramType { get; set; } = "";
        public bool IsMO { get; set; }
        public bool IsRMDO { get; set; }
        public bool IsSMDO { get; set; }
        public int ApplicableStateCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProgramMasterSaveRequest
    {
        public string Name { get; set; } = "";
        public int ProgramTypeId { get; set; }
        public bool IsMO { get; set; }
        public bool IsRMDO { get; set; }
        public bool IsSMDO { get; set; }
    }

    // One row per State in the State master; IsApplicable is false when no
    // ProgramStateMappings row exists yet (HasMapping = false).
    public class ProgramStateApplicabilityDto
    {
        public int StateId { get; set; }
        public string StateName { get; set; } = "";
        public bool IsApplicable { get; set; }
        public bool HasMapping { get; set; }
    }

    public class ProgramStateApplicabilityRequest
    {
        public bool IsApplicable { get; set; }
    }
}
