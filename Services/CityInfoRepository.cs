using CityInfo.API.DbContexts;
using CityInfo.API.Entities;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

namespace CityInfo.API.Services
{
    public class CityInfoRepository : ICityInfoRepository
    {
        private readonly CityInfoContext _context;

        // Constructor that injects the CityInfoContext dependency.
        // Throws an ArgumentNullException if the context is null, ensuring a valid context.
        public CityInfoRepository(CityInfoContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IEnumerable<City>> GetCitiesAsync()
        {
            return await _context.Cities.OrderBy(c => c.Name).ToListAsync();
            // Uses LINQ to order cities by their Name property.
        }

        public async Task<bool> CityNameMatchesCityId(string? cityName, int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId && c.Name == cityName);
            // AnyAsync() checks if any cities match the given criteria.
            // This is useful for validating the association between city name and ID.
        }

        // Retrieves a paginated list of cities filtered by name and search query.
        public async Task<(IEnumerable<City>, PaginationMetadata)> GetCitiesAsync(
            string? name, string? searchQuery, int pageNumber, int pageSize)
        {
            var collection = _context.Cities as IQueryable<City>;
            // IQueryable allows for deferred execution, building a query to be executed later.

            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.Trim();
                collection = collection.Where(c => c.Name == name);
                // Filters cities by exact match of the name after trimming whitespace.
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim();
                collection = collection.Where(a => a.Name.Contains(searchQuery)
                    || (a.Description != null && a.Description.Contains(searchQuery)));
                // Filters cities by checking if the name or description contains the search query.
                // The description check avoids null reference issues.
            }

            var totalItemCount = await collection.CountAsync();
            // Counts the total number of items after applying filters.

            var paginationMetadata = new PaginationMetadata(
                totalItemCount, pageSize, pageNumber);
            // PaginationMetadata holds information for pagination such as total count, page size, and number.

            var collectionToReturn = await collection.OrderBy(c => c.Name)
                .Skip(pageSize * (pageNumber - 1))
                .Take(pageSize)
                .ToListAsync();
            // Orders the collection by name, skips items based on page number and size, and takes the required number.

            return (collectionToReturn, paginationMetadata);
        }

        // Retrieves a city by ID, optionally including its points of interest.
        public async Task<City?> GetCityAsync(int cityId, bool includePointsOfInterest)
        {
            if (includePointsOfInterest)
            {
                return await _context.Cities.Include(c => c.PointsOfInterest)
                    .Where(c => c.Id == cityId).FirstOrDefaultAsync();
                // Includes related PointsOfInterest entities in the result if requested.
            }

            return await _context.Cities.Where(c => c.Id == cityId)
                .FirstOrDefaultAsync();
            // Returns the city entity matching the cityId without including related entities.
        }

        public async Task<bool> CityExistsAsync(int cityId)
        {
            return await _context.Cities.AnyAsync(c => c.Id == cityId);
            // Returns true if any city exists with the specified cityId, otherwise false.
        }

        public async Task<PointOfInterest?> GetPointOfInterestForCityAsync(int CityId, int pointOfInterestId)
        {
            return await _context.PointsOfInterest
                .Where(p => p.CityId == CityId && p.Id == pointOfInterestId)
                .FirstOrDefaultAsync();
            // Filters PointsOfInterest by CityId and pointOfInterestId, returning the first match or null.
        }

        public async Task<IEnumerable<PointOfInterest>> GetPointsOfInterestForCityAsync(int cityId)
        {
            return await _context.PointsOfInterest
                .Where(p => p.CityId == cityId).ToListAsync();
            // Returns a list of points of interest where the CityId matches the specified cityId.
        }

        public async Task AddPointOfInterestForCityAsync(int cityId, PointOfInterest pointOfInterest)
        {
            var city = await GetCityAsync(cityId, false);
            if (city != null)
            {
                city.PointsOfInterest.Add(pointOfInterest);
                // Adds the pointOfInterest to the city's PointsOfInterest collection.
                // Entity Framework tracks changes and will add the new entity to the database when SaveChangesAsync is called.
            }
        }

        public void DeletePointOfInterest(PointOfInterest pointOfInterest)
        {
            _context.PointsOfInterest.Remove(pointOfInterest);
            // Marks the pointOfInterest entity for deletion from the database.
            // The actual deletion occurs when SaveChangesAsync is called.
        }

        // Saves all changes made in this context to the database.
        public async Task<bool> SaveChangesAsync()
        {
            return (await _context.SaveChangesAsync() >= 0);
            // SaveChangesAsync() returns the number of affected rows.
            // This method returns true if the number of affected rows is zero or positive, indicating success.
        }
    }
}
