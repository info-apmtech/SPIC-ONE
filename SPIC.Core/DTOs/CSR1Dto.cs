using SPIC.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.DTOs
{
    public class CSR1Dto
    {
        public int Id { get; set; }

        public int ProgramTypeId { get; set; }

        public int ProgramId { get; set; }

        public int NumberOfPrograms { get; set; }

        public decimal Budget { get; set; }

        public int? HeadquarterId { get; set; }

        public int? LocationId { get; set; }

        public int? ResponsiblePersonId { get; set; }


        // Focus Details

        public int? FocusCrop1Id { get; set; }
        public int? FocusProduct1Id { get; set; }

        public int? FocusCrop2Id { get; set; }
        public int? FocusProduct2Id { get; set; }

        public int? FocusCrop3Id { get; set; }
        public int? FocusProduct3Id { get; set; }


        // Expense Details

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

        public string? Status { get; set; }
        public string? Remarks { get; set; }
    }

    public class CSR1SaveRequest
    {
        public CSR1 CSR1 { get; set; } = new();

        public int Crop1Id { get; set; }
        public List<int> Product1Ids { get; set; } = new();

        public int Crop2Id { get; set; }
        public List<int> Product2Ids { get; set; } = new();

        public int Crop3Id { get; set; }
        public List<int> Product3Ids { get; set; } = new();
    }
}
