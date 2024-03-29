﻿using Hangfire;
using Hangfire.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MIFCore.Hangfire
{
    public static class BackgroundJobExtensions
    {
        public static BackgroundJob SetJobParameter(this BackgroundJob job, string name, object value)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            JobStorage.Current.GetConnection().SetJobParameter(job.Id, name, SerializationHelper.Serialize(value, SerializationOption.User));
            return job;
        }

        public static T GetJobParameter<T>(this BackgroundJob job, string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            try
            {
                return SerializationHelper.Deserialize<T>(JobStorage.Current.GetConnection().GetJobParameter(job.Id, name), SerializationOption.User);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Could not get a value of the job parameter `{name}`. See inner exception for details.", ex);
            }
        }

        public static string GetFingerprint(this Job job)
        {
            if (job.Type == null || job.Method == null)
            {
                return string.Empty;
            }

            return GetFingerprint(job.Type, job.Method, job.Args);
        }

        public static string GetJobName(this BackgroundJob backgroundJob)
        {
            // Check if the job is a recurring job
            var jobId = backgroundJob.GetJobParameter<string>("RecurringJobId");

            if (string.IsNullOrWhiteSpace(jobId))
            {
                // If it's not, try to get the display name of the job
                var jobDisplayNameAttribute = backgroundJob.Job.Method.GetCustomAttribute<JobDisplayNameAttribute>();

                if (jobDisplayNameAttribute != null)
                {
                    jobId = jobDisplayNameAttribute.DisplayName;
                }
                else
                {
                    var displayNameAttribute = backgroundJob.Job.Method.GetCustomAttribute<DisplayNameAttribute>();
                    jobId = displayNameAttribute?.DisplayName;
                }
            }

            jobId ??= backgroundJob.Job.ToString();

            return jobId;
        }

        public static DateTime? GetLastSuccess(this BackgroundJob job)
        {
            return job.GetJobParameter<DateTime?>(TrackLastSuccessAttribute.ParameterName);
        }

        internal static string GetFingerprint(Type type, MethodInfo method, IReadOnlyList<object> args)
        {
            var hashInputBytes = new List<byte>();
            var typeFullName = type.FullName;
            var methodName = method.Name;

            hashInputBytes.AddRange(Encoding.UTF8.GetBytes(typeFullName));
            hashInputBytes.AddRange(Encoding.UTF8.GetBytes(methodName));

            if (args != null)
            {
                hashInputBytes.AddRange(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(args)));
            }

            using var sha384Hasher = SHA384.Create();
            var hashBytes = sha384Hasher.ComputeHash(hashInputBytes.ToArray());  
            var builder = new StringBuilder();

            for (int i = 0; i < hashBytes.Length; i++)
            {
                builder.Append(hashBytes[i].ToString("x2"));
            }

            return builder.ToString();
        }
    }
}
