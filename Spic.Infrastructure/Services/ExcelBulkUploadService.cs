using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Spic.Infrastructure.Services
{
    public class ExcelBulkUploadService : IExcelBulkUploadService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ExcelBulkUploadService> _logger;

        public ExcelBulkUploadService(AppDbContext db, ILogger<ExcelBulkUploadService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ExcelBulkUploadResult> ImportAsync(Stream fileStream, string currentUserId, string fileExtension, string categoryId)
        {
            var now = DateTime.UtcNow;
            var records = new List<Dictionary<string, string>>();

            var requiredCols = categoryId == "One" 
                ? new[] { "statename", "districtname", "retailerid", "retailername" }
                : categoryId == "Three"
                ? new[] { "company", "plant", "product", "state", "district", "agencyname" }
                : new[] { "state", "district", "dealerid", "agencyname", "dealertype", "dealershipnature", "company", "plant", "product", "stock", "stockdate" };
            
            bool IsHeaderRow(List<string> rowValues)
            {
                if (categoryId == "One") return rowValues.Contains("statename") && rowValues.Contains("retailerid");
                if (categoryId == "Three") return rowValues.Contains("serialnumber") && rowValues.Contains("agencyname");
                return rowValues.Contains("state") && rowValues.Contains("dealerid");
            }

            try
            {
                if (fileExtension == ".csv")
                {
                    using var reader = new StreamReader(fileStream);
                    using var csv = new CsvHelper.CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture) { HasHeaderRecord = false });
                    
                    var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    bool headerFound = false;

                    while (csv.Read())
                    {
                        if (!headerFound)
                        {
                            var rowValues = new List<string>();
                            for (int i = 0; csv.TryGetField<string>(i, out var field); i++)
                            {
                                rowValues.Add(field?.Trim('\uFEFF', '\u200B', ' ', '"').Replace(" ", "").ToLowerInvariant() ?? "");
                            }

                            if (IsHeaderRow(rowValues))
                            {
                                headerFound = true;
                                for (int i = 0; i < rowValues.Count; i++)
                                {
                                    var h = rowValues[i];
                                    if (categoryId == "Three" && h.StartsWith("wholesalerob")) h = "wholesalerob";
                                    if (!string.IsNullOrEmpty(h) && !headerMap.ContainsKey(h))
                                        headerMap[h] = i;
                                }
                            }
                            continue;
                        }

                        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var kvp in headerMap)
                        {
                            dict[kvp.Key] = csv.GetField(kvp.Value)?.Trim() ?? string.Empty;
                        }
                        
                        if (dict.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                        {
                            records.Add(dict);
                        }
                    }
                }
                else
                {
                    using var workbook = new XLWorkbook(fileStream);
                    var ws = workbook.Worksheets.First();
                    
                    var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    int headerRowIndex = -1;

                    var rows = ws.RowsUsed().Take(10).ToList();
                    foreach (var row in rows)
                    {
                        var rowValues = new List<string>();
                        int lastCol = row.LastCellUsed()?.Address.ColumnNumber ?? 0;
                        for (int c = 1; c <= lastCol; c++)
                        {
                            rowValues.Add(row.Cell(c).GetString().Trim('\uFEFF', '\u200B', ' ', '"').Replace(" ", "").ToLowerInvariant());
                        }

                        if (IsHeaderRow(rowValues))
                        {
                            headerRowIndex = row.RowNumber();
                            for (int c = 1; c <= lastCol; c++)
                            {
                                var h = rowValues[c - 1];
                                if (categoryId == "Three" && h.StartsWith("wholesalerob")) h = "wholesalerob";
                                if (!string.IsNullOrEmpty(h) && !headerMap.ContainsKey(h))
                                    headerMap[h] = c;
                            }
                            break;
                        }
                    }

                    if (headerRowIndex != -1)
                    {
                        var dataRows = ws.RowsUsed().Where(r => r.RowNumber() > headerRowIndex).ToList();
                        foreach (var row in dataRows)
                        {
                            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            foreach (var kvp in headerMap)
                            {
                                dict[kvp.Key] = row.Cell(kvp.Value).GetString().Trim();
                            }
                            
                            if (dict.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                            {
                                records.Add(dict);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new ExcelBulkUploadResult { Success = false, Message = "Failed to parse file format: " + ex.Message };
            }

            if (records.Count == 0)
            {
                return new ExcelBulkUploadResult { Success = false, Message = "No data rows found. Please ensure the file contains valid headers and data." };
            }

            foreach (var rc in requiredCols)
            {
                if (!records[0].ContainsKey(rc))
                {
                    var foundHeaders = string.Join(", ", records[0].Keys);
                    return new ExcelBulkUploadResult { Success = false, Message = $"Missing required column: {rc}. Found headers: {foundHeaders}" };
                }
            }

            string GetCell(Dictionary<string, string> rowDict, string key) =>
                rowDict.TryGetValue(key, out var val) ? val : string.Empty;

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var result = new ExcelBulkUploadResult { Success = true, Message = "Upload successful" };

                var stateDict = await _db.States.ToDictionaryAsync(s => s.StateName.Trim().ToLowerInvariant(), s => s.Id);
                var districtDict = await _db.Districts.ToDictionaryAsync(d => $"{d.DistrictName.Trim().ToLowerInvariant()}_{d.StateId}", d => d.Id);
                var subDistrictDict = await _db.SubDistricts.ToDictionaryAsync(sd => $"{sd.SubDistrictName.Trim().ToLowerInvariant()}_{sd.DistrictId}", sd => sd.Id);
                
                var rawRegDealers = await _db.DealerRegistrations
                    .Where(d => d.FirmName != null)
                    .Select(d => new { d.Id, d.FirmName })
                    .ToListAsync();
                    
                var dealerRegDict = rawRegDealers
                    .GroupBy(d => d.FirmName.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var rawIfmsDealers = await _db.IfmsDealers
                    .Where(d => d.Name != null)
                    .Select(d => new { d.Id, d.Name })
                    .ToListAsync();
                    
                var ifmsDealerByNameDict = rawIfmsDealers
                    .GroupBy(d => d.Name.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First().Id);

                var dealerTypeDict = await _db.DealerTypes.ToDictionaryAsync(dt => dt.Name.Trim().ToLowerInvariant(), dt => dt.Id);
                var natureDict = await _db.DealershipNatures.ToDictionaryAsync(n => n.Name.Trim().ToLowerInvariant(), n => n.Id);
                var companyDict = await _db.Companies.ToDictionaryAsync(c => c.Name.Trim().ToLowerInvariant(), c => c.Id);
                var plantDict = await _db.Plants.ToDictionaryAsync(p => p.Name.Trim().ToLowerInvariant(), p => p.Id);
                var productDict = await _db.Products.ToDictionaryAsync(p => p.Name.Trim().ToLowerInvariant(), p => p.Id);

                for (int i = 0; i < records.Count; i++)
                {
                    var row = records[i];
                    int rowNumber = i + 2;
                    
                    var stateStr = categoryId == "One" ? GetCell(row, "statename") : GetCell(row, "state");
                    var districtStr = categoryId == "One" ? GetCell(row, "districtname") : GetCell(row, "district");
                    var dealerIdStr = categoryId == "One" ? GetCell(row, "retailerid") : GetCell(row, "dealerid");
                    var agencyNameStr = categoryId == "One" ? GetCell(row, "retailername") : GetCell(row, "agencyname");
                    var dealerTypeStr = categoryId == "One" ? "" : GetCell(row, "dealertype");
                    var natureStr = categoryId == "Three" ? GetCell(row, "dealernature") : GetCell(row, "dealershipnature");
                    var companyStr = GetCell(row, "company");
                    var plantStr = GetCell(row, "plant");
                    var productStr = GetCell(row, "product");

                    if (string.IsNullOrEmpty(stateStr) && string.IsNullOrEmpty(districtStr) && string.IsNullOrEmpty(dealerIdStr) && string.IsNullOrEmpty(agencyNameStr))
                    {
                        result.RowsSkipped++;
                        continue;
                    }

                    int? stateId = null;
                    if (!string.IsNullOrEmpty(stateStr))
                    {
                        var key = stateStr.ToLowerInvariant();
                        if (!stateDict.TryGetValue(key, out var id))
                        {
                            var newState = new State { StateName = stateStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId, ZoneId = 1 };
                            _db.States.Add(newState);
                            await _db.SaveChangesAsync();
                            id = newState.Id;
                            stateDict[key] = id;
                            result.NewMastersCreated.States++;
                        }
                        stateId = id;
                    }

                    int? districtId = null;
                    if (!string.IsNullOrEmpty(districtStr) && stateId.HasValue)
                    {
                        var key = $"{districtStr.ToLowerInvariant()}_{stateId.Value}";
                        if (!districtDict.TryGetValue(key, out var id))
                        {
                            var newDistrict = new District { DistrictName = districtStr, StateId = stateId.Value, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                            _db.Districts.Add(newDistrict);
                            await _db.SaveChangesAsync();
                            id = newDistrict.Id;
                            districtDict[key] = id;
                            result.NewMastersCreated.Districts++;
                        }
                        districtId = id;
                    }

                    int? subDistrictId = null;
                    if (categoryId == "One")
                    {
                        var subDistrictStr = GetCell(row, "subdistrict");
                        if (!string.IsNullOrEmpty(subDistrictStr) && districtId.HasValue)
                        {
                            var key = $"{subDistrictStr.ToLowerInvariant()}_{districtId.Value}";
                            if (!subDistrictDict.TryGetValue(key, out var id))
                            {
                                var newSubDistrict = new SubDistrict { SubDistrictName = subDistrictStr, DistrictId = districtId.Value, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.SubDistricts.Add(newSubDistrict);
                                await _db.SaveChangesAsync();
                                id = newSubDistrict.Id;
                                subDistrictDict[key] = id;
                                result.NewMastersCreated.SubDistricts++;
                            }
                            subDistrictId = id;
                        }
                    }

                    int? dealerTypeId = null;
                    if (!string.IsNullOrEmpty(dealerTypeStr))
                    {
                        var key = dealerTypeStr.ToLowerInvariant();
                        if (!dealerTypeDict.TryGetValue(key, out var id))
                        {
                            var newType = new DealerType { Name = dealerTypeStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                            _db.DealerTypes.Add(newType);
                            await _db.SaveChangesAsync();
                            id = newType.Id;
                            dealerTypeDict[key] = id;
                            result.NewMastersCreated.DealerTypes++;
                        }
                        dealerTypeId = id;
                    }

                    int? natureId = null;
                    if (!string.IsNullOrEmpty(natureStr))
                    {
                        var key = natureStr.ToLowerInvariant();
                        if (!natureDict.TryGetValue(key, out var id))
                        {
                            var newNat = new DealershipNature { Name = natureStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                            _db.DealershipNatures.Add(newNat);
                            await _db.SaveChangesAsync();
                            id = newNat.Id;
                            natureDict[key] = id;
                            result.NewMastersCreated.DealershipNatures++;
                        }
                        natureId = id;
                    }

                    int? dealerRegistrationId = null;
                    int? ifmsDealerId = null;
                    bool dealerFoundInRegistration = false;

                    if (!string.IsNullOrEmpty(agencyNameStr))
                    {
                        var key = agencyNameStr.ToLowerInvariant();
                        if (dealerRegDict.TryGetValue(key, out var regId))
                        {
                            dealerRegistrationId = regId;
                            dealerFoundInRegistration = true;
                        }
                    }

                    if (!dealerFoundInRegistration && !string.IsNullOrEmpty(agencyNameStr))
                    {
                        var keyName = agencyNameStr.ToLowerInvariant();
                        if (!ifmsDealerByNameDict.TryGetValue(keyName, out int id))
                        {
                            var newDealer = new IfmsDealer { Name = agencyNameStr, StateId = stateId, DistrictId = districtId, DealerTypeId = dealerTypeId, DealershipNatureId = natureId, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                            _db.IfmsDealers.Add(newDealer);
                            await _db.SaveChangesAsync();
                            id = newDealer.Id;
                            ifmsDealerByNameDict[keyName] = id;
                            result.NewMastersCreated.IfmsDealers++;
                        }
                        ifmsDealerId = id;
                    }

                    int? companyId = null;
                    if (!string.IsNullOrEmpty(companyStr))
                    {
                        var key = companyStr.ToLowerInvariant();
                        if (!companyDict.TryGetValue(key, out var id))
                        {
                            var newComp = new Company { Name = companyStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                            _db.Companies.Add(newComp);
                            await _db.SaveChangesAsync();
                            id = newComp.Id;
                            companyDict[key] = id;
                            result.NewMastersCreated.Companies++;
                        }
                        companyId = id;
                    }

                    int? plantId = null;
                    if (!string.IsNullOrEmpty(plantStr))
                    {
                        var key = plantStr.ToLowerInvariant();
                        if (!plantDict.TryGetValue(key, out var id))
                        {
                            var newPlant = new Plant { Name = plantStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                            _db.Plants.Add(newPlant);
                            await _db.SaveChangesAsync();
                            id = newPlant.Id;
                            plantDict[key] = id;
                            result.NewMastersCreated.Plants++;
                        }
                        plantId = id;
                    }

                    int? productId = null;
                    if (!string.IsNullOrEmpty(productStr))
                    {
                        var key = productStr.ToLowerInvariant();
                        if (!productDict.TryGetValue(key, out var id))
                        {
                            var newProd = new Product { Name = productStr, CategoryId = 1, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                            _db.Products.Add(newProd);
                            await _db.SaveChangesAsync();
                            id = newProd.Id;
                            productDict[key] = id;
                            result.NewMastersCreated.Products++;
                        }
                        productId = id;
                    }

                    if (categoryId == "One")
                    {
                        var mobileNoStr = GetCell(row, "mobileno.");
                        var openBalStr = GetCell(row, "openingbalance");
                        var recvQtyStr = GetCell(row, "receivedquantity");
                        var soldQtyStr = GetCell(row, "soldquantity");
                        var availStr = GetCell(row, "availabilty");
                        var closeBalStr = GetCell(row, "closingbalance");

                        decimal.TryParse(openBalStr, out var openBal);
                        decimal.TryParse(recvQtyStr, out var recvQty);
                        decimal.TryParse(soldQtyStr, out var soldQty);
                        decimal.TryParse(availStr, out var avail);
                        decimal.TryParse(closeBalStr, out var closeBal);

                        var dptRecord = new DptReport
                        {
                            StateId = stateId,
                            DistrictId = districtId,
                            SubDistrictId = subDistrictId,
                            RetailerName = agencyNameStr,
                            DealerRegistrationId = dealerRegistrationId,
                            IfmsDealerId = ifmsDealerId,
                            MobileNo = mobileNoStr,
                            DealershipNatureId = natureId,
                            CompanyId = companyId,
                            PlantId = plantId,
                            ProductId = productId,
                            OpeningBalance = openBal,
                            ReceivedQuantity = recvQty,
                            SoldQuantity = soldQty,
                            Availability = avail,
                            ClosingBalance = closeBal,
                            CreatedAt = now,
                            UpdatedAt = now,
                            UpdatedBy = currentUserId
                        };
                        _db.DptReports.Add(dptRecord);
                        result.RowsInserted++;
                    }
                    else if (categoryId == "Five")
                    {
                        var stockStr = GetCell(row, "stock");
                        var stockDateStr = GetCell(row, "stockdate");

                        if (!decimal.TryParse(stockStr, out var stockValue))
                        {
                            throw new Exception($"Row {rowNumber}: Invalid stock value '{stockStr}'.");
                        }

                        DateTime stockDate;
                        if (DateTime.TryParseExact(stockDateStr, "dd-MM-yyyy", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                        {
                            stockDate = parsedDate;
                        }
                        else if (DateTime.TryParse(stockDateStr, out parsedDate))
                        {
                            stockDate = parsedDate;
                        }
                        else
                        {
                            throw new Exception($"Row {rowNumber}: Invalid stock date '{stockDateStr}'. Expected format dd-MM-yyyy.");
                        }

                        var stockRecord = new WholesalerStockAsOnToday
                        {
                            StateId = stateId,
                            DistrictId = districtId,
                            DealerRegistrationId = dealerRegistrationId,
                            IfmsDealerId = ifmsDealerId,
                            AgencyName = agencyNameStr,
                            DealerTypeId = dealerTypeId,
                            DealershipNatureId = natureId,
                            CompanyId = companyId,
                            PlantId = plantId,
                            ProductId = productId,
                            Stock = stockValue,
                            StockDate = stockDate.ToUniversalTime(),
                            CreatedAt = now,
                            UpdatedAt = now,
                            UpdatedBy = currentUserId
                        };

                        _db.WholesalerStockAsOnTodays.Add(stockRecord);
                        result.RowsInserted++;
                    }
                    else if (categoryId == "Three")
                    {
                        decimal.TryParse(GetCell(row, "wholesalerob"), out var openingBalance);
                        decimal.TryParse(GetCell(row, "comp-wssale"), out var compWsSale);
                        decimal.TryParse(GetCell(row, "comp-wssalercpt"), out var compWsSaleRcpt);
                        decimal.TryParse(GetCell(row, "receivedfromws"), out var receivedFromWs);
                        decimal.TryParse(GetCell(row, "receivedfromwsack"), out var receivedFromWsAck);
                        decimal.TryParse(GetCell(row, "ws-rtsale"), out var wsRtSale);
                        decimal.TryParse(GetCell(row, "ws-rtsalercpt"), out var wsRtSaleRcpt);
                        decimal.TryParse(GetCell(row, "ws-wssale"), out var wsWsSale);
                        decimal.TryParse(GetCell(row, "ws-wssalercpt"), out var wsWsSaleRcpt);
                        decimal.TryParse(GetCell(row, "totalsalesbyws"), out var totalSalesByWs);
                        decimal.TryParse(GetCell(row, "stocktransferfromwstoretailer"), out var stockTransferWsToRetailer);
                        decimal.TryParse(GetCell(row, "stocktransferfromwstoretailerack"), out var stockTransferWsToRetailerAck);
                        decimal.TryParse(GetCell(row, "balancewithws"), out var balanceWithWs);
                        decimal.TryParse(GetCell(row, "totalacktows"), out var totalAckToWs);

                        var srRecord = new SalesAndReceipt
                        {
                            CompanyId = companyId,
                            PlantId = plantId,
                            ProductId = productId,
                            StateId = stateId,
                            DistrictId = districtId,
                            DealershipNatureId = natureId,
                            AgencyName = agencyNameStr,
                            DealerRegistrationId = dealerRegistrationId,
                            IfmsDealerId = ifmsDealerId,
                            OpeningBalance = openingBalance,
                            CompWsSale = compWsSale,
                            CompWsSaleRcpt = compWsSaleRcpt,
                            ReceivedFromWs = receivedFromWs,
                            ReceivedFromWsAck = receivedFromWsAck,
                            WsRtSale = wsRtSale,
                            WsRtSaleRcpt = wsRtSaleRcpt,
                            WsWsSale = wsWsSale,
                            WsWsSaleRcpt = wsWsSaleRcpt,
                            TotalSalesByWs = totalSalesByWs,
                            StockTransferWsToRetailer = stockTransferWsToRetailer,
                            StockTransferWsToRetailerAck = stockTransferWsToRetailerAck,
                            BalanceWithWs = balanceWithWs,
                            TotalAckToWs = totalAckToWs,
                            CreatedAt = now,
                            UpdatedAt = now,
                            UpdatedBy = currentUserId
                        };
                        _db.SalesAndReceipts.Add(srRecord);
                        result.RowsInserted++;
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing bulk upload");
                string errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return new ExcelBulkUploadResult { Success = false, Message = $"Upload failed: {ex.Message} | Inner: {errorMsg}" };
            }
        }
    }
}
