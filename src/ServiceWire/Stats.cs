using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
#if NET462
using System.Management;
#endif
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceWire
{
    public class Stats : LoggerBase, IStats
    {
        private int _statsBufferSize = 10000;
        private const string StatFilePrefixDefault = "stat-";
        private const string StatFileExtensionDefault = ".txt";

        public Stats(string statsDirectory = null,
            string statsFilePrefix = null,
            string statsFileExtension = null,
            int messageBufferSize = 32,
            int statsBufferSize = 10000,
            LogOptions options = LogOptions.LogOnlyToFile,
            LogRollOptions rollOptions = LogRollOptions.Daily,
            int rollMaxMegaBytes = 1024,
            bool useUtcTimeStamp = false)
        {
#if NET462
            _logDirectory = statsDirectory ?? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");
#else
            _logDirectory = statsDirectory ?? Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "logs");
#endif
            Directory.CreateDirectory(_logDirectory); //will throw if unable - does not throw if already exists
            _logFilePrefix = statsFilePrefix ?? StatFilePrefixDefault;
            _logFileExtension = statsFileExtension ?? StatFileExtensionDefault;
            _messageBufferSize = messageBufferSize;
            _statsBufferSize = statsBufferSize;
            _rollOptions = rollOptions;
            _rollMaxMegaBytes = rollMaxMegaBytes;
            _useUtcTimeStamp = useUtcTimeStamp;

            LogOptions = options;
            if (_messageBufferSize < 1) _messageBufferSize = 1;
            if (_messageBufferSize > 4096) _messageBufferSize = 4096;
            if (_statsBufferSize < 10) _statsBufferSize = 10;
            if (_statsBufferSize > 1000000) _statsBufferSize = 1000000;
            if (_rollOptions == LogRollOptions.Size)
            {
                if (_rollMaxMegaBytes < 1) _rollMaxMegaBytes = 1;
                if (_rollMaxMegaBytes < 4096) _rollMaxMegaBytes = 4096;
            }
        }

        private void WriteLines(string[] lines)
        {
            _logQueue.Enqueue(lines);
            if (_logQueue.Count >= _messageBufferSize)
                Task.Factory.StartNew(() => WriteBuffer(_messageBufferSize));
        }

        public override void FlushLog()
        {
            var items = new List<string[]>();
            foreach (var kvp in _bag)
            {
                items.Add(kvp.Value.GetDump());
            }
            foreach(var list in items) WriteLines(list);
            base.FlushLog();
        }

        //period, cat, nm, count, total
        private int _period = 0;
        private ConcurrentDictionary<int, StatsBag> _bag = new ConcurrentDictionary<int, StatsBag>();

        private void WriteBag(StatsBag bag)
        {
            int p = _period;
            Interlocked.Increment(ref _period);
            var lines = bag.GetDump();
            StatsBag b;
            if (_bag.TryRemove(p, out b)) b.Clear();
            WriteLines(lines);
        }

        public void Log(string name, float value)
        {
            int p = _period;
            var bag = _bag.GetOrAdd(p, new StatsBag(_useUtcTimeStamp));
            bag.Add("unspecified", name, value);
            if (bag.Count >= _statsBufferSize)
            {
                WriteBag(bag);
            }
        }

        public void Log(string category, string name, float value)
        {
            int p = _period;
            var bag = _bag.GetOrAdd(p, new StatsBag(_useUtcTimeStamp));
            bag.Add(category, name, value);
            if (bag.Count >= _statsBufferSize)
            {
                WriteBag(bag);
            }
        }

        private const string SysCategory = "_sys";
        public void LogSys()
        {
            var md = GetSystemMemory();
            int p = _period;
            var bag = _bag.GetOrAdd(p, new StatsBag(_useUtcTimeStamp));
            bag.Add(SysCategory, "TotalVisibleMemoryMB", md.TotalVisibleMemorySize / 1024f / 1024f);
            bag.Add(SysCategory, "FreePhysicalMemoryMB", md.FreePhysicalMemory / 1024f / 1024f);
            if (bag.Count >= _statsBufferSize)
            {
                WriteBag(bag);
            }
        }

        private const string WinObjQuery = "SELECT * FROM CIM_OperatingSystem";
        private const string TotalVisibleMemorySize = "TotalVisibleMemorySize";
        private const string TotalVirtualMemorySize = "TotalVirtualMemorySize";
        private const string FreePhysicalMemory = "FreePhysicalMemory";
        private const string FreeVirtualMemory = "FreeVirtualMemory";

        private MemoryDetail GetSystemMemory()
        {
#if NET462
            var winQuery = new ObjectQuery(WinObjQuery);
            var searcher = new ManagementObjectSearcher(winQuery);
            foreach (ManagementObject item in searcher.Get())
            {
                var totalVisibleMemorySize = Convert.ToUInt64(item[TotalVisibleMemorySize]);
                var totalVirtualMemorySize = Convert.ToUInt64(item[TotalVirtualMemorySize]);
                var freePhysicalMemory = Convert.ToUInt64(item[FreePhysicalMemory]);
                var freeVirtualMemory = Convert.ToUInt64(item[FreeVirtualMemory]);
                var result = new MemoryDetail()
                {
                    TotalVisibleMemorySize = totalVisibleMemorySize,
                    TotalVirtualMemorySize = totalVirtualMemorySize,
                    FreePhysicalMemory = freePhysicalMemory,
                    FreeVirtualMemory = freeVirtualMemory
                };
                return result;
            }
            return new MemoryDetail();
#else
            return null;
#endif
        }

    }
}