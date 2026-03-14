using System.Threading;
using System.Threading.Tasks;
using Av.Engine;

namespace Av.Agent;

public interface IUiAlertSink
{
    Task PublishBehaviorAlertAsync(BehaviorAssessment assessment, CancellationToken cancellationToken);
}

public interface IQuarantineSink
{
    Task RequestQuarantineAsync(BehaviorAssessment assessment, CancellationToken cancellationToken);
}
