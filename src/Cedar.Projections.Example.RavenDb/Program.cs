namespace Cedar.Projections.Example.RavenDb
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Cedar.Projections.Example.RavenDb.Handlers;
    using Cedar.Projections.Example.RavenDb.Logging;
    using EventStore.ClientAPI;
    using EventStore.ClientAPI.Embedded;
    using EventStore.Core;
    using EventStore.Core.Data;
    using Raven.Client;
    using Raven.Client.Embedded;
    using Serilog;
    using ExpectedVersion = EventStore.ClientAPI.ExpectedVersion;

    internal class Program : IDisposable
    {
        private static readonly string[] s_skus;
        private static readonly DeterministicGuidGenerator s_idGenerator;
        private static readonly ILog s_log;
        private readonly IEventStoreConnection _connection;
        private readonly IDocumentStore _documentStore;

        static Program()
        {
            s_log = LogProvider.For<Program>();

            s_idGenerator = new DeterministicGuidGenerator(Guid.Parse("BC6C9F86-2594-4106-A85E-438D04C63AAF"));
            s_skus =
                @"hellenised
prodromia
report
depositional
scarier
gleanings
sauce
lutyens
nondescriptive
loma
unencumbered
scatomata
porphyrogenite
mors
militancy
circumambulatory
amphibrach
nonsatirizing
clips
pectise
metrist
preliberate
angelus
jocoseness
facial
kitchenmaid
cercis
ferocity
armillae
spam
overstimulative
androcratic
charlene
ourself
douse
unexperiential
segno
according
undiametric
sarcoenchondromata
neurocoele
czerny
those
perfidious
conveyorizer
ockham
pseudoscientific
blaster
musagetes
isosceles
depauperate
dyscrasial
puseyism
frankish
overobject
spanworm
ceratodus
germinator
moody
dimissorial
suborganically
stipple
suppï¿¥ï¾½
forfeit
depletive
cord
monohydroxy
haick
hesperinos
circumventive
trenail
unfaced
corrientes
repairman
ibises
baltassar
fijian
wickless
forenoon
figurant
benthon
victimize
unmustered
unfigurative
aggrieved
bouncy
unclean
preendorsement
nonequatorial
freebooty
dissimulative
uglily
superexceptional
toxicant
illiberalness
minuteness
devotionalness
flapperdom
wilco
disembogued".Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public Program()
        {
            _connection = CreateConnection().Result;
            _documentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true,
                Conventions =
                {
                    //AllowMultipuleAsyncOperations = true
                }
            }.Initialize();
        }

        private async Task Init()
        {
            await SeedEvents(_connection);
        }

        private async Task SeedEvents(IEventStoreConnection connection)
        {
            var max = 100000;
            var rand = new Random();
            var identityMap = s_skus.ToDictionary(x => x, s_idGenerator.Create);

            for(var i = 0; i < max; i++)
            {
                var index = rand.Next(0, s_skus.Length - 1);
                var sku = s_skus[index];
                var id = identityMap[sku];

                var streamId = "inventory-" + id.ToString("n");

                var @event = rand.Next(0, 2)%3 == 0
                    ? (object)new InventoryCheckedIn(id, sku, rand.Next(10, 500))
                    : new InventoryCheckedOut(id, sku, rand.Next(1, 50));

                await
                    connection.AppendToStreamAsync(streamId,
                        ExpectedVersion.Any,
                        @event.SerializeEventData(Guid.NewGuid()));
            }

            s_log.Info("Seeded events.");
        }

        private static Task<IEventStoreConnection> CreateConnection()
        {
            var notListening = new IPEndPoint(IPAddress.None, 0);
            ClusterVNode node = EmbeddedVNodeBuilder.AsSingleNode()
                .RunInMemory()
                .WithExternalHttpOn(notListening)
                .WithInternalHttpOn(notListening)
                .WithExternalTcpOn(notListening)
                .WithInternalTcpOn(notListening);

            var source = new TaskCompletionSource<IEventStoreConnection>();

            node.NodeStatusChanged += (_, e) =>
            {
                if(e.NewVNodeState != VNodeState.Master)
                {
                    return;
                }

                source.SetResult(EmbeddedEventStoreConnection.Create(node));
            };

            node.Start();

            return source.Task;
        }

        public Task StartProjections()
        {
            var projections = new Projections(_connection, _documentStore.OpenAsyncSession);

            return projections.Start();
        }

        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .CreateLogger();

            using(var program = new Program())
            {
                program.Init().Wait();

                program.StartProjections().Wait();

                while(true)
                {
                    Console.WriteLine("Press r for a report.");
                    if(Console.ReadKey(true).KeyChar == 'r')
                    {
                        program.PrintReport().Wait();
                    }
                }
            }

            return 0;
        }

        private async Task PrintReport()
        {
            using(var session = _documentStore.OpenAsyncSession())
            {
                var results = await session.Query<InventoryItemView>()
                    .OrderBy(x => x.Sku)
                    .ToListAsync();

                Console.WriteLine(results.Aggregate(new StringBuilder(), ((builder, item) => builder.AppendFormat("{0}: {1}", item.Sku, item.Quantity).AppendLine())).ToString());
            }
        }

        public void Dispose()
        {
            _documentStore.Dispose();
        }
    }
}