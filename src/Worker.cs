using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Proto;

namespace TestProtoActor
{
    public class Worker : BackgroundService
    {
        private readonly IActorManager _actorManager;
        private readonly ILogger<Worker> _logger;

        public Worker(IActorManager actorManager, ILogger<Worker> logger)
        {
            _actorManager = actorManager ?? throw new ArgumentNullException(nameof(actorManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            InitializeActors();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private void InitializeActors()
        {
            _actorManager.Activate();
        }
    }

    public class ActorManager : IActorManager
    {
        private readonly RootContext _context = new RootContext();
        private readonly IActorFactory _actorFactory;

        public ActorManager(IActorFactory actorFactory, ILogger<ActorManager> logger)
        {
            _actorFactory = actorFactory;
        }

        public void Activate()
        {
            var actorPing = _actorFactory.GetActor<PingActor>();
            //var actorPong = _actorFactory.GetActor<PongActor>();
    
            _context.Send(actorPing, new Ping(1));
        }
    }

    public interface IActorManager
    {
        void Activate();
    }

    public class Ping
    {
        public Ping(int iteration)
        {
            Message = $"Player #{iteration} ping";
            Iteration = iteration;
        }

        public string Message { get; }
        public int Iteration { get; }
    }

    public class Pong
    {
        public Pong(int iteration)
        {
            Message = $"Player #{iteration} pong";
            Iteration = iteration;
        }

        public string Message { get; }
        public int Iteration { get; }
    }

    public class PingActor : IActor
    {
        private readonly IActorFactory actorFactory;

        public PingActor(IActorFactory actorFactory, ILogger<PingActor> logger)
        {
            this.actorFactory = actorFactory;
            Logger = logger;
        }

        public ILogger<PingActor> Logger { get; }

        public async Task ReceiveAsync(IContext context)
        {
            var msg = context.Message;
            if (msg is Ping p)
            {
                Console.WriteLine($"Hello {p.Message} {GetHashCode()}");
                if (p.Iteration < 100)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    context.Send(this.actorFactory.GetActor<PongActor>(), new Pong(p.Iteration));
                }
            }
        }
    }

    public class PongActor : IActor
    {
        public PongActor(IActorFactory actorFactory, ILogger<PongActor> logger)
        {
            this.actorFactory = actorFactory;
            Logger = logger;
        }

        private IActorFactory actorFactory;

        public ILogger<PongActor> Logger { get; }

        public async Task ReceiveAsync(IContext context)
        {
            var msg = context.Message;
            if (msg is Pong p)
            {
                Console.WriteLine($"Hello {p.Message} {GetHashCode()}");
                if (p.Iteration < 100)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    context.Send(actorFactory.GetActor<PingActor>(), new Ping(p.Iteration + 1));
                }
            }
        }
    }
}
