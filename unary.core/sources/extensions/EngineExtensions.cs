using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace Unary.Core
{
    public static class EngineExtensions
    {
        private struct RamInfo : IComparable<RamInfo>
        {
            public ulong Capacity = 0;
            public uint Speed = 0;

            public RamInfo()
            {
            }

            public readonly int CompareTo(RamInfo other)
            {
                return Speed.CompareTo(other.Speed);
            }
        }

        private static List<RamInfo> GetRamInfo(this EngineInstance engine)
        {
#pragma warning disable CA1416
#if GODOT_WINDOWS

            List<RamInfo> entries = [];

            try
            {
                ManagementObjectSearcher searcher = new("SELECT Capacity, Speed FROM Win32_PhysicalMemory");

                foreach (ManagementObject obj in searcher.Get().Cast<ManagementObject>())
                {
                    entries.Add(new()
                    {
                        Capacity = (ulong)obj["Capacity"],
                        Speed = (uint)obj["Speed"]
                    });
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to fetch : {ex.Message}");
                return [];
            }
            entries.Sort();
            return entries;
#else
#pragma warning restore CA1416
            return (0, 0); // TODO implement for other platforms
#endif
        }

        public static byte[] SpecsHash(this EngineInstance engine)
        {
            StringBuilder builder = new();

            List<RamInfo> info = GetRamInfo(engine);

            if (info.Count == 0)
            {
                return []; // We have failed to aquire some data necessary to fingerprint this machine, fallback to 0
            }

            foreach (var target in info)
            {
                builder.Append(target.Capacity).Append(target.Speed);
            }

            builder.Append(RenderingServer.Singleton.GetVideoAdapterVendor());
            builder.Append((int)RenderingServer.Singleton.GetVideoAdapterType());
            builder.Append(RenderingServer.Singleton.GetVideoAdapterName());

            builder.Append(OS.GetProcessorCount());
            builder.Append(OS.GetProcessorName());

            builder.Append(OS.Singleton.GetModelName());
            builder.Append(OS.Singleton.GetName());

            return SHA256.HashData(Encoding.Unicode.GetBytes(builder.ToString()));
        }
    }
}
