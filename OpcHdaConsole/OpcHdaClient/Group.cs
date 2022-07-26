using System;
using System.Timers;
using System.Collections.Generic;

namespace OpcHdaConsole
{
    public class Group
    {
        public Client client;
        public long id;
        public string name;
        public long retry;
        public long period;
        public long interval;
        public long data_pack;

        private List<Parameter> parameters = new List<Parameter>();
        
        private System.Timers.Timer timer;
        private readonly object timer_lock = new object();

        public Group(long id, string name, long period, long interval, long data_pack, long retry)
        {
            this.id = id;
            this.name = name;
            this.retry = retry;
            this.period = period;
            this.interval = interval;
            this.data_pack = data_pack;
            SetTimer(1000);
        }
        ~Group()
        {
            Stop();
        }

        public void Start()
        {
            if (timer == null)
                SetTimer(1000);

            if (IsActive())
                return;

            timer.Start();
            OnTimedEvent(null, null);
        }
        public void Stop()
        {
            timer.Stop();
            timer.Close();
            timer.Dispose();
            timer = null;
        }
        public bool Init(Client client)
        {
            if (client == null)
                return false;

            this.client = client;

            string query = String.Format(@"SELECT id,name,time_last
                                          FROM parameter_view
                                          WHERE group_id={0};", id);

            Table table = Program.sqlite.Select(query);

            for (int i = 0; i < table.rc.Count; i++)
            {
                Parameter parameter = parameters.Find(x => x.id == Convert.ToInt64(table.rc[i][0]));
                if (parameter == null)
                {
                    parameters.Add(new Parameter(this,
                        Convert.ToInt64(table.rc[i][0]),
                        Convert.ToString(table.rc[i][1]),
                        Convert.ToString(table.rc[i][2]),
                        new TimeSpan(period * data_pack * 10000000),
                        new TimeSpan((period + interval) * 10000000),
                        retry));
                }
                else
                {
                    parameter.name = Convert.ToString(table.rc[i][1]);
                    parameter.time_data_step = new TimeSpan(period * data_pack * 10000000);
                    parameter.time_request_step = new TimeSpan((period + interval) * 10000000);
                    parameter.retry = retry;
                }
            }

            return true;
        }

        private bool IsActive()
        {
            return timer.Enabled;
        }
        private void SetTimer(int msec)
        {
            timer = new System.Timers.Timer(msec);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            if (!client.IsConnected())
                return;

            Task();
        }
        private void Task()
        {
            lock (timer_lock)
            {
                for (int i = 0; i < parameters.Count; i++)
                    parameters[i].Get();
            }
        }
    }
}
