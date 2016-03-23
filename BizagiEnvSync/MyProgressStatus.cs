using System;
using BizAgi.Commons.Collections;
using BizAgi.Commons.ProgressStatus;

namespace BizagiEnvSync
{
    /// <summary>
    /// Dummy implementation of required interface
    /// </summary>
    internal class MyProgressStatus : IProgressStatus
    {
        public string BoardLogDetailText { get; set; }
        public string BoardLogResumeText { get; set; }
        public string CurrentMessage { get; set; }
        public CList<string> CurrentMessagesHistory { get; }
        public string CurrentSubMessage { get; set; }
        public int CurrentValue { get; set; }
        public int IncrementStepValue { get; set; }
        public bool IsBoardLogActive { get; set; }
        public bool IsInfiniteLoopMode { get; }
        public bool IsInitialized { get; set; }
        public bool KeepCurrentMessageHistory { get; set; }
        public int MaximumValue { get; set; }
        public int MaximumValueExpected { get; set; }
        public int MinimumValue { get; set; }
        public string Title { get; set; }

        public string GetBoardLogDetailText()
        {
            return "PROGRESS: " + this.BoardLogDetailText;
        }

        public string GetBoardLogResumeText()
        {
            return "PROGRESS: " + this.BoardLogResumeText;
        }

        public string GetFullCurrentMessage()
        {
            return "PROGRESS: " + this.CurrentMessage;
        }

        public void InitializeInInfiniteLoopMode() {}
        public void InitializeInStepMode(int minimumValue, int maximumValue, int incrementStepValue, int currentValue) { }
        public void PerformStep() { }
    }
}