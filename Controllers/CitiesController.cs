using AutoMapper; // AutoMapper is used to map data between objects, simplifying the process of transforming entity models to DTOs and vice versa.
using CityInfo.API.Models;
using CityInfo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CityInfo.API.Controllers
{
    [ApiController] // Specifies that the class will handle HTTP API requests and responses
    [Authorize] // All actions within this controller require the user to be authenticated.
    [Route("api/cities")]
    public class CitiesController : ControllerBase
    {
        private readonly ICityInfoRepository _cityInfoRepository;
        private readonly IMapper _mapper;
        const int maxCitiesPageSize = 20;

        public CitiesController(ICityInfoRepository cityInfoRepository, IMapper mapper)
        {
            _cityInfoRepository = cityInfoRepository ?? throw new ArgumentNullException(nameof(cityInfoRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // Retrieves a paged list of cities, optionally filtered by name or search query.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CityWithoutPointsOfInterestDto>>> GetCities(
            string? name, string? searchQuery, int pageNumber = 1, int pageSize = 10)
        {
            // Ensures the requested page size does not exceed the maximum allowed limit.
            if (pageSize > maxCitiesPageSize)
            {
                pageSize = maxCitiesPageSize;
            }

            // Calls the method to retrieve the list of cities and pagination metadata.
            var (cityEntities, paginationMetadata) = await _cityInfoRepository.GetCitiesAsync(
                name, searchQuery, pageNumber, pageSize);

            // Adds the page metadata to the response headers, which allows clients to understand the paging details.
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(paginationMetadata));

            // Maps the list of city entities
            return Ok(_mapper.Map<IEnumerable<CityWithoutPointsOfInterestDto>>(cityEntities));
        }

        // Retrieves a specific city by its ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCity(
            int id, bool includePointsOfInterest = false)
        {
            // Calls the method to retrieve the city entity, with optional inclusion of points of interest.
            var city = await _cityInfoRepository.GetCityAsync(id, includePointsOfInterest);
            if (city == null)
            {
                // Returns a 404 Not Found response if the city is not found.
                return NotFound();
            }

            if (includePointsOfInterest)
            {
                // If points of interest are included, map the city entity to a CityDto and return it.
                return Ok(_mapper.Map<CityDto>(city));
            }

            // If points of interest are not included, map the city entity to a CityWithoutPointsOfInterestDto and return it.
            return Ok(_mapper.Map<CityWithoutPointsOfInterestDto>(city));
        }
    }
}
