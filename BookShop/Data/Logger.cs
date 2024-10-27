
namespace BookShop.Data
{
    public static class Logger
    {
        public static void WriteLog(string message)
        {
            string logPath = "logger.txt";

            using StreamWriter logStream = new StreamWriter(logPath);
            logStream.WriteLine($"{DateTime.Now} : {message}");

        }

    }
}
