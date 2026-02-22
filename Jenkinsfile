pipeline {
    agent any

    environment {
        API_NAME = 'financial-api'
        WEB_NAME = 'financial-web'
        PROJECT_LABEL = 'financial-analyzer'
        // Definimos a porta como variável para facilitar manutenção
        API_PORT = '5020'
    }

    stages {
        stage('01- Checkout') {
            steps {
                // Baixa o código do repositório
                checkout scm
            }
        }

        stage('02- Build & Deploy API') {
            steps {
                script {
                    // 1. Build da imagem: O ponto (.) indica que o contexto é a raiz
                    sh "docker build -t ${API_NAME}:latest -f Dockerfile ."
                    
                    // 2. Parada e remoção segura do container anterior
                    // O '|| true' evita que o Jenkins falhe se o container não existir
                    sh "docker stop ${API_NAME} || true && docker rm ${API_NAME} || true"
                    
                    // 3. Execução do novo container
                    // Note que mantivemos o label para integração com o Portainer
                    sh """
                        docker run -d \
                        --name ${API_NAME} \
                        --label "com.docker.compose.project=${env.PROJECT_LABEL}" \
                        -p ${API_PORT}:${API_PORT} \
                        --restart unless-stopped \
                        ${API_NAME}:latest
                    """
                }
            }
        }

        stage('03- Build & Deploy Web (Placeholder)') {
            steps {
                script {
                    // Este bloco será preenchido quando criarmos o Dockerfile.web
                    echo "Aguardando definição do Dockerfile.web para o projeto Server.Web"
                }
            }
        }
    }

    post {
        always {
            // Limpa o workspace para não ocupar espaço em disco no servidor Jenkins
            cleanWs()
        }
        success {
            echo "Deploy da API realizado com sucesso, Hal."
        }
        failure {
            echo "Falha no deploy. Verifique os logs do Docker acima."
        }
    }
}