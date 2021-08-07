using System;

namespace IngameScript
{
    partial class Program
    {
        public class LogManager
        {
            public double Distance {get;set;}            
            public Action<string> SendLogs;
            
            private LimitedQueue Logs;
            
            public LogManager(Action<string> sendLogs)
            {
                SendLogs = sendLogs;
                Logs = new LimitedQueue(11);
            }

            public void Add(string log)
            {
                Logs.Enqueue(log);
            }

            public void Echo(Steps currentStep)
            {
                SendLogs($"Distance: {Distance}m{Environment.NewLine}{Logs.Dump()}");
            }
        }
    }
}
