namespace DreamLens.Api.Infrastructure.Monetization;

public interface IEntitlementService
{
    EntitlementSnapshot GetEntitlement(string userSubject);
}
