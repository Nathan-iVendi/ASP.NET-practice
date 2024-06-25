using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CityInfo.API.Controllers
{
    // [Authorize] attribute is left out here because this controller needs to be accessible to unauthenticated users.
    // This allows users to obtain authentication by sending their credentials.
    [Route("api/authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        // This nested class is used to represent the structure of the request body for authentication.
        public class AuthenticationRequestBody
        {
            public string? Username { get; set; }
            public string? Password { get; set; }
        }

        // To store user information
        private class CityInfoUser
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string City { get; set; }

            // Constructor to initialize a CityInfoUser object with specific values.
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

        // Returns an authentication token if the credentials are valid.
        [HttpPost("authenticate")]
        public ActionResult<string> Authenticate(AuthenticationRequestBody authenticationRequestBody)
        {
            var user = ValidateUserCredentials(
                authenticationRequestBody.Username,
                authenticationRequestBody.Password);

            if (user == null)
            {
                return Unauthorized();
            }

            // Step 2: Create a security token for the authenticated user.
            // The secret key is retrieved from the configuration settings.
            var securityKey = new SymmetricSecurityKey(
                Convert.FromBase64String(_configuration["Authentication:SecretForKey"]));
            // Use HMAC SHA256 for signing the token.
            var signingCredentials = new SigningCredentials(
                securityKey, SecurityAlgorithms.HmacSha256);

            // List of claims representing the user's identity and attributes.
            // "sub" is the standard claim for the user's unique identifier.
            var claimsForToken = new List<Claim>
            {
                new Claim("sub", user.UserId.ToString()),
                new Claim("given_name", user.FirstName), // Standardized claim for the user's first name.
                new Claim("family_name", user.LastName), // Standardized claim for the user's last name.
                new Claim("city", user.City) // Custom claim for the user's city.
            };

            // Create the JWT token with issuer, audience, claims, validity period, and signing credentials.
            var jwtSecurityToken = new JwtSecurityToken(
                _configuration["Authentication:Issuer"],
                _configuration["Authentication:Audience"],
                claimsForToken,
                DateTime.UtcNow, // The token is valid from now.
                DateTime.UtcNow.AddHours(1), // The token expires in 1 hour.
                signingCredentials);

            // Write the token to a string format.
            var tokenToReturn = new JwtSecurityTokenHandler()
                .WriteToken(jwtSecurityToken);

            // Return the token string in the response.
            return Ok(tokenToReturn);
        }

        // This method simulates the validation of user credentials.
        // In a real application, this would involve checking the credentials against a database.
        private CityInfoUser ValidateUserCredentials(string? username, string? password)
        {
            // No user database is available for this currently. In this case, assume the credentials are valid.
            // In a real scenario, it would retrieve and validate the user from a database.

            // Create and return a new CityInfoUser object. These values would typically come from the database.
            return new CityInfoUser(
                1,
                username ?? "", // Use the provided username or an empty string if null.
                "Nathan",
                "Yates",
                "Antwerp"
            );
        }
    }
}
