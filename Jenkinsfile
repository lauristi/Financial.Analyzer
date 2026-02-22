pipeline {
    agent any

    environment {
        API_NAME = 'financial-api'
        WEB_NAME = 'financial-web'
        PROJECT_LABEL = 'financial-analyzer'
        // Definimos a porta como vari�vel para facilitar manuten��o
        API_PORT = '5020'
    }

    stages {
        stage('01- Checkout') {
            steps {
                // Baixa o c�digo do reposit�rio
                checkout scm
            }
        }

        stage('02- Build & Deploy API') {
            steps {
                script {
                    // 1. Build da imagem: O ponto (.) indica que o contexto � a raiz
                    sh "docker build -t ${API_NAME}:latest -f Dockerfile ."
                    
                    // 2. Parada e remo��o segura do container anterior
                    // O '|| true' evita que o Jenkins falhe se o container n�o existir
                    sh "docker stop ${API_NAME} || true && docker rm ${API_NAME} || true"
                    
                    // 3. Execu��o do novo container
                    // Note que mantivemos o label para integra��o com o Portainer
                    sh """
                        docker run -d \
                        --name ${API_NAME} \
                        --network proxy_network \
                        --label "com.docker.compose.project=${env.PROJECT_LABEL}" \
                        -p ${API_PORT}:${API_PORT} \
                        -v /home/sysdba/financial_data/Expenses:/app/Expenses \
                        -v /home/sysdba/financial_data/Statement:/app/Statement \
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
            // Limpa o workspace para n�o ocupar espa�o em disco no servidor Jenkins
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