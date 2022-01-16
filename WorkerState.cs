using System;

namespace qubic_miner_helper
{
    public class WorkerState
    {
        public int workerId;
        public int workerThreads;
        public DateTime starteDateTime;
        public DateTime updatedDateTime;
        public DateTime lastErrorFoundByWorkerDateTime;
        public float lastErrorReductionCount;
        public int workerRestartedTimes;

        public string qinerVersion;
        public string errorsLeftText;
        public string rankText;
        public string iterationsPerSecondText;
        public string errorsPerHourText;
        public string poolErrorsPerHourText;
        public string fullLog;
    }
}