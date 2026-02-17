//window torna a funcao downloadFile globale acessivel em qualquer parte do codigo
window.downloadService = {
    downloadFile: function (url, fileName) {
        // Cria um elemento <a> para fazer o download
        var downloadLink = document.createElement('a');
        downloadLink.href = url;
        downloadLink.download = fileName;

        // Adiciona o elemento ao corpo do documento
        document.body.appendChild(downloadLink);

        // Simula o clique no link para iniciar o download
        downloadLink.click();

        // Remove o elemento do corpo do documento após o download
        document.body.removeChild(downloadLink);
    }
};

//limpa o INPUTFILE depois do download
window.clearInputFile = (inputId) => {
    const input = document.getElementById(inputId);
    if (input) {
        input.value = null;
    }
};