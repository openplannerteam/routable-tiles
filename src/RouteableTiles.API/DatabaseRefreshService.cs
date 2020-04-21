using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace RouteableTiles.API
{
    internal class DatabaseRefreshService : IHostedService
    {
        private Timer _timer;
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Log.Information($"{nameof(DatabaseRefreshService)} started!");
            
            _timer = new Timer(TryRefresh, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(10));
            
            return Task.CompletedTask;
        }
        
        private void TryRefresh(object state)
        {
            if (DatabaseInstance.Default == null) return;

            if (DatabaseInstance.Default.TryReload())
            {
                Log.Information("Database reloaded.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information($"{nameof(DatabaseRefreshService)} stopped!");
            
            return Task.CompletedTask;
        }
    }
}