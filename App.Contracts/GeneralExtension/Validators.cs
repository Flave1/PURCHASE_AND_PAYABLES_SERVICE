using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Puchase_and_payables.Contracts.GeneralExtension
{
    public static class CustomValidators
    {
        public static bool IsNumeric(string value)
        {
            return value.All(char.IsNumber);
        }
    }

    public class CustomEncoder
    {
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
