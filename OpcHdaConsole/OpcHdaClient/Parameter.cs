using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace OpcHdaConsole
{
    public class Parameter
    {
        // public
        public long id,
                    retry;

        public string name;

        public TimeSpan time_data_step,
                        time_request_step;

        // private
        private bool m_is_in_thread;
        private Thread thread;
        private Group group;
        private DateTime time_request,
                         time_data_start,
                         time_data_end;

        private int count_errors = 0;

        public Parameter()
        {

        }
        public Parameter(Group group, long id, string name, string time_start, TimeSpan time_step_data, TimeSpan time_step_request, long retry)
        {
            this.time_data_start = DateTime.Parse(time_start);
            this.time_data_step = time_step_data;
            this.time_request_step = time_step_request;

            m_is_in_thread = false;
            this.id = id;
            this.name = name;
            this.retry = retry;
            this.group = group;
        }
        ~Parameter()
        {
            Abort();
        }

        private void Abort()
        {
            if (IsAlive())
                thread.Abort();
        }

        public bool IsAlive()
        {
            return thread == null ? false : thread.IsAlive;
        }

        public bool IsTimeRequest()
        {
            return time_request != null &&
                    time_request <= DateTime.Now;
        }

        public void Get()
        {
            if (!IsTimeRequest())
                return;

            if (IsAlive())
                return;

            if (m_is_in_thread)
            {
                thread = new Thread(new ThreadStart(Request));
                thread.IsBackground = false;
                thread.Priority = ThreadPriority.Highest;
                thread.Start();
            }
            else
                Request();
        }

        private void Request()
        {
            try
            {
                if (count_errors >= retry)
                    count_errors = 0;

                time_data_end = time_data_start.Add(time_data_step);
                Program.Log(String.Format("[Request] Server[{0}], Time[from]{1}, Time[to]{2}, Group[{3}]{4}, Parameter[{5}]{6}", group.client.server_name, time_data_start, time_data_end, group.id, group.name, id, name));
                List<Item> items = group.client.server.ReadItem(name, time_data_start, time_data_end, 0);
                if (items.Count <= 0)
                {
                    TimeSpan ts = new TimeSpan(time_request_step.Ticks);

                    if (ts.Days >= 1)
                        time_request = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                    else if (ts.Hours >= 1)
                        time_request = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    else if (ts.Minutes >= 1)
                        time_request = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0);
                    else
                        time_request = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                    time_request = time_request.Add(time_request_step);
                    time_data_start = time_data_start.Add(-time_data_step);
                }
                else
                {
                    time_data_start = new DateTime(time_data_end.Ticks);

                    // Добавление в базу
                    for (int i = 0; i < items.Count; i++)
                    {
                        double res;
                        bool isDouble = Double.TryParse(items[i].value.ToString(), out res);

                        string value_string = "";
                        if (isDouble)
                            value_string=Convert.ToDouble(items[i].value).ToString(System.Globalization.CultureInfo.InvariantCulture);
                        else
                        {
                            string[] values = (string[])items[i].value;
                            for (int j = 0; j < values.Length; j++)
                                value_string += values[j]+" ";

                            value_string = "'" + value_string + "'";
                        }

                        string query = String.Format(@"INSERT OR IGNORE INTO data (
                                                         parameter_id,
                                                         time,
                                                         value
                                                     )
                                                     VALUES (
                                                         {0},
                                                         '{1}',
                                                         {2}
                                                     );", 
                                                        id, 
                                                        items[i].time.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                                        value_string);

                        Program.sqlite.Execute(query);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log(String.Format("[Request->Error] {0}", ex.Message));
                count_errors++;
                if (count_errors >= retry)
                {
                    time_request = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                    time_request = time_request.Add(time_request_step);
                }
            }
        }
    }
}
