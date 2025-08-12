using System.Net;
using System.Text.Json;

namespace LocalizationManager.Web.Exceptions;

public class BadRequestException : Exception
{
    public HttpStatusCode StatusCode => HttpStatusCode.BadRequest;
    public BadRequestException(string message) : base(message) { }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

[Serializable]
public class InternalServerErrorException : Exception
{
    public HttpStatusCode StatusCode => HttpStatusCode.InternalServerError;
    public InternalServerErrorException(string message) : base(message) { }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

