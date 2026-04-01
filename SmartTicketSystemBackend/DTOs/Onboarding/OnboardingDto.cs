using System.ComponentModel.DataAnnotations;

namespace SmartTicketSystemBackend.DTOs.Onboarding
{
    public class OnboardingDto
    {
        // Organization info
        [Required, MaxLength(200)]
        public string OrganizationName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? OrganizationDomain { get; set; }

        [MaxLength(500)]
        public string? OrganizationDescription { get; set; }

        // Admin user info
        [Required, MaxLength(100)]
        public string AdminFirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string AdminLastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string AdminPassword { get; set; } = string.Empty;
    }

    public class InviteUserDto
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Agent";
    }
}
