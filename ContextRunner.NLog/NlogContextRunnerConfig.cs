﻿using System;
using ContextRunner.Base;
using NLog;

namespace ContextRunner.NLog
{
    public class NlogContextRunnerConfig
    {
        public NlogContextRunnerConfig()
        {
            EnableContextStartMessage = false;
            ContextStartMessageLevel = LogLevel.Trace;

            EnableContextEndMessage = true;
            ContextEndMessageLevel = LogLevel.Trace;

            ContextErrorMessageLevel = LogLevel.Error;
        }

        public bool EnableContextStartMessage { get; set; }
        public LogLevel ContextStartMessageLevel { get; set; }

        public bool EnableContextEndMessage { get; set; }
        public LogLevel ContextEndMessageLevel { get; set; }

        public LogLevel ContextErrorMessageLevel { get; set; }

        public bool AddSpacingToEntries { get; set; }
        public string[] SanitizedProperties { get; set; }
    }
}
