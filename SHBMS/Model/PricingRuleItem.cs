using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SHBMS.Model
{
    public class PricingRuleItem
    {
        public int RuleId { get; set; }
        public string RuleName { get; set; } = "";
        public string RoomType { get; set; } = "";
        public string AdjustmentType { get; set; } = "";
        public decimal AdjustmentValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active";

        public string AdjustmentDisplay =>
            $"{(AdjustmentType == "Increase" ? "+" : "-")}{AdjustmentValue}%";
    }
}