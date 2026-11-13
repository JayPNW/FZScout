using System.Net;

namespace FZScout.Tests.Site;

/// <summary>Stands in for the network: answers every request with one canned response, or throws one exception.</summary>
class StubHttpHandler : HttpMessageHandler
{
    readonly HttpStatusCode _status;
    readonly string _body;
    readonly Exception? _exception;

    public StubHttpHandler(HttpStatusCode httpStatusCode, string body)
    {
        this._status = httpStatusCode;
        this._body = body;
    }

    public StubHttpHandler(Exception exception)
        : this(HttpStatusCode.OK, string.Empty)
    {
        this._exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (this._exception is not null)
        {
            throw this._exception;
        }

        return Task.FromResult(new HttpResponseMessage(this._status) { Content = new StringContent(this._body) });
    }
}
