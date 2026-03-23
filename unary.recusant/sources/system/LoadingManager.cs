using System;
using System.Collections.Generic;
using Godot;
using Unary.Core;

namespace Unary.Recusant
{
    [Tool]
    [GlobalClass]
    public partial class LoadingManager : Node, IModSystem
    {
        public bool IsLoading { get; private set; } = false;

        private enum LoadingJobType
        {
            None,
            Bool,
            Float
        };

        private struct LoadingJob
        {
            public LoadingJobType Type;
            public Func<bool> ValueBool;
            public Func<float> ValueFloat;
            public Func<float> MaxFloat;
        }

        private Type _switchState = null;
        private readonly HashSet<string> _removeQueue = new();
        private readonly Dictionary<string, LoadingJob> _jobs = new();

        // Normalized from 0.0 to 1.0
        public float _currentValue = 0.0f;
        public float GlobalValue { get; private set; } = 0.0f;

        public List<string> GetJobs()
        {
            List<string> jobs = [];

            foreach (var job in _jobs)
            {
                jobs.Add(job.Key);
            }

            return jobs;
        }

        public void AddJob(string name, Func<float> value, Func<float> max)
        {
            if (!IsLoading)
            {
                IsLoading = true;
            }

            _jobs[name] = new()
            {
                Type = LoadingJobType.Float,
                ValueFloat = value,
                MaxFloat = max
            };
        }

        public void AddJob(string name, Func<float> value)
        {
            if (!IsLoading)
            {
                IsLoading = true;
            }

            _jobs[name] = new()
            {
                Type = LoadingJobType.Float,
                ValueFloat = value,
                MaxFloat = () => { return 1.0f; }
            };
        }

        public void AddJob(string name, Func<bool> value)
        {
            if (!IsLoading)
            {
                IsLoading = true;
            }

            _jobs[name] = new()
            {
                Type = LoadingJobType.Bool,
                ValueBool = value
            };
        }

        public void AddJob(string name)
        {
            if (!IsLoading)
            {
                IsLoading = true;
            }

            _jobs[name] = new()
            {
                Type = LoadingJobType.None
            };
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

            foreach (var job in _removeQueue)
            {
                if (_jobs.ContainsKey(job))
                {
                    _jobs.Remove(job);
                }
            }

            _removeQueue.Clear();
        }
    }
}
