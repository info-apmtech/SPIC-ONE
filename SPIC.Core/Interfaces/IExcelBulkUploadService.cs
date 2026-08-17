using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SPIC.Core.DTOs;

namespace SPIC.Core.Interfaces
{
	public interface IExcelBulkUploadService
	{
		Task<ExcelBulkUploadResult> ImportAsync(
			Stream fileStream,
			string currentUserId,
			string fileExtension,
			string categoryId,
			string fileName,
			DateTime? reportDate,
			CancellationToken cancellationToken = default);
	}
}