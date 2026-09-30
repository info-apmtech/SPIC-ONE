using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class CSR1ListDto
    {
        public int Id { get; set; }

        public int ProgramTypeId { get; set; }
        public string ProgramType { get; set; } = "";

        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = "";

        public int NumberOfPrograms { get; set; }
        public decimal Budget { get; set; }

        public int? HeadquarterId { get; set; }
        public int? LocationId { get; set; }
        public int? ResponsiblePersonId { get; set; }

        public string Status { get; set; } = "";
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public List<CSR1ProductDto> Products1 { get; set; } = new();
        public List<CSR1ProductDto> Products2 { get; set; } = new();
        public List<CSR1ProductDto> Products3 { get; set; } = new();
    }

    public class CSR1ProductDto
    {
        public int Id { get; set; }
        public int CropId { get; set; }
        public int ProductId { get; set; }
    }
}
