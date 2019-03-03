using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SquBitServer
{
    class Program
    {
        static void Main(string[] args)
        {
            NetworkClass Network = new NetworkClass();
            if (Network.Start("127.0.0.1", 3726))
            {
                Database.Init("localhost", "user", "pass", "db", Convert.ToInt32(3306));

                Network.AcceptConnectionThread();
                Console.WriteLine("Listening");

            }
            else
            {
                Console.WriteLine("Network.Start() Failed.");
            }
        }

    }
}
