using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace D200.Models
{
    public class Response
    {
        public bool IsSuccess { get; set; }
        public object Data { get; set; }
        public string Msg { get; set; }
        public string ErrMsg { get; set; }
    }
}
