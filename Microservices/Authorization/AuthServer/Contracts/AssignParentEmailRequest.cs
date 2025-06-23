using FluentValidation;

namespace AuthServer.Contracts
{
    public class VerifyParentEmailRequest
    {
        public string ParentEmail { get; set; }
    }

    public class AssignParentEmailRequest
    {
        public string ParentEmail { get; set; }

        public string VerificationCode { get; set; }
    }

    public class AssignParentEmailRequestValidator : AbstractValidator<VerifyParentEmailRequest>
    {
        public AssignParentEmailRequestValidator()
        {
            RuleFor(e => e.ParentEmail).NotNull().NotEmpty().EmailAddress();
        }
    }

    public class VerifyParentEmailRequestValidator : AbstractValidator<AssignParentEmailRequest>
    {
        public VerifyParentEmailRequestValidator()
        {
            RuleFor(e => e.ParentEmail).NotNull().NotEmpty().EmailAddress();
            RuleFor(e => e.VerificationCode).NotNull().NotEmpty();
        }
    }
}