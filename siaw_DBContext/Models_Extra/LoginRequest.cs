using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace siaw_DBContext.Models
{
    public class LoginRequest
    {
        public string login { get; set; }
        public string password { get; set; }
        public string conDatDesc { get; set; }
        
    }
}