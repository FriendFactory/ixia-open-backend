using System;
using System.ComponentModel.DataAnnotations;

namespace AuthServer.Quickstart.Account
{
    public class AppleSingInInputModel
    {
        [Required] public string Name { get; set; }

        /// <summary>
        /// JWT from Apple
        /// </summary>
        [Required]
        public string IndentityToken { get; set; }

        [Required] public string AuthorizationCode { get; set; }

        [Required] public string Email { get; set; }

        public int? Gender { get; set; }

        [Required, DataType(DataType.Date)] public DateTime BirthDate { get; set; }
        public bool? AllowDataCollection { get; set; }
        public bool AnalyticsEnabled { get; set; }
    }
}