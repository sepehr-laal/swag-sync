﻿namespace swag
{
    using System;
    using System.Net.NetworkInformation;

    /// <summary>
    /// Serves as a periodic Internet connectivity checker.
    /// Use it like: Internet.IsUp
    /// </summary>
    public class InternetService : Service
    {
        private bool m_IsUp = false;

        /// <summary>
        /// Event propagated whenever Internet is restored
        /// </summary>
        public Action ConnectionRestored;
        
        /// <summary>
        /// Event propagated whenever Internet is lost
        /// </summary>
        public Action ConnectionLost;

        /// <summary>
        /// Returns whether Google (aka the Internet) is reachable.
        /// This actually returns a cached version of the last periodic
        /// check result we made. The actual check happens every "CheckPeriod"
        /// </summary>
        public bool IsUp
        {
            get
            {
                if (Period == 0)
                    return true;

                if (!Started)
                    Start();

                return m_IsUp;
            }
        }

        protected override void Run()
        {
            bool was_up = m_IsUp;

            Log.Info("Checking Internet connectivity.");

            if (Period != 0)
            {
                bool timed_out = true;

                TaskUtils.RunTimed(() =>
                {
                    try
                    {
                        using (var ping = new Ping())
                        {
                            m_IsUp = (ping.Send("8.8.8.8").Status == IPStatus.Success);
                        }
                    }
                    catch
                    {
                        m_IsUp = false;
                    }
                    finally
                    {
                        timed_out = false;
                    }
                },
                TimeSpan.FromSeconds(5)).Wait();

                if (timed_out)
                    m_IsUp = false;
            }

            if (!was_up && m_IsUp)
            {
                Log.Write("Internet connectivity check passed.");

                if (ConnectionRestored != null)
                    ConnectionRestored();
            }
            else if (was_up && !m_IsUp)
            {
                Log.Write("Internet connectivity check failed.");

                if (ConnectionLost != null)
                    ConnectionLost();
            }
        }
    }
}
