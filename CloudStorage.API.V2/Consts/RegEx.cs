namespace CloudStorage.API.V2.Consts
{
    public class RegEx
    {
        public const string PASSWORD_VALIDATION = "^(?=.+[0-9])(?=.+[a-z])(?=.+[A-Z])(?=.+[@#$%^&+=.\\-_*])(.{8,})$";
        public const string PASSWORD_UPPERCASE_VALIDATION = "(?=.+[A-Z])";
        public const string PASSWORD_LOWERCASE_VALIDATION = "(?=.+[a-z])";
        public const string PASSWORD_SYMBOL_VALIDATION = "(?=.+[@#$%^&+=.\\-_*])";
        public const string PASSWORD_NUMBER_VALIDATION = "(?=.+[0-9])";
    }
}
