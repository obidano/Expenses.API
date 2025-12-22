using Expenses.API.Domain.Transaction.Services;
using Expenses.API.Domain.Ussd.Handlers;
using Expenses.API.Domain.Ussd.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Expenses.API.Framework.Extensions {
    public static class DependencyInjection {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services) {

            services.AddScoped<TransactionService, TransactionServiceImpl>();

            // Register USSD services
            services.AddScoped<UssdStateService, UssdStateServiceImpl>();
            
            // Register MainMenuHandler (router for all menus and submenus)
            services.AddScoped<MainMenuHandler>();

            return services;
        }
    }
}
