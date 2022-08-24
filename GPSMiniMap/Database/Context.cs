using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using GoogleApi;
using GoogleApi.Entities.Common.Enums;
using GoogleApi.Entities.Maps.Geocoding;
using GoogleApi.Entities.Maps.Geocoding.Address.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;

namespace GPSMiniMap.Database
{
    public static class DbSetExtensions
    {
        public static EntityEntry<T> AddIfNotExists<T>(this DbSet<T> dbSet, T entity, Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            var exists = predicate != null ? dbSet.Any(predicate) : dbSet.Any();
            return !exists ? dbSet.Add(entity) : null;
        }
    }

    public record LocationRecord
    {
        [Key]
        public DateTime Time { get; set; }


        public float Longitude { get; set; }
        public float Latitude { get; set; }
        public float Speed { get; set; }
        public float Heading { get; set; }
        public float Accuracy { get; set; }
        public float Altitude { get; set; }
        public float AltitudeAccuracy { get; set; }
    }

    public record GeocodeRecord
    {
        [Key]
        public string Name { get; set; }
        public string RealName { get; set; }


        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }


    public class Context : DbContext
    {
        private readonly IConfiguration _config;
        public DbSet<LocationRecord> LocationHistory { get; set; }
        public DbSet<GeocodeRecord> GeocodeCache { get; set; }

        public string DbPath { get; }

        public Context(IConfiguration config)
        {
            _config = config;
            //var folder = Environment.SpecialFolder.LocalApplicationData;
            //var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(/*path,*/ "gpsdatabase.db");
            Database.Migrate();
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={DbPath}");

        public async Task<GeocodeRecord> GetNameLocation(string name)
        {
            // Cache
            if (await GeocodeCache.AnyAsync(x => x.Name == name.ToLower()))
            {
                return await GeocodeCache.FirstAsync(x => x.Name == name.ToLower());
            }

            var response = GoogleMaps.Geocode.AddressGeocode.Query(new AddressGeocodeRequest {Address = name, Key = _config.GetValue<string>("GoogleApiKey") });

            if (response.Status != Status.Ok)
            {
                // fail, insert zero cache so we don't keep re-requesting things that will fail
                var newRecord = new GeocodeRecord
                {
                    Name = name.ToLower(),
                    Latitude = 0,
                    Longitude = 0
                };

                await GeocodeCache.AddAsync(newRecord);
                await SaveChangesAsync();

                return newRecord;
            }
            else
            {
                var newRecord = new GeocodeRecord
                {
                    Name = name.ToLower(),
                    RealName = response.Results.First().FormattedAddress,
                    Latitude = response.Results.First().Geometry.Location.Latitude,
                    Longitude = response.Results.First().Geometry.Location.Longitude
                };

                await GeocodeCache.AddAsync(newRecord);
                await SaveChangesAsync();

                return newRecord;
            }
        }






    }
}
