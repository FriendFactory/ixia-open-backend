using System;
using System.ComponentModel.DataAnnotations;
using AuthServer.Services.PhoneNumberAuth;

namespace AuthServer.Quickstart.Account
{
    public class RegisterUserViewModel
    {
        [MaxLength(256)] [DataType(DataType.EmailAddress)] public string Email { get; set; }
        [RegularExpression(VerifyPhoneNumberRequestValidator.PhoneNumberRegex)] public string PhoneNumber { get; set; }
        public string VerificationCode { get; set; }
        [DataType(DataType.Password)] public string Password { get; set; }
        public string AppleId { get; set; }

        //TODO: we can drop in 1.9 version and use IdentityToken for both Apple and Google
        public string AppleIdentityToken { get; set; }
        public string GoogleId { get; set; }
        public string IdentityToken { get; set; }
        public string UserName { get; set; }
        [Required, DataType(DataType.Date)] public DateTime BirthDate { get; set; }
        public bool? AllowDataCollection { get; set; }
        public bool AnalyticsEnabled { get; set; }
        [MaxLength(3)] public string DefaultLanguage { get; set; }
        [MaxLength(3)] public string Country { get; set; }

        public void Validate()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Country);
            ArgumentException.ThrowIfNullOrWhiteSpace(DefaultLanguage);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(BirthDate, DateTime.UtcNow, nameof(BirthDate));
            ArgumentOutOfRangeException.ThrowIfLessThan(BirthDate, DateTime.UtcNow.AddYears(-150), nameof(BirthDate));
        }
    }
}