using MIFCore.Hangfire.APIETL.Load;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIFCore.Hangfire.APIETL.SqlServer
{
    internal class SqlServerGetDestinationType : IGetDestinationType
    {
        public Task<string> GetDestinationType(ApiEndpointModel apiEndpointModel, string sourceKey, IEnumerable<Type> sourceModelTypes)
        {
            var clrType = sourceModelTypes.FirstOrDefault(y => y != null);
            var destinationType = Type.GetTypeCode(clrType) switch
            {
                TypeCode.Empty => throw new NotImplementedException(),
                TypeCode.Object => throw new NotImplementedException(),
                TypeCode.DBNull => throw new NotImplementedException(),
                TypeCode.Boolean => "bit",
                TypeCode.Char => "char(max)",
                TypeCode.SByte or TypeCode.Byte => "binary",
                TypeCode.Int16 => "smallint",
                TypeCode.UInt16 => "smallint",
                TypeCode.Int32 or TypeCode.UInt32 => "int",
                TypeCode.Int64 or TypeCode.UInt64 => "bigint",
                TypeCode.Single => "real",
                TypeCode.Double => "float",
                TypeCode.Decimal => "decimal(18,4)",
                TypeCode.DateTime => "datetimeoffset",
                TypeCode.String => "nvarchar(max)",
                _ => throw new NotImplementedException(),
            };

            return Task.FromResult(destinationType);
        }
    }
}
