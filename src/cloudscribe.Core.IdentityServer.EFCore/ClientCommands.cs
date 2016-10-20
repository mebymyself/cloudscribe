﻿
using cloudscribe.Core.IdentityServer.EFCore.Interfaces;
using cloudscribe.Core.IdentityServer.EFCore.Mappers;
using cloudscribe.Core.IdentityServerIntegration.Storage;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace cloudscribe.Core.IdentityServer.EFCore
{
    public class ClientCommands : IClientCommands
    {
        public ClientCommands(
            IConfigurationDbContext context
            )
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            this.context = context;
        }

        private readonly IConfigurationDbContext context;

        public async Task CreateClient(string siteId, Client client, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ent = client.ToEntity();
            ent.SiteId = siteId;
            context.Clients.Add(ent);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task UpdateClient(string siteId, Client client, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ent = client.ToEntity();
            ent.SiteId = siteId;
            context.Clients.Update(ent);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task DeleteClient(string siteId, string clientId, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var client = context.Clients
                .Include(x => x.AllowedGrantTypes)
                .Include(x => x.RedirectUris)
                .Include(x => x.PostLogoutRedirectUris)
                .Include(x => x.AllowedScopes)
                .Include(x => x.ClientSecrets)
                .Include(x => x.Claims)
                .Include(x => x.IdentityProviderRestrictions)
                .Include(x => x.AllowedCorsOrigins)
                .FirstOrDefault(x => x.SiteId == siteId && x.ClientId == clientId);

            context.Clients.Remove(client);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

    }
}