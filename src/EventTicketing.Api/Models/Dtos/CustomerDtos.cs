using System.ComponentModel.DataAnnotations;

namespace EventTicketing.Models.Dtos;

public record CreateCustomerRequest(
    [Required, StringLength(200)] string FullName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Phone, StringLength(40)] string? Phone);

public record UpdateCustomerRequest(
    [Required, StringLength(200)] string FullName,
    [Phone, StringLength(40)] string? Phone);

public record CustomerResponse(
    int Id,
    string FullName,
    string Email,
    string? Phone,
    DateTime CreatedAtUtc);
