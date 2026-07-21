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
                : categoryId == "Two"
                ? new[] { "transactionid", "invoiceno", "dealername" }
                : categoryId == "Three"
                ? new[] { "company", "plant", "product", "state", "district", "agencyname" }
                : categoryId == "Four"
                ? new[] { "transactionid", "marketer", "wholesalerid", "wholesaleragencyname" }
                : categoryId == "Six"
                ? new[] { "state", "openingstock", "openinggit", "production/imports", "receipt", "dispatches", "sales", "salesreturn", "stockadjustment", "closinggit", "closingstock" }
                : categoryId == "Seven"
                ? new[] { "state", "district", "warehouse/location", "openingstock(atlocation)", "openingstock(git)", "imports/production", "receipt", "dispatches", "sales", "salesreturn", "stockadjustment", "closinggit", "closingstock" }
                : new[] { "state", "district", "dealerid", "agencyname", "dealertype", "dealershipnature", "company", "plant", "product", "stock", "stockdate" };
            
            bool IsHeaderRow(List<string> rowValues)
            {
                if (categoryId == "One") return rowValues.Contains("statename") && rowValues.Contains("retailerid");
                if (categoryId == "Two") return rowValues.Contains("transactionid") && rowValues.Contains("dealername");
                if (categoryId == "Three") return rowValues.Contains("serialnumber") && rowValues.Contains("agencyname");
                if (categoryId == "Four") return rowValues.Contains("transactionid") && rowValues.Contains("marketer");
                if (categoryId == "Six") return rowValues.Contains("state") && rowValues.Contains("openingstock");
                if (categoryId == "Seven") return rowValues.Contains("state") && rowValues.Contains("district") && rowValues.Contains("warehouse/location");
                return rowValues.Contains("state") && rowValues.Contains("dealerid");
            }

            string globalPlantStr = null;
            string globalProductStr = null;
            void ExtractCategoryTitle(string cellText)
            {
                if (!string.IsNullOrWhiteSpace(cellText))
                {
                    if ((categoryId == "Six" && cellText.StartsWith("State-Wise Global Stock Reconciliation", StringComparison.OrdinalIgnoreCase)) ||
                        (categoryId == "Seven" && cellText.StartsWith("District-Wise Details Global Stock Reconciliation", StringComparison.OrdinalIgnoreCase)))
                    {
                        var parts = cellText.Split(new[] { " for " }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 3)
                        {
                            globalPlantStr = parts[1].Trim();
                            if (globalPlantStr.Equals("GFL", StringComparison.OrdinalIgnoreCase))
                            {
                                globalPlantStr = "Green Star";
                            }
                            globalProductStr = parts[2].Trim();
                        }
                    }
                }
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
                                var text = field?.Trim() ?? "";
                                ExtractCategoryTitle(text);
                                rowValues.Add(text.Trim('\uFEFF', '\u200B', ' ', '"').Replace(" ", "").ToLowerInvariant());
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
                            var text = row.Cell(c).GetString().Trim();
                            ExtractCategoryTitle(text);
                            rowValues.Add(text.Trim('\uFEFF', '\u200B', ' ', '"').Replace(" ", "").ToLowerInvariant());
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
                

                var txnTypeDict = await _db.TxnTypes.ToDictionaryAsync(t => t.Name.Trim().ToLowerInvariant(), t => t.Id);
                var unitDict = await _db.Units.ToDictionaryAsync(u => u.Name.Trim().ToLowerInvariant(), u => u.Id);
                var statusDict = await _db.Statuses.ToDictionaryAsync(s => s.Name.Trim().ToLowerInvariant(), s => s.Id);
                var ackThroughDict = await _db.AckThroughs.ToDictionaryAsync(a => a.Name.Trim().ToLowerInvariant(), a => a.Id);
                var warehouseDict = await _db.Warehouses.ToDictionaryAsync(w => w.Name.Trim().ToLowerInvariant(), w => w.Id);

                string lastStateStr = string.Empty;

                for (int i = 0; i < records.Count; i++)
                {
                    var row = records[i];
                    int rowNumber = i + 2;
                    
                    var stateStr = categoryId == "One" ? GetCell(row, "statename") : GetCell(row, "state");
                    var districtStr = categoryId == "One" ? GetCell(row, "districtname") : GetCell(row, "district");
                    var dealerIdStr = categoryId == "One" ? GetCell(row, "retailerid") : GetCell(row, "dealerid");
                    var agencyNameStr = categoryId == "One" ? GetCell(row, "retailername") : categoryId == "Two" ? GetCell(row, "dealername") : GetCell(row, "agencyname");
                    var dealerTypeStr = categoryId == "One" ? "" : GetCell(row, "dealertype");
                    var natureStr = categoryId == "Three" ? GetCell(row, "dealernature") : GetCell(row, "dealershipnature");
                    var companyStr = (categoryId == "Four" || categoryId == "Two") ? GetCell(row, "manufacturer") : GetCell(row, "company");
                    var plantStr = GetCell(row, "plant");
                    var productStr = (categoryId == "Four" || categoryId == "Two") ? GetCell(row, "companyproduct") : GetCell(row, "product");

                    if (categoryId == "Seven")
                    {
                        if (string.IsNullOrWhiteSpace(stateStr) && !string.IsNullOrWhiteSpace(districtStr))
                        {
                            stateStr = lastStateStr;
                        }
                        else
                        {
                            lastStateStr = stateStr;
                        }
                    }

                    if (string.IsNullOrEmpty(stateStr) && string.IsNullOrEmpty(districtStr) && string.IsNullOrEmpty(dealerIdStr) && string.IsNullOrEmpty(agencyNameStr) && string.IsNullOrEmpty(GetCell(row, "warehouse/location")))
                    {
                        result.RowsSkipped++;
                        continue;
                    }
                    if (categoryId == "Six" && string.Equals(stateStr, "total", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    int? stateId = null;
                    if ((categoryId != "Six" && categoryId != "Seven") || !stateStr.Trim().Equals("plant", StringComparison.OrdinalIgnoreCase))
                    {
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
                    }
                    else if (categoryId == "Four")
                    {
                        var marketerStr = GetCell(row, "marketer");
                        var ackThroughStr = GetCell(row, "ackthrough");
                        var wholesalerNatureStr = GetCell(row, "wholesalernature");
                        var dealerNatureStr = GetCell(row, "dealernature");
                        var unitStr = GetCell(row, "unit");
                        var statusStr = GetCell(row, "status");
                        var txnTypeStr = GetCell(row, "txntype");
                        
                        var wholesalerAgencyStr = GetCell(row, "wholesaleragencyname");
                        var sellerDistrictStr = GetCell(row, "sellerdistrict");
                        var buyerDistrictStr = GetCell(row, "buyerdistrict");

                        int? marketerId = null;
                        if (!string.IsNullOrEmpty(marketerStr))
                        {
                            var key = marketerStr.ToLowerInvariant();
                            if (!companyDict.TryGetValue(key, out var id))
                            {
                                var newComp = new Company { Name = marketerStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Companies.Add(newComp);
                                await _db.SaveChangesAsync();
                                id = newComp.Id;
                                companyDict[key] = id;
                            }
                            marketerId = id;
                        }

                        int? ackThroughId = null;
                        if (!string.IsNullOrEmpty(ackThroughStr))
                        {
                            var key = ackThroughStr.ToLowerInvariant();
                            if (!ackThroughDict.TryGetValue(key, out var id))
                            {
                                var newAck = new AckThrough { Name = ackThroughStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.AckThroughs.Add(newAck);
                                await _db.SaveChangesAsync();
                                id = newAck.Id;
                                ackThroughDict[key] = id;
                            }
                            ackThroughId = id;
                        }

                        int? txnTypeId = null;
                        if (!string.IsNullOrEmpty(txnTypeStr))
                        {
                            var key = txnTypeStr.ToLowerInvariant();
                            if (!txnTypeDict.TryGetValue(key, out var id))
                            {
                                var newT = new TxnType { Name = txnTypeStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.TxnTypes.Add(newT);
                                await _db.SaveChangesAsync();
                                id = newT.Id;
                                txnTypeDict[key] = id;
                            }
                            txnTypeId = id;
                        }

                        int? unitId = null;
                        if (!string.IsNullOrEmpty(unitStr))
                        {
                            var key = unitStr.ToLowerInvariant();
                            if (!unitDict.TryGetValue(key, out var id))
                            {
                                var newU = new Unit { Name = unitStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Units.Add(newU);
                                await _db.SaveChangesAsync();
                                id = newU.Id;
                                unitDict[key] = id;
                            }
                            unitId = id;
                        }

                        int? statusId = null;
                        if (!string.IsNullOrEmpty(statusStr))
                        {
                            var key = statusStr.ToLowerInvariant();
                            if (!statusDict.TryGetValue(key, out var id))
                            {
                                var newS = new Status { Name = statusStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Statuses.Add(newS);
                                await _db.SaveChangesAsync();
                                id = newS.Id;
                                statusDict[key] = id;
                            }
                            statusId = id;
                        }

                        int? wholesalerNatureId = null;
                        if (!string.IsNullOrEmpty(wholesalerNatureStr))
                        {
                            var key = wholesalerNatureStr.ToLowerInvariant();
                            if (!natureDict.TryGetValue(key, out var id))
                            {
                                var newN = new DealershipNature { Name = wholesalerNatureStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.DealershipNatures.Add(newN);
                                await _db.SaveChangesAsync();
                                id = newN.Id;
                                natureDict[key] = id;
                            }
                            wholesalerNatureId = id;
                        }
                        
                        int? dealerNatureId = null;
                        if (!string.IsNullOrEmpty(dealerNatureStr))
                        {
                            var key = dealerNatureStr.ToLowerInvariant();
                            if (!natureDict.TryGetValue(key, out var id))
                            {
                                var newN = new DealershipNature { Name = dealerNatureStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.DealershipNatures.Add(newN);
                                await _db.SaveChangesAsync();
                                id = newN.Id;
                                natureDict[key] = id;
                            }
                            dealerNatureId = id;
                        }

                        int? sellerDistrictId = null;
                        if (!string.IsNullOrEmpty(sellerDistrictStr) && stateId.HasValue)
                        {
                            var key = $"{sellerDistrictStr.ToLowerInvariant()}_{stateId.Value}";
                            if (!districtDict.TryGetValue(key, out var id))
                            {
                                var newDistrict = new District { DistrictName = sellerDistrictStr, StateId = stateId.Value, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Districts.Add(newDistrict);
                                await _db.SaveChangesAsync();
                                id = newDistrict.Id;
                                districtDict[key] = id;
                            }
                            sellerDistrictId = id;
                        }

                        int? buyerDistrictId = null;
                        if (!string.IsNullOrEmpty(buyerDistrictStr) && stateId.HasValue)
                        {
                            var key = $"{buyerDistrictStr.ToLowerInvariant()}_{stateId.Value}";
                            if (!districtDict.TryGetValue(key, out var id))
                            {
                                var newDistrict = new District { DistrictName = buyerDistrictStr, StateId = stateId.Value, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Districts.Add(newDistrict);
                                await _db.SaveChangesAsync();
                                id = newDistrict.Id;
                                districtDict[key] = id;
                            }
                            buyerDistrictId = id;
                        }

                        int? wholesalerRegistrationId = null;
                        int? ifmsWholesalerId = null;
                        bool wholesalerFoundInRegistration = false;
                        
                        if (!string.IsNullOrEmpty(wholesalerAgencyStr))
                        {
                            var key = wholesalerAgencyStr.ToLowerInvariant();
                            if (dealerRegDict.TryGetValue(key, out var regId))
                            {
                                wholesalerRegistrationId = regId;
                                wholesalerFoundInRegistration = true;
                            }
                        }

                        if (!wholesalerFoundInRegistration && !string.IsNullOrEmpty(wholesalerAgencyStr))
                        {
                            var keyName = wholesalerAgencyStr.ToLowerInvariant();
                            if (!ifmsDealerByNameDict.TryGetValue(keyName, out int id))
                            {
                                var newDealer = new IfmsDealer { Name = wholesalerAgencyStr, StateId = stateId, DistrictId = districtId, DealershipNatureId = wholesalerNatureId, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.IfmsDealers.Add(newDealer);
                                await _db.SaveChangesAsync();
                                id = newDealer.Id;
                                ifmsDealerByNameDict[keyName] = id;
                            }
                            ifmsWholesalerId = id;
                        }

                        DateTime.TryParse(GetCell(row, "invoicedate"), out var invDate);
                        DateTime.TryParse(GetCell(row, "entrydate"), out var entDate);
                        DateTime.TryParse(GetCell(row, "lockdate"), out var lckDate);
                        DateTime.TryParse(GetCell(row, "retailerreceiptdate"), out var rrDate);

                        decimal.TryParse(GetCell(row, "quantity"), out var qty);
                        decimal.TryParse(GetCell(row, "quantity(mt)"), out var qtymt);
                        decimal.TryParse(GetCell(row, "receivedquantity(mt)"), out var recvQtymt);
                        decimal.TryParse(GetCell(row, "month1qty"), out var m1qty);
                        decimal.TryParse(GetCell(row, "month2qty"), out var m2qty);
                        decimal.TryParse(GetCell(row, "lorrycapacity"), out var lorryCap);

                        var salesWholesaler = new SalesWholesaler
                        {
                            TransactionId = GetCell(row, "transactionid"),
                            InvoiceNo = GetCell(row, "invoiceno"),
                            InvoiceDate = invDate == default ? null : invDate.ToUniversalTime(),
                            MarketerId = marketerId,
                            ManufacturerId = companyId,
                            PlantId = plantId,
                            WholesalerId = wholesalerRegistrationId,
                            IfmsWholesalerId = ifmsWholesalerId,
                            WholesalerAgencyName = wholesalerAgencyStr,
                            WholesalerNatureId = wholesalerNatureId,
                            StateId = stateId,
                            SellerDistrictId = sellerDistrictId,
                            BuyerDistrictId = buyerDistrictId,
                            DealerId = dealerRegistrationId, 
                            DealerTypeId = dealerTypeId,
                            IfmsDealerId = ifmsDealerId, 
                            AgencyName = agencyNameStr,
                            DealerNatureId = dealerNatureId,
                            MobileNo = GetCell(row, "mobileno"),
                            ProductId = productId,
                            UnitId = unitId,
                            Quantity = qty,
                            QuantityMT = qtymt,
                            ReceivedQuantityMT = recvQtymt,
                            StatusId = statusId,
                            TxnTypeId = txnTypeId,
                            EntryDate = entDate == default ? null : entDate.ToUniversalTime(),
                            LockDate = lckDate == default ? null : lckDate.ToUniversalTime(),
                            AckThroughId = ackThroughId,
                            TxnRemark = GetCell(row, "txnremark"),
                            SubsidyMonth1 = GetCell(row, "subsidymonth1"),
                            SubsidyYear1 = GetCell(row, "subsidyyear1"),
                            Month1Qty = m1qty,
                            SubsidyMonth2 = GetCell(row, "subsidymonth2"),
                            SubsidyYear2 = GetCell(row, "subsidyyear2"),
                            Month2Qty = m2qty,
                            ChallanNo = GetCell(row, "challanno"),
                            LorryNo = GetCell(row, "lorryno"),
                            LorryCapacity = lorryCap,
                            DispatchNo = GetCell(row, "dispatchno"),
                            RetailerReceiptDate = rrDate == default ? null : rrDate.ToUniversalTime(),
                            CreatedAt = now,
                            UpdatedAt = now,
                            UpdatedBy = currentUserId
                        };

                        _db.SalesWholesalers.Add(salesWholesaler);
                        result.RowsInserted++;
                    }
                    else if (categoryId == "Two")
                    {
                        var marketerStr = GetCell(row, "marketer");
                        var ackThroughStr = GetCell(row, "ackthrough");
                        var unitStr = GetCell(row, "unit");
                        var statusStr = GetCell(row, "status");
                        var txnTypeStr = GetCell(row, "txntype");
                        
                        int? marketerId = null;
                        if (!string.IsNullOrEmpty(marketerStr))
                        {
                            var key = marketerStr.ToLowerInvariant();
                            if (!companyDict.TryGetValue(key, out var id))
                            {
                                var newComp = new Company { Name = marketerStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Companies.Add(newComp);
                                await _db.SaveChangesAsync();
                                id = newComp.Id;
                                companyDict[key] = id;
                            }
                            marketerId = id;
                        }

                        int? ackThroughId = null;
                        if (!string.IsNullOrEmpty(ackThroughStr))
                        {
                            var key = ackThroughStr.ToLowerInvariant();
                            if (!ackThroughDict.TryGetValue(key, out var id))
                            {
                                var newAck = new AckThrough { Name = ackThroughStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.AckThroughs.Add(newAck);
                                await _db.SaveChangesAsync();
                                id = newAck.Id;
                                ackThroughDict[key] = id;
                            }
                            ackThroughId = id;
                        }

                        int? unitId = null;
                        if (!string.IsNullOrEmpty(unitStr))
                        {
                            var key = unitStr.ToLowerInvariant();
                            if (!unitDict.TryGetValue(key, out var id))
                            {
                                var newU = new Unit { Name = unitStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Units.Add(newU);
                                await _db.SaveChangesAsync();
                                id = newU.Id;
                                unitDict[key] = id;
                            }
                            unitId = id;
                        }

                        int? statusId = null;
                        if (!string.IsNullOrEmpty(statusStr))
                        {
                            var key = statusStr.ToLowerInvariant();
                            if (!statusDict.TryGetValue(key, out var id))
                            {
                                var newS = new Status { Name = statusStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Statuses.Add(newS);
                                await _db.SaveChangesAsync();
                                id = newS.Id;
                                statusDict[key] = id;
                            }
                            statusId = id;
                        }

                        DateTime.TryParse(GetCell(row, "invoicedate"), out var invDate);
                        DateTime.TryParse(GetCell(row, "entrydate"), out var entDate);
                        DateTime.TryParse(GetCell(row, "lockdate"), out var lckDate);
                        DateTime.TryParse(GetCell(row, "retailerreceiptdate"), out var rrDate);

                        decimal.TryParse(GetCell(row, "quantity"), out var qty);
                        decimal.TryParse(GetCell(row, "quantity(mt)"), out var qtymt);
                        decimal.TryParse(GetCell(row, "receivedquantity"), out var recvQty);
                        decimal.TryParse(GetCell(row, "month1qty"), out var m1qty);
                        decimal.TryParse(GetCell(row, "month2qty"), out var m2qty);
                        decimal.TryParse(GetCell(row, "lorrycapacity"), out var lorryCap);

                        var salesCompany = new SalesCompanySale
                        {
                            TransactionId = GetCell(row, "transactionid"),
                            InvoiceNo = GetCell(row, "invoiceno"),
                            InvoiceDate = invDate == default ? null : invDate.ToUniversalTime(),
                            MarketerId = marketerId,
                            ManufacturerId = companyId,
                            PlantId = plantId,
                            DealerName = agencyNameStr,
                            DealerTypeId = dealerTypeId,
                            DealershipNatureId = natureId,
                            MobileNo = GetCell(row, "mobileno"),
                            DealerRegistrationId = dealerRegistrationId,
                            IfmsDealerId = ifmsDealerId,
                            StateId = stateId,
                            DistrictId = districtId,
                            ProductId = productId,
                            UnitId = unitId,
                            Quantity = qty,
                            QuantityMT = qtymt,
                            ReceivedQuantity = recvQty,
                            StatusId = statusId,
                            EntryDate = entDate == default ? null : entDate.ToUniversalTime(),
                            LockDate = lckDate == default ? null : lckDate.ToUniversalTime(),
                            AckThroughId = ackThroughId,
                            TxnRemark = GetCell(row, "txnremark"),
                            SubsidyMonth1 = GetCell(row, "subsidymonth1"),
                            SubsidyYear1 = GetCell(row, "subsidyyear1"),
                            Month1Qty = m1qty,
                            SubsidyMonth2 = GetCell(row, "subsidymonth2"),
                            SubsidyYear2 = GetCell(row, "subsidyyear2"),
                            Month2Qty = m2qty,
                            ChallanNo = GetCell(row, "challanno."),
                            DdNo = GetCell(row, "ddno."),
                            LorryNo = GetCell(row, "lorryno."),
                            LorryCapacity = lorryCap,
                            RetailerReceiptDate = rrDate == default ? null : rrDate.ToUniversalTime(),
                            CreatedAt = now,
                            UpdatedAt = now,
                            UpdatedBy = currentUserId
                        };

                        _db.SalesCompanySales.Add(salesCompany);
                        result.RowsInserted++;
                    }
                    else if (categoryId == "Six")
                    {
                        if (string.Equals(stateStr, "total", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip the Total row
                        }

                        if (!string.IsNullOrEmpty(globalPlantStr))
                        {
                            var key = globalPlantStr.ToLowerInvariant();
                            if (!plantDict.TryGetValue(key, out var id))
                            {
                                var newPlant = new Plant { Name = globalPlantStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Plants.Add(newPlant);
                                await _db.SaveChangesAsync();
                                id = newPlant.Id;
                                plantDict[key] = id;
                                result.NewMastersCreated.Plants++;
                            }
                            plantId = id;
                        }

                        if (!string.IsNullOrEmpty(globalProductStr))
                        {
                            var key = globalProductStr.ToLowerInvariant();
                            if (!productDict.TryGetValue(key, out var id))
                            {
                                var newProd = new Product { Name = globalProductStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Products.Add(newProd);
                                await _db.SaveChangesAsync();
                                id = newProd.Id;
                                productDict[key] = id;
                                result.NewMastersCreated.Products++;
                            }
                            productId = id;
                        }

                        if (string.Equals(stateStr, "plant", StringComparison.OrdinalIgnoreCase))
                        {
                            // Keep stateId as null for "PLANT" row
                        }
                        else if (!string.IsNullOrEmpty(stateStr))
                        {
                            var stateKey = stateStr.ToLowerInvariant();
                            if (!stateDict.TryGetValue(stateKey, out var id))
                            {
                                var newState = new State { StateName = stateStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.States.Add(newState);
                                await _db.SaveChangesAsync();
                                id = newState.Id;
                                stateDict[stateKey] = id;
                                result.NewMastersCreated.States++;
                            }
                            stateId = id;
                        }

                        decimal.TryParse(GetCell(row, "openingstock"), out var openingStock);
                        decimal.TryParse(GetCell(row, "openinggit"), out var openingGit);
                        decimal.TryParse(GetCell(row, "production/imports"), out var production);
                        decimal.TryParse(GetCell(row, "receipt"), out var receipt);
                        decimal.TryParse(GetCell(row, "dispatches"), out var dispatches);
                        decimal.TryParse(GetCell(row, "sales"), out var sales);
                        decimal.TryParse(GetCell(row, "salesreturn"), out var salesReturn);
                        decimal.TryParse(GetCell(row, "stockadjustment"), out var stockAdj);
                        decimal.TryParse(GetCell(row, "closinggit"), out var closingGit);
                        decimal.TryParse(GetCell(row, "closingstock"), out var closingStock);

                        var recon = new StateGlobalStockReconciliation
                        {
                            PlantId = plantId,
                            ProductId = productId,
                            StateId = stateId,
                            OpeningStock = openingStock,
                            OpeningGIT = openingGit,
                            ProductionImports = production,
                            Receipt = receipt,
                            Dispatches = dispatches,
                            Sales = sales,
                            SalesReturn = salesReturn,
                            StockAdjustment = stockAdj,
                            ClosingGIT = closingGit,
                            ClosingStock = closingStock,
                            CreatedAt = now,
                            UpdatedAt = now,
                            UpdatedBy = currentUserId
                        };

                        _db.StateGlobalStockReconciliations.Add(recon);
                        result.RowsInserted++;
                    }
                    else if (categoryId == "Seven")
                    {
                        if (string.Equals(stateStr, "total", StringComparison.OrdinalIgnoreCase) ||
                            stateStr.EndsWith("-total", StringComparison.OrdinalIgnoreCase) ||
                            stateStr.EndsWith(" total", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(stateStr, "grand total", StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // Skip the Total rows
                        }

                        if (!string.IsNullOrEmpty(globalPlantStr))
                        {
                            var key = globalPlantStr.ToLowerInvariant();
                            if (!plantDict.TryGetValue(key, out var id))
                            {
                                var newPlant = new Plant { Name = globalPlantStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Plants.Add(newPlant);
                                await _db.SaveChangesAsync();
                                id = newPlant.Id;
                                plantDict[key] = id;
                                result.NewMastersCreated.Plants++;
                            }
                            plantId = id;
                        }

                        if (!string.IsNullOrEmpty(globalProductStr))
                        {
                            var key = globalProductStr.ToLowerInvariant();
                            if (!productDict.TryGetValue(key, out var id))
                            {
                                var newProd = new Product { Name = globalProductStr, CategoryId = 1, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Products.Add(newProd);
                                await _db.SaveChangesAsync();
                                id = newProd.Id;
                                productDict[key] = id;
                                result.NewMastersCreated.Products++;
                            }
                            productId = id;
                        }

                        if (stateStr.Trim().Equals("plant", StringComparison.OrdinalIgnoreCase))
                        {
                            stateId = null;
                        }
                        else if (!string.IsNullOrEmpty(stateStr))
                        {
                            var stateKey = stateStr.ToLowerInvariant();
                            if (!stateDict.TryGetValue(stateKey, out var id))
                            {
                                var newState = new State { StateName = stateStr, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.States.Add(newState);
                                await _db.SaveChangesAsync();
                                id = newState.Id;
                                stateDict[stateKey] = id;
                                result.NewMastersCreated.States++;
                            }
                            stateId = id;
                        }
                        
                        int? distId = null;
                        if (!string.IsNullOrEmpty(districtStr))
                        {
                            // Wait, districtDict is keyed by {districtName}_{stateId}
                            // I need to use the resolved stateId
                            var distKey = $"{districtStr.ToLowerInvariant()}_{stateId}";
                            if (!districtDict.TryGetValue(distKey, out var id))
                            {
                                var newDist = new District { DistrictName = districtStr, StateId = stateId ?? 0, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Districts.Add(newDist);
                                await _db.SaveChangesAsync();
                                id = newDist.Id;
                                districtDict[distKey] = id;
                                result.NewMastersCreated.Districts++;
                            }
                            distId = id;
                        }

                        var warehouseStr = GetCell(row, "warehouse/location");
                        int? whseId = null;
                        if (!string.IsNullOrEmpty(warehouseStr))
                        {
                            var whKey = warehouseStr.ToLowerInvariant();
                            if (!warehouseDict.TryGetValue(whKey, out var id))
                            {
                                var newWhse = new Warehouse { Name = warehouseStr, WarehouseCode = string.Empty, IsActive = true, CreatedAt = now, UpdatedAt = now, UpdatedBy = currentUserId };
                                _db.Warehouses.Add(newWhse);
                                await _db.SaveChangesAsync();
                                id = newWhse.Id;
                                warehouseDict[whKey] = id;
                            }
                            whseId = id;
                        }

                        decimal.TryParse(GetCell(row, "openingstock(atlocation)"), out var openingStockLoc);
                        decimal.TryParse(GetCell(row, "openingstock(git)"), out var openingGit);
                        decimal.TryParse(GetCell(row, "imports/production"), out var production);
                        decimal.TryParse(GetCell(row, "receipt"), out var receipt);
                        decimal.TryParse(GetCell(row, "dispatches"), out var dispatches);
                        decimal.TryParse(GetCell(row, "sales"), out var sales);
                        decimal.TryParse(GetCell(row, "salesreturn"), out var salesReturn);
                        decimal.TryParse(GetCell(row, "stockadjustment"), out var stockAdj);
                        decimal.TryParse(GetCell(row, "closinggit"), out var closingGit);
                        decimal.TryParse(GetCell(row, "closingstock"), out var closingStock);

                        var recon = new WarehouseDistrictGlobalStockReconciliation
                        {
                            PlantId = plantId,
                            ProductId = productId,
                            StateId = stateId,
                            DistrictId = distId,
                            WarehouseId = whseId,
                            OpeningStockAtLocation = openingStockLoc,
                            OpeningStockGIT = openingGit,
                            ProductionImports = production,
                            Receipt = receipt,
                            Dispatches = dispatches,
                            Sales = sales,
                            SalesReturn = salesReturn,
                            StockAdjustment = stockAdj,
                            ClosingGIT = closingGit,
                            ClosingStock = closingStock,
                            CreatedAt = now,
                            UpdatedAt = now,
                            UpdatedBy = currentUserId
                        };

                        _db.WarehouseDistrictGlobalStockReconciliations.Add(recon);
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
