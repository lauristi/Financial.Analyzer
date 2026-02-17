namespace Server.Api.Domain.Service.InfrastrutureService.Interface
{
    public interface IHttpClientService
    {
        Task<HttpResponseMessage> GetAsync(string url);
    }
}