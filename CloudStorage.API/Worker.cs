using CloudStorage.API.Models;
using JB.Common.Errors;
using Newtonsoft.Json;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using CloudStorage.API.Consts;
using CloudStorage.Consts;

namespace CloudStorage.API
{
    public static class Worker
    {
        public static JwtPayload GetJwtPayloadFromBearerToken(string pBearerToken)
        {
            if (string.IsNullOrEmpty(pBearerToken) || !pBearerToken.StartsWith("bearer", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new JBException("No bearer token supplied");
            }

            string bearerToken = pBearerToken.Remove(0, 6).Trim();
            string[] tokenParts = bearerToken.Split('.');

            if (tokenParts.Length <= 1)
            {
                throw new JBException("Unable to get payload");
            }
            string bearerPayload = tokenParts[1];


            int paddingRequired = bearerPayload.Length % 4;
            if (paddingRequired > 0)
            {
                StringBuilder sBuilder = new StringBuilder(bearerPayload);
                switch (paddingRequired)
                {
                    case 0: // No pad characters.
                        break;
                    case 2: // Two pad characters.
                        sBuilder.Append("==");
                        break;
                    case 3: // One pad character.
                        sBuilder.Append("=");
                        break;
                    default:
                        throw new FormatException("Invalid Base64 URL encoding.");
                }
                bearerPayload = sBuilder.ToString();
            }


            string jsonBearerToken = Encoding.UTF8.GetString(Convert.FromBase64String(bearerPayload));

            JwtPayload? jwtPayload = JsonConvert.DeserializeObject<JwtPayload>(jsonBearerToken);

            if (jwtPayload == null)
            {
                throw new JBException("Unable to deserialize JWT");
            }

            return jwtPayload;
        }

        public static string GetContentType(string pFileExtension)
        {
            switch(pFileExtension.ToLower().Trim('.'))
            {
                case "png":
                    return "image/png";
                case "jpg":
                    return "image/jpeg";

                default:
                    return "text/plain";
            } 
        }

        public static string CreatePermissionsList(IList<Permission> pPermissions)
        {
            StringBuilder sb = new StringBuilder();

            for (int p = 0; p < pPermissions.Count; p++) { 
                if (p != 0)
                {
                    sb.Append(',');
                }

                sb.Append((int)pPermissions[p]);
            }

            return sb.ToString();
        }
    }
}
