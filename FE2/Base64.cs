using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE2
{
    public static class Base64
    {
        public static string Encode(string ToEncode)
        {
            byte[] ToEncodeAsBytes = System.Text.ASCIIEncoding.ASCII.GetBytes(ToEncode);
            string ReturnValue = System.Convert.ToBase64String(ToEncodeAsBytes);
            return ReturnValue;
        }

        static public string Decode(string EncodedData)
        {
            byte[] EncodedDataAsBytes = System.Convert.FromBase64String(EncodedData);
            string ReturnValue = System.Text.ASCIIEncoding.ASCII.GetString(EncodedDataAsBytes);
            return ReturnValue;
        }

    } // end class
}
