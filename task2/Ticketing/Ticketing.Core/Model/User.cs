using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Ticketing.Core.Model;

public class User : IdentityUser
{
  [MaxLength(256)]
  public string? FirstName { get; set; }
  [MaxLength(256)]
  public string? LastName { get; set; }
  [MaxLength(256)]
  public string? Address { get; set; }
  [MaxLength(256)]
  public string? IdentificationNumber { get; set; }
}
