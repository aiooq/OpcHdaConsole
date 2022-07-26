using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OpcHdaConsole
{
    public class Client
    {
        // public
        public Server server;
        public string server_name;

        // private
        private bool is_started = false;
        private bool is_connected = false;
        private List<Group> groups = new List<Group>();

        public Client()
        {

        }
        ~Client()
        {
            Stop();
        }
        public Client(string server_name = "Logika.HDA.2")
        {
            this.server_name = server_name;
            server = new Server(server_name);
            Program.Log("[Created] Client for " + server_name);
        }
        public void Init()
        {
            string query = @"SELECT id, name, period, interval, data_pack, retry
                            FROM [group]
                            WHERE server_id=(SELECT id FROM server WHERE name='@server_name')".Replace("@server_name", server_name);

            Table table = Program.sqlite.Select(query);

            for (int i = 0; i < table.rc.Count; i++)
            {
                Group group = groups.Find(x => x.id == Convert.ToInt64(table.rc[i][0]));
                if (group == null)
                {
                    groups.Add(new Group(
                        Convert.ToInt64(table.rc[i][0]),
                        Convert.ToString(table.rc[i][1]),
                        Convert.ToInt64(table.rc[i][2]),
                        Convert.ToInt64(table.rc[i][3]),
                        Convert.ToInt64(table.rc[i][4]),
                        Convert.ToInt64(table.rc[i][5])));
                }
                else
                {
                    group.name = Convert.ToString(table.rc[i][1]);
                    group.period = Convert.ToInt64(table.rc[i][2]);
                    group.interval = Convert.ToInt64(table.rc[i][3]);
                    group.data_pack = Convert.ToInt64(table.rc[i][4]);
                    group.retry = Convert.ToInt64(table.rc[i][5]);
                }
            }

            for (int i = 0; i < groups.Count; i++)
                if (groups[i].Init(this) && is_started)
                    groups[i].Start();
        }
        public bool Start()
        {
            if (is_started)
                return true;

            Thread thread = new Thread(Starting);
            thread.IsBackground = false;
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
            is_started = true;
            return true;
        }
        public void Stop()
        {
            Thread thread = new Thread(Stoping);
            thread.IsBackground = false;
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }
        public bool IsConnected()
        {
            return is_connected;
        }

        private void Starting()
        {
            Disconnecting();
            Connecting();

            for (int i = 0; i < groups.Count; i++)
                groups[i].Start();
        }
        private void Stoping()
        {
            for (int i = 0; i < groups.Count; i++)
                groups[i].Stop();

            Disconnecting();
        }

        private bool Reconnecting()
        {
            Disconnecting();
            return Connecting();
        }
        private bool Connecting()
        {
            if (is_connected)
                return is_connected;

            if (server == null)
                return false;

            server.Attach();
            return is_connected = true;
        }
        private void Disconnecting()
        {
            if (server == null)
                return;

            server.Detach();
            is_connected = false;
        }
    }
}
