using CyNewsCorner.Requests;
using FluentValidation;

namespace CyNewsCorner
{
    public class GetPostListRequestValidator : AbstractValidator<GetPostListRequest>
    {
        public GetPostListRequestValidator()
        {
            RuleFor(x => x.SelectedNewsSources)
                .ForEach(s => s.GreaterThan(0)
                .WithMessage("SelectedNewsSources value {PropertyValue} should be greater than 0")
                );
        }
    }
}