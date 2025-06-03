using System.Collections.Concurrent;
using XRCultureMiddleware.Pages;

namespace XRCultureMiddleware
{
    public interface IOperation
    {
        Guid OperationId { get; }
    }

    public interface IOperationTransient : IOperation
    {
    }

    public interface IOperationScoped : IOperation
    {
    }

    public interface IOperationSingleton : IOperation
    {
        public bool Started { get; set; }        
    }

    public interface IOperationSingletonInstance : IOperation
    {
        public ConcurrentDictionary<string, Viewer> Viewers { get; }
    }

    public class Operation : IOperationTransient, IOperationScoped, IOperationSingleton, IOperationSingletonInstance
    {
        Guid _guid;
        ConcurrentDictionary<string, Viewer> _viewers = new();
        public Operation() : this(Guid.NewGuid())
        {

        }

        public Operation(Guid guid)
        {
            _guid = guid;
        }

        public Guid OperationId => _guid;

        public bool Started { get; set; } = false;

        ConcurrentDictionary<string, Viewer> IOperationSingletonInstance.Viewers => _viewers;
    }

    public class OperationService
    {
        public IOperationTransient TransientOperation { get; }
        public IOperationScoped ScopedOperation { get; }
        public IOperationSingleton SingletonOperation { get; }
        public IOperationSingletonInstance SingletonInstanceOperation { get; }

        public OperationService(IOperationTransient transientOperation,
            IOperationScoped scopedOperation,
            IOperationSingleton singletonOperation,
            IOperationSingletonInstance instanceOperation)
        {
            TransientOperation = transientOperation;
            ScopedOperation = scopedOperation;
            SingletonOperation = singletonOperation;
            SingletonInstanceOperation = instanceOperation;
        }
    }

    public class Viewer
    {
        public string EndPoint { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public string XmlDefinition { get; set; }
    }
}
