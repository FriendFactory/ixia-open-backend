namespace Common.Models;

public static class ErrorCodes
{
    public const string ModerationError = "MODERATION_ERROR";

    public static class Auth
    {
        public const string AccountAlreadyExist = "ERROR_ACCOUNT_EXIST";
        public const string AccountBlocked = "ERROR_ACCOUNT_BLOCKED";
        public const string AccountHasLoginMethod = "ERROR_ACCOUNT_LOGIN_METHOD_EXIST";
        public const string AccountInsufficientPermissions = "ERROR_ACCOUNT_INSUFFICIENT_PERMISSIONS";
        public const string AccountLastLoginMethod = "ERROR_ACCOUNT_LAST_LOGIN_METHOD";
        public const string AccountNotExist = "ERROR_ACCOUNT_NOT_EXIST";
        public const string AccountRegistrationTooManyRequests = "ERROR_ACCOUNT_REGISTRATION_TOO_MANY_REQUESTS";
        public const string AppleIdAlreadyUsed = "ERROR_APPLE_ID_USED";
        public const string AppleTokenInvalid = "ERROR_APPLE_IDENTITY_TOKEN_INVALID";
        public const string EmailAlreadyUsed = "ERROR_EMAIL_USED";
        public const string EmailInvalid = "ERROR_EMAIL_INVALID";
        public const string GroupNotFound = "ERROR_GROUP_NOT_FOUND";
        public const string GroupNotMinor = "ERROR_GROUP_NOT_MINOR";
        public const string GoogleIdAlreadyUsed = "ERROR_GOOGLE_ID_USED";
        public const string GoogleTokenInvalid = "ERROR_GOOGLE_IDENTITY_TOKEN_INVALID";
        public const string MinorCredentialsInvalid = "ERROR_MINOR_CREDENTIALS_INVALID";
        public const string PasswordEmpty = "ERROR_PASSWORD_EMPTY";
        public const string PasswordInvalid = "ERROR_PASSWORD_INVALID";
        public const string PasswordMatchesUsername = "ERROR_PASSWORD_MATCHES_USERNAME";
        public const string PasswordMinLenght = "ERROR_PASSWORD_MIN_LENGTH";
        public const string PasswordOrTokenRequired = "ERROR_PASSWORD_OR_TOKEN_REQUIRED";
        public const string PasswordTooSimple = "ERROR_PASSWORD_TOO_SIMPLE";
        public const string PasswordAlreadyExist = "ERROR_PASSWORD_EXIST";
        public const string PhoneNumberAlreadyUsed = "ERROR_PHONE_NUMBER_USED";
        public const string PhoneNumberInvalid = "ERROR_PHONE_NUMBER_INVALID";
        public const string PhoneNumberFormatInvalid = "ERROR_PHONE_NUMBER_FORMAT_INVALID";
        public const string PhoneNumberRetryNotAvailable = "ERROR_PHONE_NUMBER_RETRY_NOT_AVAILABLE";
        public const string PhoneNumberTooManyRequests = "ERROR_PHONE_NUMBER_TOO_MANY_REQUESTS";
        public const string PhoneNumberVerificationError = "ERROR_PHONE_NUMBER_VERIFICATION";
        public const string VerificationCodeInvalid = "ERROR_VERIFICATION_CODE_INVALID";
        public const string VerificationCodeEmpty = "ERROR_VERIFICATION_CODE_EMPTY";
        public const string VerificationTokenInvalid = "ERROR_VERIFICATION_TOKEN_INVALID";
        public const string UserNotFound = "ERROR_USER_NOT_FOUND";
        public const string UserWithEmailNotFound = "ERROR_USER_WITH_EMAIL_NOT_FOUND";
        public const string UserNameAlreadyUsed = "ERROR_USERNAME_USED";
        public const string UserNameEmpty = "ERROR_USERNAME_EMPTY";
        public const string UserNameContainsInvalidSymbols = "ERROR_USERNAME_CONTAINS_INVALID_SYMBOLS";
        public const string UserNameLengthInvalid = "ERROR_USERNAME_LENGTH_INVALID";
        public const string UsernameModerationFailed = "ERROR_USERNAME_MODERATION_FAILED";
        public const string UsernameUpdateLimit = "ERROR_USERNAME_UPDATE_LIMIT";
    }

    public static class Client
    {
        public const string ArtStyleNotFound = "ERROR_ARTSTYLE_NOT_FOUND";
        public const string CharacterNotFound = "ERROR_CHARACTER_NOT_FOUND";
        public const string GenderNotFound = "ERROR_GENDER_NOT_FOUND";
        public const string GroupNotFound = "ERROR_GROUP_NOT_FOUND";
        public const string PurchaseNotEnoughCurrency = "ERROR_PURCHASE_NOT_ENOUGH_CURRENCY";
    }

    public static class Video
    {
        public const string VideoNotFound = "ERROR_VIDEO_NOT_FOUND";
    }
}