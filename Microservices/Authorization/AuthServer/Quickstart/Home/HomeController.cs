// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using AuthServer.Services;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IdentityServer4.Quickstart.UI
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _environment;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly ILogger _logger;

        public HomeController(
            IIdentityServerInteractionService interaction,
            IWebHostEnvironment environment,
            ILogger<HomeController> logger,
            IConfiguration config
        )
        {
            _interaction = interaction;
            _environment = environment;
            _logger = logger;
            _config = config;
        }

        public IActionResult Index()
        {
            if (_environment.IsDevelopment() || _environment.IsEnvironment("MyLocal") || _environment.IsEnvironment("Local"))
            {
                // only show in development
                return View();
            }

            _logger.LogInformation("Homepage is disabled in production. Returning 404.");

            return NotFound();
        }

        [HttpGet("services/urls")]
        public IActionResult GetServiceUrls()
        {
            var result = ServiceUrlInfoProvider.GetExternalUrlConfiguration(_config);

            return Ok(result);
        }

        /// <summary>
        /// Shows the error page
        /// </summary>
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            // retrieve error details from identity server
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;

                if (!_environment.IsDevelopment())
                {
                    // only show in development
                    message.ErrorDescription = null;
                }
            }

            return View("Error", vm);
        }
    }
}