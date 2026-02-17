/**
 * Realiza o download de um arquivo no navegador a partir de uma string Base64.
 * Este método utiliza a técnica de criação de Blobs para garantir eficiência
 * e compatibilidade com arquivos de diferentes tamanhos.
 * * @param {string} fileName - O nome que o arquivo terá ao ser salvo (ex: "extrato.xlsx").
 * @param {string} base64String - O conteúdo do arquivo codificado em Base64 enviado pelo Servidor.
 */
export function downloadFileFromBytes(fileName, base64String) {
    // 1. Decodifica a string Base64 em uma string de caracteres binários.
    // atob() é uma função nativa do navegador que reverte a codificação Base64.
    const byteCharacters = atob(base64String);

    // 2. Cria um array numérico para armazenar os códigos de cada caractere.
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        // Converte cada caractere para seu valor numérico correspondente (0-255).
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }

    // 3. Converte o array numérico em um TypedArray de bytes reais (8-bit unsigned integers).
    // Isso é necessário porque o objeto Blob exige um formato de dados binários puro.
    const byteArray = new Uint8Array(byteNumbers);

    // 4. Cria um objeto Blob (Binary Large Object) que representa os dados do arquivo.
    // O tipo 'application/octet-stream' indica que é um arquivo binário genérico.
    const blob = new Blob([byteArray], { type: "application/octet-stream" });

    // 5. Gera uma URL temporária apontando para o objeto Blob na memória do navegador.
    // Esta URL começa com 'blob:' e permite que o navegador acesse os dados como se fossem um arquivo real.
    const url = URL.createObjectURL(blob);

    // 6. Cria um elemento de link (<a>) oculto para simular o comportamento de download.
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName; // Define o nome do arquivo que aparecerá para o usuário.

    // 7. Adiciona o link ao DOM, simula o clique e o remove logo em seguida.
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);

    // 8. IMPORTANTE: Libera a URL temporária da memória do navegador.
    // Como os Blobs podem ser grandes, revogar a URL evita vazamentos de memória (memory leaks).
    URL.revokeObjectURL(url);
}