namespace OracleOfDereth
{
    // KillerNotification is sent to the character credited with the killing blow.
    // Victim notifications and nearby death broadcasts must not count as kills.
    public sealed class IncomeKills
    {
        private readonly IncomeMetric metric;
        public IncomeKills(IncomeMetric metric) { this.metric = metric; }

        public void ObserveEvent(int messageType, int eventType)
        {
            if (messageType == 0xF7B0 && eventType == 0x01AD) metric.AddGain(1);
        }
    }
}
