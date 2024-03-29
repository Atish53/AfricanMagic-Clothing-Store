﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AfricanMagicSystem.Models
{
    [Bind(Exclude = "CustomerReviewID")]
    public class CustomerReviews
    {
        [Key]
        public int CustomerReviewID { get; set; }

        [Required(ErrorMessage = "Please enter a valid Sale ID. Find this on your Invoice.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Please use numbers only")]
        public int InvoiceNumber { get; set; }

        public string Username { get; set; }

        public string PartReviewed { get; set; }

        public int Vote { get; set; }

        [Required(ErrorMessage = "Enter a comment.")]
        public string Comment { get; set; }

        public bool? Flagged { get; set; }
    }
}