using FluentValidation;
using InsightBase.Application.Commands.Auth;
namespace InsightBase.Application.Validators.Auth
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Request.Email)
                .NotEmpty().WithMessage("Email adresi boş olamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");

            RuleFor(x => x.Request.Password)
                .NotEmpty().WithMessage("Şifre boş olamaz.")
                .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
                .Matches(@"[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
                .Matches(@"[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
                .Matches(@"[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")
                .Matches(@"[\W]").WithMessage("Şifre en az bir özel karakter içermelidir.");

            // Şifre onaylama kontrolü (Business Logic Validation)
            RuleFor(x => x.Request.ConfirmPassword)
                .Equal(x => x.Request.Password).WithMessage("Şifreler birbiriyle eşleşmiyor.");

            RuleFor(x => x.Request.FullName)
                .MinimumLength(2).WithMessage("Ad Soyad en az 2 karakter olmalıdır.")
                .MaximumLength(100).WithMessage("Ad Soyad 100 karakterden uzun olamaz.");
        }
    }
}
