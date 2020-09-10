using Hangfire;
using Hangfire.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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

        public static string GetFingerprint(this Job job)
        {
            if (job.Type == null || job.Method == null)
            {
                return string.Empty;
            }

            return GetFingerprint(job.Type, job.Method, job.Args);
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
                foreach (var a in args)
                {
                    hashInputBytes.AddRange(GetHashBytes(a));
                }
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

        private static IEnumerable<byte> GetHashBytes(object a)
        {
            var aType = a.GetType();
            var aTypeCode = Type.GetTypeCode(aType);

            switch (aTypeCode)
            {
                case TypeCode.Object:
                    if (a is IEnumerable<object> aLst)
                    {
                        foreach (var aItem in aLst)
                        {
                            foreach (var b in GetHashBytes(aItem)) yield return b;
                        }
                    }
                    else
                    {
                        foreach (var b in Encoding.UTF8.GetBytes(a.ToString())) yield return b;
                    }

                    break;

                default:
                    foreach (var b in Encoding.UTF8.GetBytes(a.ToString())) yield return b;
                    break;
            }
        }
    }
}
