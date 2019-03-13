using System.Runtime.InteropServices;

namespace RadiusYandex.Models
{
    public class Internet
    {
        //Creating the extern function...
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);

        //Creating a function that uses the API function...
        public static bool IsConnectedToInternet()
        {
            return InternetGetConnectedState(out int Desc, 0);
        }
    }
}