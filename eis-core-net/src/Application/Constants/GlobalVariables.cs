namespace EisCore.Application.Constants
{
    public class GlobalVariables
    {
        private static readonly object stopLock = new object();
        private static readonly object transportLock=new object();
        private static bool isUnprocessedOutMessagePresent=true;
        private static bool isUnprocessedInMessagePresent=true;
        private static bool isTransportInterrupted = true;
        private static bool isCurrentIpLockedForConsumer = false;
        public static bool IsUnprocessedOutMessagePresent
        {
            get
            {
                lock (stopLock)
                {
                    return isUnprocessedOutMessagePresent;
                }

            }
            set
            {
                lock (stopLock)
                {
                    isUnprocessedOutMessagePresent = value;
                }
            }

        }
        public static bool IsUnprocessedInMessagePresent
        {
            get
            {
                lock (stopLock)
                {
                    return isUnprocessedInMessagePresent;
                }

            }
            set
            {
                lock (stopLock)
                {
                    isUnprocessedInMessagePresent = value;
                }
            }

        }
        public static bool IsTransportInterrupted
        {
            get
            {
                lock (transportLock)
                {
                    return isTransportInterrupted;
                }

            }
            set
            {
                lock (transportLock)
                {
                    isTransportInterrupted = value;
                }
            }

        }

        public static bool IsCurrentIpLockedForConsumer
        {
            get
            {
                lock (transportLock)
                {
                    return isCurrentIpLockedForConsumer;
                }

            }
            set
            {
                lock (transportLock)
                {
                    isCurrentIpLockedForConsumer = value;
                }
            }

        }
    }
}