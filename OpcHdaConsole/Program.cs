using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace OpcHdaConsole
{
    class Program
    {
        public static SQLite sqlite = new SQLite("SQLite/DataBase.sqlite");

        private static List<Client> clients = new List<Client>();
        private static string path_to_logs = Environment.CurrentDirectory + "\\Logs";
        private static System.Timers.Timer timer_init;

        static void Main(string[] args)
        {
            sqlite.Execute(@"CREATE TABLE IF NOT EXISTS server (
                                id   INTEGER PRIMARY KEY AUTOINCREMENT,
                                name VARCHAR UNIQUE
                                             NOT NULL
                            );");

            sqlite.Execute(@"CREATE TABLE IF NOT EXISTS [group] (
                                id        INTEGER PRIMARY KEY AUTOINCREMENT,
                                name      VARCHAR,
                                server_id INTEGER REFERENCES server (id) ON DELETE CASCADE,
                                period    INTEGER NOT NULL
                                                  DEFAULT (3600),
                                interval  INTEGER NOT NULL
                                                  DEFAULT (300),
                                data_pack INTEGER NOT NULL
                                                  DEFAULT (1),
                                retry     INTEGER NOT NULL
                                                  DEFAULT (2) 
                            );");

            sqlite.Execute(@"CREATE TABLE IF NOT EXISTS parameter (
                                id         INTEGER  PRIMARY KEY AUTOINCREMENT,
                                group_id   INTEGER  REFERENCES [group] (id) ON DELETE CASCADE,
                                name       STRING   NOT NULL,
                                time_start DATETIME NOT NULL
                                                    DEFAULT (strftime('%Y-%m-01 00:00:00.000', 'now', 'localtime') ) 
                            );");

            sqlite.Execute(@"CREATE TABLE IF NOT EXISTS data (
                                parameter_id INTEGER  REFERENCES parameter (id) ON DELETE CASCADE,
                                time         DATETIME NOT NULL,
                                value        DOUBLE   NOT NULL
                            );");

            sqlite.Execute(@"CREATE UNIQUE INDEX IF NOT EXISTS group_unique ON [group] (
                                server_id,
                                name
                            );");

            sqlite.Execute(@"CREATE UNIQUE INDEX IF NOT EXISTS parameter_unique ON parameter (
                                group_id,
                                name
                            );");

            sqlite.Execute(@"CREATE UNIQUE INDEX IF NOT EXISTS data_unique ON data (
                                parameter_id,
                                time
                            );");

            sqlite.Execute(@"CREATE VIEW IF NOT EXISTS parameter_view AS
                                SELECT id,
                                       group_id,
                                       name,
                                       max(time_start, (
                                               SELECT IFNULL(MAX(time), 0) 
                                                 FROM data
                                                WHERE parameter_id = id
                                           )
                                       ) AS time_last
                                  FROM parameter;");

            List<string> server_names = Program.sqlite.SelectColumn<string>("SELECT name FROM server");
            for (int i = 0; i < server_names.Count; i++)
            {
                clients.Add(new Client(server_names[i]));
                clients[i].Start();
            }

            timer_init = GetTimer((args.Length == 0 ? 60000 : Convert.ToInt32(args[0])), OnTimerInit);
            OnTimerInit(null, null);
            
            while (true)
                Thread.Sleep(60000);
        }

        private static System.Timers.Timer GetTimer(double interval, ElapsedEventHandler OnFunc)
        {
            System.Timers.Timer timer = new System.Timers.Timer(interval);
            timer.Elapsed += OnFunc;
            timer.AutoReset = true;
            timer.Enabled = true;
            return timer;
        }
        private static void OnTimerInit(Object source, ElapsedEventArgs e)
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Init();
        }

        static public void Log(string message)
        {
            try
            {
                message = DateTime.Now.ToString() + " | " + message;
                // Запись в консоль
                Console.WriteLine(message);

                // Запись в файл
                string filename = DateTime.Now.Year.ToString() +
                    (DateTime.Now.Month < 10 ? "0" + DateTime.Now.Month.ToString() : DateTime.Now.Month.ToString()) +
                    (DateTime.Now.Day < 10 ? "0" + DateTime.Now.Day.ToString() : DateTime.Now.Day.ToString());

                if (!Directory.Exists(path_to_logs))
                    Directory.CreateDirectory(path_to_logs);

                using (FileStream fs = File.OpenWrite(path_to_logs + "\\" + filename + ".log"))
                {
                    Byte[] data = new UTF8Encoding(true).GetBytes(message + "\r\n");

                    fs.Seek(0, SeekOrigin.End);
                    fs.Write(data, 0, data.Length);
                }
            }
            catch
            {

            }
        }
        static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("Newtonsoft.Json"))
                return Assembly.Load(OpcHdaConsole.Properties.Resources.Newtonsoft_Json);

            if (args.Name.Contains("System.Data.SQLite"))
                return Assembly.Load(OpcHdaConsole.Properties.Resources.System_Data_SQLite);

            if (args.Name.Contains("SQLite.Interop"))
                return Assembly.Load(OpcHdaConsole.Properties.Resources.SQLite_Interop);

            return null;
        }
    }
}
