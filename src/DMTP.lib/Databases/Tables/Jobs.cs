﻿using System;

using DMTP.lib.Databases.Tables.Base;

namespace DMTP.lib.Databases.Tables
{
    public class Jobs : BaseTable
    {
        public string AssignedHost { get; set; }

        public string Name { get; set; }

        public string TrainingDataPath { get; set; }

        public string ModelType { get; set; }

        public DateTime SubmissionTime { get; set; }

        public DateTime CompletedTime { get; set; }

        public DateTime StartTime { get; set; }

        public bool Started { get; set; }

        public bool Completed { get; set; }

        public byte[] Model { get; set; }

        public string ModelEvaluationMetrics { get; set; }

        public string Debug { get; set; }
    }
}