namespace CloudStorage.SPA
{
    public class Worker
    {
        public static IDictionary<string, string> GetQueryParameters(string pRelativeUri)
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            int queryStart = pRelativeUri.IndexOf('?') + 1;
            if (queryStart != -1)
            {
                string pQueries = pRelativeUri.Substring(queryStart);
                string[] nameAndValues = pQueries.Split('&');

                foreach(string n  in nameAndValues)
                {
                    string[] nameValue = n.Split("=");
                    if (nameValue.Length == 2)
                    {
                        parameters.Add(nameValue[0], nameValue[1]);
                    }
                }
            }

            return parameters;
        }
    }
}
