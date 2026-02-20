using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentValidation;
using InsightBase.Application.Validators.Auth;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InsightBase.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // MediatR (Command/Query handler'ları için)
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            // Diğer Application-level servis kayıtları (Validators, Mappers interfaces) burada olur
            // services.AddTransient<IYourService, YourService>();
            return services;
        }
        
    }
}