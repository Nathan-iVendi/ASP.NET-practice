using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CityInfo.API.Controllers
{   // I don't put [Authorize] within here as it needs to be
    // accessible to Unauthenticated users that want to authenticate.
    [Route("api/authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        // We will not use this outside this class, so we can scope it to this namespace.
        public class AuthenticationRequestBody
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        private class CityInfoUser // To store user information
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string City { get; set; }

            public CityInfoUser(
                int userId,
                string username,
                string firstName,
                string lastName,
                string city)
            {
                UserId = userId;
                Username = username;
                FirstName = firstName;
                LastName = lastName;
                City = city;
            }

        }

        public AuthenticationController(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpPost("authenticate")]
        public ActionResult<string> Authenticate(
            AuthenticationRequestBody authenticationRequestBody) // Accept as input
        {
            // Step 1: Validate the Username & Password
            var user = ValidateUserCredentials(
                authenticationRequestBody.Username,
                authenticationRequestBody.Password);

            if (user == null)
            {
                return Unauthorized();
            }

            // Step 2: Create a token
            var securityKey = new SymmetricSecurityKey(
                Convert.FromBase64String(_configuration["Authentication:SecretForKey"]));
            var signingCredentials = new SigningCredentials(
                securityKey, SecurityAlgorithms.HmacSha256); // < Widely-used standard - HS256

            //List of claims that the user has:
            var claimsForToken = new List<Claim>(); // Sub is the standardised key for the unique user identifier
            claimsForToken.Add(new Claim("sub", user.UserId.ToString()));
            claimsForToken.Add(new Claim("given_name", user.FirstName)); // Standardised name for FirstName 
            claimsForToken.Add(new Claim("family_name", user.LastName)); // Standardised name for LastName
            claimsForToken.Add(new Claim("city", user.City));

            var jwtSecurityToken = new JwtSecurityToken(
                _configuration["Authentication:Issuer"],
                _configuration["Authentication:Audience"],
                claimsForToken,
                DateTime.UtcNow, // Indicates the start of token validity - Not valid before now
                DateTime.UtcNow.AddHours(1), // Indicates the end of token validity - After this time it is invalid
                signingCredentials);

            var tokenToReturn = new JwtSecurityTokenHandler() // Token handler
                .WriteToken(jwtSecurityToken);

            return Ok(tokenToReturn); // Returning the token string
        }

        private CityInfoUser ValidateUserCredentials(string? username, string? password)
        {
            // We do not have a User DB or table. When I do, check the passed-through
            // username/password against what is stored in the database.
            // For demo purposes, we assume the credentials are valid.

            // Return a new CityInfoUser (Values would normally come from the user DB/table)
            return new CityInfoUser(
                1,
                username ?? "",
                "Nathan",
                "Yates",
                "Antwerp");

        }
    }
}
