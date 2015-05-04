namespace Cedar.Projections
{
    using System.Threading;
    using System.Threading.Tasks;

    public delegate Task Handler<TMessage>(EventMessage<TMessage> eventMessage, CancellationToken ct)
        where TMessage: class;
}