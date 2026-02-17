namespace Server.Web.Service.Interface
{
    public interface IFileService
    {
        /// <summary>
        /// Realiza o download de um arquivo no lado do cliente a partir de um array de bytes.
        /// </summary>
        /// <param name="fileName">Nome do arquivo com extensão.</param>
        /// <param name="fileBytes">Conteúdo binário do arquivo.</param>
        Task DownloadFileByteAsync(string fileName, byte[] fileBytes);
    }
}