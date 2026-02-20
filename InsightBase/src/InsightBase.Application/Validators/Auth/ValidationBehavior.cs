using FluentValidation;
using InsightBase.Application.DTOs.Auth;
using MediatR;

namespace InsightBase.Application.Validators.Auth
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if(_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(
                        _validators.Select(v => v.ValidateAsync(context, cancellationToken))
                );
                var failures = validationResults
                    .SelectMany(r => r.Errors)
                    .Where(f => f != null)
                    .ToList();
                
                if(failures.Count != 0)
                {
                    var errorMessages = failures.Select(f => f.ErrorMessage);

                    if(typeof(TResponse) == typeof(AuthResponse))
                    {
                        return (TResponse)(object)AuthResponse.Fail(errorMessages);
                    }
                    throw new ValidationException(failures);
                }
            }
            return await next();
        }

    }
}