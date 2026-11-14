using System;
using System.Collections.Generic;
using System.Text;

namespace SPIC.Core.Entities
{
    public sealed class SalesAuditMatchedInvoice
    {
        public long Id { get; set; }
        public int CSR2Id { get; set; }
        public long SourceSaleId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
