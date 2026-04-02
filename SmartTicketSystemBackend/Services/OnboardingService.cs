using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartTicketSystemBackend.Data;
using SmartTicketSystemBackend.DTOs.Auth;
using SmartTicketSystemBackend.DTOs.Onboarding;
using SmartTicketSystemBackend.Models;

namespace SmartTicketSystemBackend.Services
{
    public class OnboardingService : IOnboardingService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public OnboardingService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<AuthResponseDto> SetupOrganizationAsync(OnboardingDto dto)
        {
            var org = new Organization
            {
                Name = dto.OrganizationName,
                Domain = dto.OrganizationDomain,
                Description = dto.OrganizationDescription
            };
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();

            var admin = new User
            {
                FirstName = dto.AdminFirstName,
                LastName = dto.AdminLastName,
                Email = dto.AdminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.AdminPassword),
                Role = UserRole.Admin,
                OrganizationId = org.Id
            };
            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = GenerateToken(admin),
                UserId = admin.Id,
                FullName = admin.FullName,
                Email = admin.Email,
                Role = admin.Role.ToString(),
                OrganizationId = org.Id,
                OrganizationName = org.Name
            };
        }

        public async Task<User> InviteUserAsync(int organizationId, InviteUserDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new InvalidOperationException("A user with this email already exists.");

            if (!Enum.TryParse<UserRole>(dto.Role, out var role))
                role = UserRole.Agent;

            var tempPassword = Guid.NewGuid().ToString("N")[..8];
            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                Role = role,
                OrganizationId = organizationId
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<List<User>> GetTeamMembersAsync(int organizationId)
        {
            return await _context.Users
                .Where(u => u.OrganizationId == organizationId && u.IsActive)
                .OrderBy(u => u.FirstName)
                .ToListAsync();
        }

        private string GenerateToken(User user)
        {
            var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("OrganizationId", user.OrganizationId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
