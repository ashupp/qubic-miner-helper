using System;

namespace qubic_miner_helper
{
    public class MachineState
    {
        public string machineName;
        public float overallCurrentIterationsPerSecond;
        public float overallSessionErrorsFound;
        public int overallWorkerCount;
        public int overallThreadCount;
        public DateTime currentMachineDateTime;
        public string currentCommandLine;
        public string currentMinerVersion;
        public string currentMinerPath;
        public WorkerState[] currentWorkerStates;
    }
}