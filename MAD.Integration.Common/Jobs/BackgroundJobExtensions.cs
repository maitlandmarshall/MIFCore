using Hangfire;
using Hangfire.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MAD.Integration.Common.Jobs
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
    }
}
