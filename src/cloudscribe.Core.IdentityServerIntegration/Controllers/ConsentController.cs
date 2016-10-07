﻿// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer4.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using cloudscribe.Core.IdentityServerIntegration.Models;
using cloudscribe.Core.Models;
using cloudscribe.Core.Identity;

namespace cloudscribe.Core.IdentityServerIntegration
{
    /// <summary>
    /// This controller implements the consent logic
    /// </summary>
    public class ConsentController : Controller
    {
        private readonly ILogger<ConsentController> _logger;
        private readonly IClientStore _clientStore;
        private readonly IScopeStore _scopeStore;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IIdentityServerIntegration identityServerIntegration;
        private readonly SiteSettings _site;

        public ConsentController(
            ILogger<ConsentController> logger,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IScopeStore scopeStore,
            IIdentityServerIntegration identityServerIntegration,
            SiteSettings currentSite
            )
        {
            _logger = logger;
            _interaction = interaction;
            _clientStore = clientStore;
            _scopeStore = scopeStore;
            _site = currentSite;
            this.identityServerIntegration = identityServerIntegration;
        }

        
        /// <summary>
        /// Shows the consent screen
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index(string returnUrl)
        {
            returnUrl = identityServerIntegration.EnsureFolderSegmentIfNeeded(_site, returnUrl);
            var vm = await BuildViewModelAsync(returnUrl);
            if (vm != null)
            {
                return View("Index", vm);
            }

            return View("Error");
        }

        /// <summary>
        /// Handles the consent screen postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ConsentInputModel model)
        {
            // parse the return URL back to an AuthorizeRequest object
            var returnUrl = identityServerIntegration.EnsureFolderSegmentIfNeeded(_site, model.ReturnUrl);
            var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
            ConsentResponse response = null;

            // user clicked 'no' - send back the standard 'access_denied' response
            if (model.Button == "no")
            {
                response = ConsentResponse.Denied;
            }
            // user clicked 'yes' - validate the data
            else if (model.Button == "yes" && model != null)
            {
                // if the user consented to some scope, build the response model
                if (model.ScopesConsented != null && model.ScopesConsented.Any())
                {
                    response = new ConsentResponse
                    {
                        RememberConsent = model.RememberConsent,
                        ScopesConsented = model.ScopesConsented
                    };
                }
                else
                {
                    ModelState.AddModelError("", "You must pick at least one permission.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Invalid Selection");
            }

            if (response != null)
            {
                // communicate outcome of consent back to identityserver
                await _interaction.GrantConsentAsync(request, response);

                // redirect back to authorization endpoint
                return Redirect(returnUrl);
            }

            var vm = await BuildViewModelAsync(returnUrl, model);
            if (vm != null)
            {
                return View("Index", vm);
            }

            return View("Error");
        }

        async Task<ConsentViewModel> BuildViewModelAsync(string returnUrl, ConsentInputModel model = null)
        {
            var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (request != null)
            {
                var client = await _clientStore.FindClientByIdAsync(request.ClientId);
                if (client != null)
                {
                    var scopes = await _scopeStore.FindScopesAsync(request.ScopesRequested);
                    if (scopes != null && scopes.Any())
                    {
                        return new ConsentViewModel(model, returnUrl, request, client, scopes);
                    }
                    else
                    {
                        _logger.LogError("No scopes matching: {0}", request.ScopesRequested.Aggregate((x, y) => x + ", " + y));
                    }
                }
                else
                {
                    _logger.LogError("Invalid client id: {0}", request.ClientId);
                }
            }
            else
            {
                _logger.LogError("No consent request matching request: {0}", returnUrl);
            }

            return null;
        }
    }

}