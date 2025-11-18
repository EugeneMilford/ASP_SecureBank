using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SecureBank.API.Data;
using SecureBank.API.Models.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;
using SecureBank.API.Models.Domain;

namespace SecureBank.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly BankingContext _context;
        private readonly string _SecretKey;

        public UsersController(BankingContext context, IConfiguration configuration)
        {
            _context = context;
            _SecretKey = configuration.GetValue<string>("ApiSettings:Secret");
        }

        // Allow anonymous so users can log in without a token
        [AllowAnonymous]
        [HttpPost("UserLogin")]
        public async Task<ActionResult<UserLoginResponseDto>> Login(UserLoginRequestDto loginDetails)
        {
            if (loginDetails == null || string.IsNullOrWhiteSpace(loginDetails.Username) || string.IsNullOrWhiteSpace(loginDetails.Password))
                return BadRequest("Username and password are required.");

            // Find user by username (case-insensitive)
            var user = await _context.users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == loginDetails.Username.ToLower());

            // NOTE: In production you MUST verify the hashed password (bcrypt/Argon2/PBKDF2).
            if (user == null || user.Password != loginDetails.Password)
            {
                // Don't reveal whether username or password failed
                return Unauthorized("Invalid username or password.");
            }

            // Create JWT token including user id and role
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_SecretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // important: numeric id
                new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var loginResponseDto = new UserLoginResponseDto()
            {
                Token = tokenString,
                UserDetails = user
            };

            return Ok(loginResponseDto);
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<ActionResult<UserRegisterResponseDto>> Register(UserRegisterRequestDto registerDto)
        {
            if (registerDto == null)
                return BadRequest("Invalid registration data.");

            // Basic validations
            if (_context.users.Any(u => u.Username.ToLower() == registerDto.Username.ToLower()))
                return BadRequest("Username already exists.");
            if (_context.users.Any(u => u.Email.ToLower() == registerDto.Email.ToLower()))
                return BadRequest("Email already exists.");

            var user = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email,
                Username = registerDto.Username,
                Password = registerDto.Password, // TODO: Hash this in production
                PhoneNumber = registerDto.PhoneNumber,
                Role = string.IsNullOrEmpty(registerDto.Role) ? "User" : registerDto.Role,
                CreatedDate = DateTime.UtcNow
            };

            _context.users.Add(user);
            await _context.SaveChangesAsync();

            var response = new UserRegisterResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                Message = "Registration successful."
            };

            return Ok(response);
        }
    }
}


