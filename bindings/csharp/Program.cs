using OpenTTRPG.CLI;

namespace OpenTTRPG
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var cli = new CLIController();
            cli.Run();
        }
    }
}

