using Godot;
using System;
using System.Collections.Generic;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class LoadingManager : Node, IModSystem
    {
        public bool IsLoading { get; private set; } = false;

        public enum LoadingJobType
        {
            None,
            Bool,
            Float
        };

        public struct LoadingJob
        {
            public string Name;
            public LoadingJobType Type;
            public Func<bool> ValueBool;
            public Func<float> ValueFloat;
            public Func<float> MaxFloat;
        }

        private Type _switchState = null;
        private readonly HashSet<uint> _removeQueue = [];
        private readonly Dictionary<uint, LoadingJob> _jobs = [];

        // Normalized from 0.0 to 1.0
        public float _currentValue = 0.0f;
        public float GlobalValue { get; private set; } = 0.0f;

        private uint _jobId = 0;

        public struct JobData
        {
            public uint Id;
            public LoadingJob Job;
        }

        public EventFunc<JobData> OnJobAdded { get; } = new();
        public EventFunc<JobData> OnJobFinished { get; } = new();

        public void AddJob(string name, Func<float> value, Func<float> max)
        {
            if (!IsLoading)
            {
                IsLoading = true;
            }

            _jobs[_jobId] = new()
            {
                Name = name,
                Type = LoadingJobType.Float,
                ValueFloat = value,
                MaxFloat = max
            };

            OnJobAdded.Publish(new()
            {
                Id = _jobId,
                Job = _jobs[_jobId]
            });

            _jobId++;
        }

        public void AddJob(string name, Func<float> value)
        {
            if (!IsLoading)
            {
                IsLoading = true;
            }

            _jobs[_jobId] = new()
            {
                Name = name,
                Type = LoadingJobType.Float,
                ValueFloat = value,
                MaxFloat = () => { return 1.0f; }
            };

            OnJobAdded.Publish(new()
            {
                Id = _jobId,
                Job = _jobs[_jobId]
            });

            _jobId++;
        }

        public void AddJob(string name, Func<bool> value)
        {
            if (!IsLoading)
            {
                IsLoading = true;
            }

            _jobs[_jobId] = new()
            {
                Name = name,
                Type = LoadingJobType.Bool,
                ValueBool = value
            };

            OnJobAdded.Publish(new()
            {
                Id = _jobId,
                Job = _jobs[_jobId]
            });

            _jobId++;
        }

        public void AddJob(string name)
        {
            if (!IsLoading)
            {
                IsLoading = true;
            }

            _jobs[_jobId] = new()
            {
                Name = name,
                Type = LoadingJobType.None
            };

            OnJobAdded.Publish(new()
            {
                Id = _jobId,
                Job = _jobs[_jobId]
            });

            _jobId++;
        }

        // Automatically switches to switchStateOnDone UI-wise when done
        public void ShowLoading(Type switchStateOnDone = null)
        {
            UiManager.Singleton.Open(typeof(UiLoadingState));
            GlobalValue = 0.0f;
            _switchState = switchStateOnDone;
        }

        // Cleans all loading tasks and switches to the provided state immediatelly
        public void HideLoading(Type switchState)
        {
            IsLoading = false;
            GlobalValue = 1.0f;
            _jobs.Clear();

            if (switchState != null)
            {
                UiManager.Singleton.Open(switchState);
            }
        }

        void ISystem.Process(float delta)
        {
            if (!IsLoading)
            {
                return;
            }

            foreach (var job in _removeQueue)
            {
                if (_jobs.TryGetValue(job, out LoadingJob value))
                {
                    OnJobFinished.Publish(new()
                    {
                        Id = job,
                        Job = value
                    });
                    _jobs.Remove(job);
                }
            }

            _removeQueue.Clear();

            float calculatedValue = 0.0f;
            float calculatedMax = 0.0f;

            if (_jobs.Count == 0)
            {
                HideLoading(_switchState);
                return;
            }

            foreach (var job in _jobs)
            {
                var targetJob = job.Value;

                if (_removeQueue.Contains(job.Key))
                {
                    continue;
                }

                switch (job.Value.Type)
                {
                    default:
                    case LoadingJobType.None:
                        {
                            calculatedValue += 0.99f;
                            calculatedMax += 1.0f;

                            break;
                        }
                    case LoadingJobType.Bool:
                        {
                            bool value = targetJob.ValueBool();

                            if (value)
                            {
                                _removeQueue.Add(job.Key);
                                continue;
                            }

                            calculatedValue += 0.99f;
                            calculatedMax += 1.0f;

                            break;
                        }
                    case LoadingJobType.Float:
                        {
                            float value = targetJob.ValueFloat();
                            float max = targetJob.MaxFloat();

                            if (Mathf.IsEqualApprox(value, max))
                            {
                                _removeQueue.Add(job.Key);
                                continue;
                            }

                            calculatedValue += value;
                            calculatedMax += max;

                            break;
                        }
                }
            }

            // Prevent division by zero and NAN% in loading as a result
            if (Mathf.Abs(calculatedMax) <= Mathf.Epsilon)
            {
                calculatedMax = 1.0f;
            }

            _currentValue = calculatedValue.Remap(0.0f, calculatedMax, 0.0f, 1.0f);
            GlobalValue = Mathf.Lerp(GlobalValue, _currentValue, 0.33f);
        }
    }
}
