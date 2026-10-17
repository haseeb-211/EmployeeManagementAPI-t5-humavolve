using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Api.Dtos;

public class UpdateEmployeeDto
{
    [Required(ErrorMessage = "FullName is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "FullName must be between 1 and 200 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [StringLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Department must be between 1 and 100 characters.")]
    public string Department { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335",
        ErrorMessage = "Salary must be greater than or equal to zero.")]
    public decimal Salary { get; set; }
}
