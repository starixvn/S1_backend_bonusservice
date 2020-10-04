using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BonusService.Service.Library
{
    public class Log2File
    {
        private readonly string LogFolder = "";
        public Log2File(IHostEnvironment environment)
        {
            LogFolder = $@"{environment.ContentRootPath}\Logs";
        }
        public void Info(string InfoContent, string PrefixName)
        {
            var filePath = $@"{LogFolder}\{PrefixName}_Info_{DateTime.Now:yyyyMMdd}.log";
            Task.Run(() =>
            {
                Directory.CreateDirectory(LogFolder);
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss}\r\n  Thread:{Thread.CurrentThread.ManagedThreadId}\r\n{InfoContent}");
            });
        }
        public void Error(string ErrorContent, string PrefixName)
        {
            var filePath = $@"{LogFolder}\{PrefixName}_Err_{DateTime.Now:yyyyMMdd}.log";
            Task.Run(() =>
            {
                Directory.CreateDirectory(LogFolder);
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine($"{DateTime.Now:yyyy/MM/dd HH:mm:ss}\r\n  Thread:{Thread.CurrentThread.ManagedThreadId}\r\n{ErrorContent}");
            });
        }
    }
}
