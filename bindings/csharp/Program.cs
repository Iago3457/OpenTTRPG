using System.Linq;
using OpenTTRPG.CLI;
using OpenTTRPG.Tests;

namespace OpenTTRPG
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args != null && args.Any(a => a == "--test" || a == "-t"))
            {
                Tormenta20TestSuite.RunAll();
                return;
            }

            var cli = new CLIController();
            cli.Run();
        }
    }
}
