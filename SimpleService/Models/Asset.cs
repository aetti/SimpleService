using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SimpleService.Models
{

    [Serializable]
    public class Asset
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsFixIncome { get; set; }
        public bool IsConvertible { get; set; }
        public bool IsSwap { get; set; }
        public bool IsCash { get; set; }
        public bool IsFuture { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
