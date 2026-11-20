using Myapp.Server.Api;
using Suwgate.Architecture.Profiles;
using Suwgate.Database.Profiles;

namespace Myapp.Gate;

public sealed class GuardTests
{
    [Fact]
    public void Every_guard_of_suwgate_passes() =>
        SuwgateGuards.VerifyAll(
            new GuardInputs
            {
                Production = [typeof(ServerApp).Assembly],
                Tests = [typeof(GuardTests).Assembly],
                Server = typeof(ServerApp),
            },
            new DatabaseProfile()
        );
}
