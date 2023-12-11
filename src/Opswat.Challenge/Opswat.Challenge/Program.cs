using System.Text.Json;

namespace Opswat.Challenge
{
    internal class Program
    {
        private const string BaseApiUri = "https://api.metadefender.com/v4";
        private const string GetByDataIdUri = BaseApiUri + "/file/{0}";
        private const string GetByHashUri = BaseApiUri + "/hash/{0}";
        private const string PostFileUri = BaseApiUri + "/file";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}
