using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class CSR1DetailsDto
    {
        public int Id { get; set; }

        // Program
        public int ProgramTypeId { get; set; }
        public string ProgramType { get; set; } = "";

        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = "";

        public int NumberOfPrograms { get; set; }

        public decimal Budget { get; set; }


        // Location
        public int? HeadquarterId { get; set; }
        public string HeadquarterName { get; set; } = "";

        public int? LocationId { get; set; }
        public string LocationName { get; set; } = "";

        public int? ResponsiblePersonId { get; set; }
        public string ResponsiblePersonName { get; set; } = "";


        // Expenses
        public decimal PrintingAndStationery { get; set; }
        public decimal PublicityMaterial { get; set; }
        public decimal StageArrangements { get; set; }
        public decimal ServiceChargesLCA { get; set; }
        public decimal TransportRent { get; set; }
        public decimal JeepRunningExpenses { get; set; }
        public decimal Refreshments { get; set; }
        public decimal Inputs { get; set; }
        public decimal Photography { get; set; }
        public decimal Compliments { get; set; }
        public decimal Others { get; set; }


        // Workflow
        public string Status { get; set; } = "";

        public string? Remarks { get; set; }

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }


        // Focus Groups
        public List<CSR1FocusDetailsDto> Focus1 { get; set; } = new();

        public List<CSR1FocusDetailsDto> Focus2 { get; set; } = new();

        public List<CSR1FocusDetailsDto> Focus3 { get; set; } = new();
    }


    public class CSR1FocusDetailsDto
    {
        public int CropId { get; set; }

        public string CropName { get; set; } = "";

        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";
    }
}
