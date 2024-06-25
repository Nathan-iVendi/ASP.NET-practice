using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CityInfo.API.Models;
using Microsoft.AspNetCore.JsonPatch;
using CityInfo.API.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;

namespace CityInfo.API.Controllers
{
    [Route("api/cities/{cityId}/pointsofinterest")]
    [Authorize(Policy = "MustBeFromAntwerp")] // Requires authentication and enforces a specific policy for access control.
    [ApiController]
    public class PointsOfInterestController : ControllerBase
    {
        private readonly ILogger<PointsOfInterestController> _logger; // Logger for logging information, warnings, and errors.
        private readonly IMailService _mailService; // Service for sending emails.
        private readonly ICityInfoRepository _cityInfoRepository;
        private readonly IMapper _mapper;

        // Constructor for the PointsOfInterestController, injecting dependencies via the constructor injection.
        public PointsOfInterestController(ILogger<PointsOfInterestController> logger,
            IMailService mailService,
            ICityInfoRepository cityInfoRepository,
            IMapper mapper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mailService = mailService ?? throw new ArgumentNullException(nameof(mailService));
            _cityInfoRepository = cityInfoRepository ?? throw new ArgumentNullException(nameof(cityInfoRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        // Retrieves all points of interest for a specific city.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PointOfInterestDto>>> GetPointsOfInterest(
            int cityId)
        {
            // Checks if the city exists in the repository.
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                // Logs a message indicating that the city was not found.
                _logger.LogInformation(
                    $"City with id {cityId} wasn't found when accessing points of interest.");
                return NotFound(); // Returns 404 Not Found if the city does not exist.
            }

            // Retrieves the points of interest for the specified city.
            var pointsOfInterestForCity = await _cityInfoRepository
                .GetPointsOfInterestForCityAsync(cityId);

            // Maps the retrieved points of interest to the DTO and returns them with a 200 OK response.
            return Ok(_mapper.Map<IEnumerable<PointOfInterestDto>>(pointsOfInterestForCity));
        }

        // Retrieves a specific point of interest for a given city.
        [HttpGet("{pointofinterestid}", Name = "GetPointOfInterest")]
        public async Task<ActionResult<PointOfInterestDto>> GetPointOfInterest(int cityId, int pointOfInterestId)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            // Retrieves the specific point of interest for the specified city.
            var pointOfInterest = await _cityInfoRepository
                .GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterest == null)
            {
                return NotFound(); // Returns 404 Not Found if the point of interest does not exist.
            }

            return Ok(_mapper.Map<PointOfInterestDto>(pointOfInterest));
        }

        // Creates a new point of interest for a specific city.
        [HttpPost]
        public async Task<ActionResult<PointOfInterestDto>> CreatePointOfInterest(
            int cityId, PointOfInterestForCreationDto pointOfInterest)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            // Maps the DTO to the entity model for the point of interest.
            var finalPointOfInterest = _mapper.Map<Entities.PointOfInterest>(pointOfInterest);

            // Adds the new point of interest to the repository for the specified city.
            await _cityInfoRepository
                .AddPointOfInterestForCityAsync(cityId, finalPointOfInterest);

            // Saves the changes to the repository.
            await _cityInfoRepository.SaveChangesAsync();

            // Maps the created point of interest entity back to a DTO.
            var createdPointOfInterestToReturn =
                _mapper.Map<Models.PointOfInterestDto>(finalPointOfInterest);

            // Returns the created point of interest with a 201 Created response, including a route to get the resource.
            return CreatedAtRoute("GetPointOfInterest",
                new
                {
                    cityId = cityId,
                    pointOfInterestId = createdPointOfInterestToReturn.Id
                },
                createdPointOfInterestToReturn);
        }

        // Updates an existing point of interest for a specific city.
        [HttpPut("{pointofinterestid}")]
        public async Task<ActionResult> UpdatePointOfInterest(int cityId, int pointOfInterestId,
            PointOfInterestForUpdateDto pointOfInterest)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _cityInfoRepository
                .GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            // Maps the update DTO to the existing point of interest entity.
            _mapper.Map(pointOfInterest, pointOfInterestEntity);

            await _cityInfoRepository.SaveChangesAsync();

            // Returns a 204 No Content response to indicate successful update.
            return NoContent();
        }

        // Partially updates an existing point of interest for a specific city.
        [HttpPatch("{pointofinterestid}")]
        public async Task<ActionResult> PartiallyUpdatePointOfInterest(int cityId, int pointOfInterestId,
            JsonPatchDocument<PointOfInterestForUpdateDto> patchDocument)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _cityInfoRepository
                .GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            // Maps the point of interest entity to a DTO for partial update.
            var pointOfInterestToPatch = _mapper.Map<PointOfInterestForUpdateDto>(pointOfInterestEntity);

            // Applies the JSON Patch document to the DTO.
            patchDocument.ApplyTo(pointOfInterestToPatch, ModelState);

            // Checks for any validation errors.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Returns 400 Bad Request if the model state is invalid.
            }

            // Validates the patched model.
            if (!TryValidateModel(pointOfInterestToPatch))
            {
                return BadRequest(ModelState); // Returns 400 Bad Request if validation fails.
            }

            // Maps the patched DTO back to the entity model.
            _mapper.Map(pointOfInterestToPatch, pointOfInterestEntity);

            await _cityInfoRepository.SaveChangesAsync();

            return NoContent();
        }

        // Deletes an existing point of interest for a specific city.
        [HttpDelete("{pointofinterestid}")]
        public async Task<ActionResult> DeletePointOfInterest(int cityId, int pointOfInterestId)
        {
            if (!await _cityInfoRepository.CityExistsAsync(cityId))
            {
                return NotFound();
            }

            var pointOfInterestEntity = await _cityInfoRepository
                .GetPointOfInterestForCityAsync(cityId, pointOfInterestId);

            if (pointOfInterestEntity == null)
            {
                return NotFound();
            }

            // Deletes the point of interest from the repository.
            _cityInfoRepository.DeletePointOfInterest(pointOfInterestEntity);

            await _cityInfoRepository.SaveChangesAsync();

            // Sends an email notification about the deletion.
            _mailService.Send("Point of interest deleted.", // Subject of the email.
                $"Point of interest {pointOfInterestEntity.Name} with ID {pointOfInterestEntity.Id} was deleted."); // Message content.

            // Returns a 204 No Content response to indicate successful deletion.
            return NoContent();
        }
    }
}
