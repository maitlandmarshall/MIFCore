using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MAD.Integration.Common.EFCore
{
    public static class UseDesignDefaultsExtensions
    {
        public static DbContextOptionsBuilder UseDesignDefaults(this DbContextOptionsBuilder dbContextOptionsBuilder)
        {
            var json = JObject.Parse(File.ReadAllText("settings.default.json"));
            dbContextOptionsBuilder.UseSqlServer(json["connectionString"].ToString());

            return dbContextOptionsBuilder;
        }
    }
}
