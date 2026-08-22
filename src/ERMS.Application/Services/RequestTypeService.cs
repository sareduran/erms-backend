using ERMS.Application.DTOs;
using ERMS.Application.Interfaces;

namespace ERMS.Application.Services;

/// <summary>
/// Talep türü okuma işini API katmanından ayırır. Controller repository'yi bilmez;
/// yalnızca Application katmanındaki bu kullanım senaryosunu çağırır.
/// </summary>
public sealed class RequestTypeService(IErmsRepository repository) : IRequestTypeService
{
    public async Task<IReadOnlyList<RequestTypeDto>> ListActiveAsync(CancellationToken ct) =>
        (await repository.GetRequestTypesAsync(activeOnly: true, ct))
        .Select(type => new RequestTypeDto(type.Id, type.Name, type.RequiresApproval, type.IsActive))
        .ToList();
}
