pipeline {
    agent any

    environment {
        // Nomes das Imagens e Containers
        API_NAME = 'financial-api'
        WEB_NAME = 'financial-web'
    }

    stages {
        stage('01- Checkout') {
            steps {
                checkout scm
            }
        }

        stage('02- Build & Deploy API') {
            steps {
                script {
                    // Build da imagem usando o arquivo de solução como contexto
                    sh "docker build -t ${API_NAME}:latest -f Api/Server.Api/Dockerfile ."
                    
                    // Substituição do container antigo
                    sh "docker stop ${API_NAME} || true && docker rm ${API_NAME} || true"
                    sh "docker run -d --name ${API_NAME} -p 5020:5020 --restart unless-stopped ${API_NAME}:latest"
                }
            }
        }

        stage('03- Build & Deploy Web') {
            steps {
                script {
                    // Build da Web (Blazor)
                    sh "docker build -t ${WEB_NAME}:latest -f Web/Server.Web/Dockerfile ."
                    
                    // Substituição do container antigo
                    sh "docker stop ${WEB_NAME} || true && docker rm ${WEB_NAME} || true"
                    sh "docker run -d --name ${WEB_NAME} -p 5023:5023 --restart unless-stopped ${WEB_NAME}:latest"
                }
            }
        }
    }

    post {
        always {
            cleanWs() // Limpa o workspace para economizar espaço no Lenovo
        }
    }
}