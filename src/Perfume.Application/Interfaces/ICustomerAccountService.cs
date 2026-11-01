using Perfume.Application.DTOs;

namespace Perfume.Application.Interfaces;

public interface ICustomerAccountService
{
    Task<AccountOperationResultDto> RegisterCustomerAsync(CustomerRegisterRequest request, CancellationToken cancellationToken = default);
    Task<AccountOperationResultDto> AuthenticateCustomerAsync(CustomerLoginRequest request, CancellationToken cancellationToken = default);
    Task<CustomerProfileDto?> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task<AccountOperationResultDto> UpdateProfileAsync(string userId, CustomerProfileUpdateRequest request, CancellationToken cancellationToken = default);
    bool ValidatePasswordPolicy(string? password, out List<string> errors);
}
