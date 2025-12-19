using Expenses.API.Domain.Transaction.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Expenses.API.Framework.Extensions {
    public static class DependencyInjection {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services) {

            services.AddScoped<TransactionService, TransactionServiceImpl>();

            return services;
        }
    }
}
