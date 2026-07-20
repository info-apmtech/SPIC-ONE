using System.IO;
using System.Threading.Tasks;

namespace SPIC.Core.Interfaces
{
    public interface IExcelBulkUploadService
    {
        Task<ExcelBulkUploadResult> ImportAsync(Stream fileStream, string currentUserId, string fileExtension, string categoryId);
    }

    public class ExcelBulkUploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int RowsInserted { get; set; }
        public int RowsSkipped { get; set; }
        public WholesaleStockMastersSummary NewMastersCreated { get; set; } = new();
    }

    public class WholesaleStockMastersSummary
    {
        public int States { get; set; }
        public int Districts { get; set; }
        public int SubDistricts { get; set; }
        public int IfmsDealers { get; set; }
        public int DealerTypes { get; set; }
        public int DealershipNatures { get; set; }
        public int Companies { get; set; }
        public int Plants { get; set; }
        public int Products { get; set; }
    }
}
