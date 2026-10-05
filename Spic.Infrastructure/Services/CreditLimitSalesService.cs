//using Microsoft.EntityFrameworkCore;
//using Spic.Infrastructure.Data;
//using SPIC.Core.DTOs;
//using SPIC.Core.Entities;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//namespace Spic.Infrastructure.Services
//{

//    public interface ICreditLimitSalesService
//    {
//        Task<List<DealerCreditLimitSalesDto>> GetAllAsync();
//        Task<DealerCreditLimitSalesDto> GetByIdAsync(int id);
//        Task<DealerCreditLimitSalesDto> CreateAsync(DealerCreditLimitSalesDto dto);
//        Task<DealerCreditLimitSalesDto> UpdateAsync(int id, DealerCreditLimitSalesDto dto);
//        Task<bool> DeleteAsync(int id);
//        Task<bool> ToggleStatusAsync(int id, bool isActive);
//        Task<int> BulkUploadAsync(string filePath, string userId);
//    }

//    public class CreditLimitSalesService : ICreditLimitSalesService
//    {

//        public async Task<List<DealerCreditLimitSalesDto>> GetAllAsync()
//        {
//            try
//            {

//                return new List<DealerCreditLimitSalesDto>();
//            }
//            catch (Exception ex)
//            {

//                throw;
//            }
//        }
//        public async Task<DealerCreditLimitSalesDto> GetByIdAsync(int id)
//        {
//            try
//            {

//                return null;
//            }
//            catch (Exception ex)
//            {
//                throw;
//            }
//        }
//        public async Task<DealerCreditLimitSalesDto> CreateAsync(DealerCreditLimitSalesDto dto)
//        {
//            try
//            {
//                // Validate required fields
//                if (string.IsNullOrWhiteSpace(dto.State) ||
//                    string.IsNullOrWhiteSpace(dto.CustomerNumber) ||
//                    string.IsNullOrWhiteSpace(dto.CustomerName))
//                {
//                    throw new ArgumentException("Required fields are missing");
//                }


//                return dto;
//            }
//            catch (Exception ex)
//            {
//                throw;
//            }
//        }
//        public async Task<DealerCreditLimitSalesDto> UpdateAsync(int id, DealerCreditLimitSalesDto dto)
//        {
//            try
//            {

//                return dto;
//            }
//            catch (Exception ex)
//            {
//                throw;
//            }
//        }
//        public async Task<bool> DeleteAsync(int id)
//        {
//            try
//            {

//                return true;
//            }
//            catch (Exception ex)
//            {

//                throw;
//            }
//        }
//        public async Task<bool> ToggleStatusAsync(int id, bool isActive)
//        {
//            try
//            {

//                return true;
//            }
//            catch (Exception ex)
//            {

//                throw;
//            }
//        }
//        public async Task<int> BulkUploadAsync(string filePath, string userId)
//        {
//            try
//            {
//                int recordsAdded = 0;

//                return recordsAdded;
//            }
//            catch (Exception ex)
//            {

//                throw;
//            }
//        }

//    }
//}
