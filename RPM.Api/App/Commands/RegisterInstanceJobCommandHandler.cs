using MediatR;

namespace RPM.Api.App.Commands;

public class RegisterInstanceJobCommandHandler : IRequest<int>
{
    public long CredId { get; set; }
    public long AccountId { get; set; }

}