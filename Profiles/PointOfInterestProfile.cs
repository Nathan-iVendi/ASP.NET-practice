using AutoMapper;
using System.Runtime.CompilerServices;

namespace CityInfo.API.Profiles
{
    public class PointOfInterestProfile : Profile
    {
        public PointOfInterestProfile()
        {
            // Created the mapper for PointOfInterestDto
            CreateMap<Entities.PointOfInterest, Models.PointOfInterestDto>();

            // Created the mapper for PointOfInterestForCreationDto
            CreateMap<Models.PointOfInterestForCreationDto, Entities.PointOfInterest>();

            // Created the mapper for PointOfInterestForUpdateDto
            CreateMap<Models.PointOfInterestForUpdateDto, Entities.PointOfInterest>();

            // Created the mapper for partially updating the PointOfInterest
            CreateMap<Entities.PointOfInterest, Models.PointOfInterestForUpdateDto>();
        }
    }
}
