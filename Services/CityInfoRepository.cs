using CityInfo.API.DbContexts;
using CityInfo.API.Entities;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace CityInfo.API.Services
{
    public class CityInfoRepository : ICityInfoRepository
    {
        private readonly CityInfoContext _context;

        public CityInfoRepository(CityInfoContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<City>> GetCitiesAsync()
        {
            return await _context.Cities.OrderBy(c => c.Name).ToListAsync(); // Order cities by name.
        }

        public async Task<bool> CityNameMatchesCityId(string? cityName, int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId && c.Name == cityName);
        }

        public async Task<(IEnumerable<City>, PaginationMetadata)> GetCitiesAsync(
            string? name, string? searchQuery, int pageNumber, int pageSize)
        {
            // Collection to start from:
            var collection = _context.Cities as IQueryable<City>;

            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim(); // Trims leading or trailing spaces
                collection = collection.Where(c => c.Name == name); // Filters
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {// Return any order where name or description contains the search query
                searchQuery = searchQuery.Trim();
                collection = collection.Where(a => a.Name == (searchQuery)
                    || (a.Description != null && a.Description.Contains(searchQuery)));// Avoids null issues
            }

            var totalItemCount = await collection.CountAsync();

            var paginationMetadata = new PaginationMetadata(
                totalItemCount, pageSize, pageNumber);

            var collectionToReturn = await collection.OrderBy(c => c.Name)
                // Leave this till last otherwise the search and filter will be based on one page
                .Skip(pageSize * (pageNumber - 1)) // Skip orders
                .Take(pageSize) // Requested page size
                .ToListAsync(); // Executed on the DB.

            return (collectionToReturn, paginationMetadata);
        }

        public async Task<City?> GetCityAsync(int cityId, bool includePointsOfInterest)
        {
            if (includePointsOfInterest)
            {
                return await _context.Cities.Include(c => c.PointsOfInterest)
                    .Where(c => c.Id == cityId).FirstOrDefaultAsync(); // If points of interest should be included.
            }

            return await _context.Cities.Where(c => c.Id == cityId)
                .FirstOrDefaultAsync(); // If points of interest shouldn't be included.
        }

        public async Task<bool> CityExistsAsync(int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId); 
            // Returns true if city with cityId does exist, false if it does not.
        }

        public async Task<PointOfInterest?> GetPointOfInterestForCityAsync(
            int CityId,
            int pointOfInterestId)
        {
            return await _context.PointsOfInterest
                .Where(p => p.CityId == CityId && p.Id == pointOfInterestId)
                .FirstOrDefaultAsync(); // Return point of interest, needs to match the CityId and POI Id.
        }

        public async Task<IEnumerable<PointOfInterest>> GetPointsOfInterestForCityAsync(int cityId)
        {
            return await _context.PointsOfInterest
                .Where(p => p.CityId == cityId).ToListAsync(); // Bring back points of interest where the CityID matches.
        }

        public async Task AddPointOfInterestForCityAsync(int cityId, PointOfInterest pointOfInterest)
        {
            var city = await GetCityAsync(cityId, false);
            if (city != null)
            {
                city.PointsOfInterest.Add(pointOfInterest);
            }
        }

        public void DeletePointOfInterest(PointOfInterest pointOfInterest)
        {
            _context.PointsOfInterest.Remove(pointOfInterest);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync() >=0);
        }
    }
}
