// Template: Models (ApiModel.t4) version 3.0

// This code was generated by AMF Server Scaffolder

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace ChinookAspNetCore.ChinookV1.Models
{
    public partial class Customer  : Person
    {
        

        [Required]
        [MaxLength(0)]
        [MinLength(0)]
        public string Company { get; set; }
    } // end class

} // end Models namespace
