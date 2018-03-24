using System;
using System.Collections.Generic;
using System.Text;

namespace Fiasm.Core.Models.RequestModels
{
    public class RequestModel
    {
        public string Details { get; set; }
    }

    public class ResponseModel
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}
