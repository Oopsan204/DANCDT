namespace DACDT_2026
{
    public sealed class ProgramRunCompletionTracker
    {
        private bool observedActiveDataNo;

        public void Begin()
        {
            observedActiveDataNo = false;
        }

        public void Reset()
        {
            observedActiveDataNo = false;
        }

        public bool Observe(int activeDataNo, int lastDataNo, int processRowCount, bool allAxesStopped)
        {
            if (processRowCount <= 0)
                return false;

            if (activeDataNo > 0)
                observedActiveDataNo = true;

            if (!observedActiveDataNo || !allAxesStopped)
                return false;

            if (lastDataNo < processRowCount)
                return false;

            return activeDataNo == 0 || activeDataNo >= processRowCount;
        }
    }
}
