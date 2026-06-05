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
                // Baixa o código do repositório principal
                checkout scm
            
                // Inicializa e atualiza os submódulos de forma recursiva
                sh 'git submodule update --init --recursive'
            }
        }

        stage('02- Build & Deploy API') {
            steps {
                script {
                    // 1. Build da imagem: O ponto (.) indica que o contexto é a raiz
                    sh "docker build --no-cache -t ${API_NAME}:latest -f Dockerfile ."
                    
                    // 2. Parada e remoção segura do container anterior
                    // O '|| true' evita que o Jenkins falhe se o container não existir
                    sh "docker stop ${API_NAME} || true && docker rm ${API_NAME} || true"
                    
                    // 3. Execução do novo container
                    // Note que mantivemos o label para integração com o Portainer
                    sh """
                        docker run -d \
                        --name ${API_NAME} \
                        --network proxy_network \
                        --label "com.docker.compose.project=${env.PROJECT_LABEL}" \
                        -p ${API_PORT}:${API_PORT} \
                        -v /mnt/docker-data/Apps/FinancialAnalyzer/Expenses:/app/Expenses \
                        -v /mnt/docker-data/Apps/FinancialAnalyzer/Statements:/app/Statements \
                        --restart unless-stopped \
                        ${API_NAME}:latest
                    """
                }
            }
        }

        stage('03- Build & Deploy Web') {
            steps {
                script {
                    // Gera a imagem do Frontend
                    sh "docker build -t financial-web:latest -f Dockerfile.web ."
                    
                    // Remove versão anterior se existir
                    sh "docker stop financial-web || true"
                    sh "docker rm financial-web || true"
                    
                    // Inicia o container na porta 5021 e conecta na rede do Proxy
                    sh "docker run -d --name financial-web --network proxy_network -p 5021:5021 --restart unless-stopped financial-web:latest"
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