using System;
using System.Collections.Generic;

namespace qubic_miner_helper
{
    public class MachineState
    {
        public string machineName;
        public float overallCurrentIterationsPerSecond;
        public float overallSessionErrorsFound;
        public int overallWorkerCount;
        public int overallThreadCount;
        public int overallRestartTimes;
        public DateTime currentMachineDateTime;
        public DateTime lastErrorReductionByMachineDateTime;
        public string currentCommandLine;
        public string currentMinerVersion;
        public string currentHelperVersion;
        public string currentMinerPath;
        public string currentCPUTemps;
        public string currentCPULoads;
        public List<WorkerState> currentWorkerStates;
    }
}