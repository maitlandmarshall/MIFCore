using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
                var entityType = dbContext.Model.FindEntityType(entity.GetType());

                if (entityType is null)
                    return;

                transformations?.Invoke(entity);

                var primaryKey = entityType.FindPrimaryKey();
                var keys = primaryKey.Properties.Select(x =>
                {
                    if (x.PropertyInfo is null)
                    {
                        return g.Entry.Property(x.Name).CurrentValue;
                    }
                    else
                    {
                        return x.PropertyInfo.GetValue(entity);
                    }
                }).ToList();

                if (entityType.IsOwned())
                {
                    g.Entry.State = g.SourceEntry.State;
                }
                else
                {
                    if (keys.Count == 1)
                    {
                        dbContext.AddOrUpdateEntity(entity, g.Entry, keys.First());
                    }
                    else
                    {
                        dbContext.AddOrUpdateEntity(entity, g.Entry, keys.ToArray());
                    }
                }
            });
        }

        private static void AddOrUpdateEntity(this DbContext dbContext, object entity, EntityEntry entry, params object[] primaryKeys)
        {
            var existingEntity = dbContext.Find(entity.GetType(), primaryKeys);

            if (existingEntity is null)
            {
                entry.State = EntityState.Added;
            }
            else
            {
                dbContext.Entry(existingEntity).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            }
        }
    }
}
