using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SPIC.Core.Entities
{
    public class CSR1
    {
        [Key]
        public int Id { get; set; }

        // -------------------------
        // CSR-1 Plan Details
        // -------------------------

        public int ProgramTypeId { get; set; }

        public int ProgramId { get; set; }

        public int NumberOfPrograms { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Budget { get; set; }

        public int? HeadquarterId { get; set; }

        public int? LocationId { get; set; }

        public int? ResponsiblePersonId { get; set; }


        // -------------------------
        // Focus Details
        // -------------------------
        // Each focus section has one Crop
        // Products are stored in separate child tables
        // CSR1Products1 / CSRProducts2 / CSRProducts3
        // -------------------------

        //public int? FocusCrop1Id { get; set; }

        //public int? FocusCrop2Id { get; set; }

        //public int? FocusCrop3Id { get; set; }


        // -------------------------
        // Expense Breakdown
        // -------------------------

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrintingAndStationery { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PublicityMaterial { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal StageArrangements { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ServiceChargesLCA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TransportRent { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal JeepRunningExpenses { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Refreshments { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Inputs { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Photography { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Compliments { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Others { get; set; }


        // -------------------------
        // Workflow
        // -------------------------

        [MaxLength(50)]
        public string Status { get; set; } = "Draft";

        public string? Remarks { get; set; }


        // -------------------------
        // Audit
        // -------------------------

        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }



    // =========================================================
    // Focus Product Group 1
    // =========================================================

    public class CSR1Products1
    {
        [Key]
        public int Id { get; set; }

        public int CSR1Id { get; set; }

        public int CropId { get; set; }

        public int ProductId { get; set; }

        public bool IsActive { get; set; } = true;
      
    }


    // =========================================================
    // Focus Product Group 2
    // =========================================================

    public class CSR1Products2
    {
        [Key]
        public int Id { get; set; }

        public int CSR1Id { get; set; }

        public int CropId { get; set; }

        public int ProductId { get; set; }

        public bool IsActive { get; set; } = true;
    }


    // =========================================================
    // Focus Product Group 3
    // =========================================================

    public class CSR1Products3
    {
        [Key]
        public int Id { get; set; }

        public int CSR1Id { get; set; }

        public int CropId { get; set; }

        public int ProductId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
