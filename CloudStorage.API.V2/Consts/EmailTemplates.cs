namespace CloudStorage.API.V2.Consts
{
    public class EmailTemplates
    {
        public const string AccountActivation =
@"
<html>
    <body>
        <p><a href='https://localhost:44318/api/account/activate/{0}'>Activate</a></p>
    </body>
</html>
";
    }
}
