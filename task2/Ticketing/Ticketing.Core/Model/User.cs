using Microsoft.AspNetCore.Identity;

namespace Ticketing.Core.Model;

public class User : IdentityUser
{
  string? FirstName { get; set; }
  string? LastName { get; set; }
  string? Address { get; set; }
  string? IdentificationNumber { get; set; }
}
