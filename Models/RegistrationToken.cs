﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
namespace TCTE.Models
{
    public class RegistrationToken
    {
        public int Id { get; set; }
        [Required]
        public string Token { get; set; }
        [Required, Display(Name = "设备来源")]
        public string Category { get; set; }
    }
}