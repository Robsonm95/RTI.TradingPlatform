using RTI.OrderGenerator.DTOs;
using RTI.Shared.Constants;

namespace RTI.OrderGenerator.Validation;

public static class CreateOrderValidator
{
    public static List<string> Validate(CreateOrderRequest request)
    {
        var errors = new List<string>();

        if (!Symbols.All.Contains(request.Symbol))
        {
            errors.Add("Invalid symbol");
        }

        if (request.Quantity <= 0 ||
            request.Quantity >= 100000)
        {
            errors.Add("Invalid quantity");
        }

        if (request.Price <= 0 ||
            request.Price >= 1000)
        {
            errors.Add("Invalid price");
        }

        if (request.Price % 0.01m != 0)
        {
            errors.Add("Price must be multiple of 0.01");
        }

        return errors;
    }
}