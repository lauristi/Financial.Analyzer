namespace Server.Api.Domain.Service.SharedService.Interface
{
    public interface ICrypto
    {
        string Encrypt(string input);

        string Decrypt(string input);
    }
}