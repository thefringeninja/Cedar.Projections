namespace Cedar.Projections.Example.RavenDb.RavenDb
{
    using System.Threading.Tasks;

    internal interface IAsyncObserver<in T>
    {
        Task OnNext(T next);

        Task OnCompleted();
    }
}