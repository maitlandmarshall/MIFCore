using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SqlKata.Compilers;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MAD.Integration.Common.EFCore
{
    public static class UpsertExtensions
    {
        public static void Upsert(this DbContext dbContext, object entity, Action<object> transformations = null)
        {
            dbContext.ChangeTracker.TrackGraph(entity, g =>
            {
                var entity = g.Entry.Entity;
                var entityType = g.Entry.OriginalValues.EntityType;

                if (entityType is null)
                    return;

                transformations?.Invoke(entity);

                var primaryKey = entityType.FindPrimaryKey();
                var keys = primaryKey.Properties.ToDictionary(
                    keySelector: y => y.Name,
                    elementSelector: x =>
                    {
                        if (x.PropertyInfo is null)
                        {
                            return g.Entry.Property(x.Name).CurrentValue;
                        }
                        else
                        {
                            return x.PropertyInfo.GetValue(entity);
                        }
                    });

                if (entityType.IsOwned())
                {
                    var ownership = entityType.FindOwnership();
                    var navigation = ownership.GetNavigation(false);

                    if (navigation.IsCollection())
                    {
                        dbContext.AddOrUpdateEntity(g.Entry, entityType, keys);
                    }
                    else
                    {
                        g.Entry.State = g.SourceEntry.State;
                    }
                }
                else
                {
                    dbContext.AddOrUpdateEntity(g.Entry, entityType, keys);
                }
            });
        }

        private static void AddOrUpdateEntity(this DbContext dbContext, EntityEntry entry, IEntityType entityType, IDictionary<string, object> keys)
        {
            var tableName = entityType.GetTableName();
            var db = dbContext.GetQueryFactory();

            var existingEntityCount = db
                .Query(tableName)
                .Where(keys)
                .Count<int>();
            
            if (existingEntityCount == 0)
            {
                entry.State = EntityState.Added;
            }
            else
            {
                entry.State = EntityState.Modified;
            }
        }

        private static QueryFactory GetQueryFactory(this DbContext dbContext)
        {
            var compiler = new SqlServerCompiler();
            var db = new QueryFactory(dbContext.Database.GetDbConnection(), compiler);

            return db;
        }
    }
}
