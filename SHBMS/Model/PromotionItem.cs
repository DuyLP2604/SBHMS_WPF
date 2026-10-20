using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SHBMS.Model
{
    public class PromotionItem
    {
        public int PromotionId { get; set; }
        public string PromotionName { get; set; } = "";
        public string Code { get; set; } = "";
        public string DiscountType { get; set; } = "";
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int UsageLimit { get; set; }
        public string Status { get; set; } = "Active";

        public string DiscountDisplay =>
            DiscountType == "Percentage"
                ? $"{DiscountValue}%"
                : $"{DiscountValue:N0} VND";
    }
}