pipeline {
    agent any

    environment {
        API_NAME = 'financial-api'
        WEB_NAME = 'financial-web'
        PROJECT_LABEL = 'financial-analyzer'
        API_PORT = '5020'
        WEB_PORT = '5021'
        DATA_PATH = '/mnt/docker-data/Apps/FinancialAnalyzer'
    }

    stages {
        stage('01- Checkout') {
            steps {
                checkout scm
                sh 'git submodule update --init --recursive'
            }
        }

        stage('02- Build & Deploy API') {
            steps {
                script {
                    // 1. Garante que as pastas de persistência no Host do Linux existam
                    sh "mkdir -p ${DATA_PATH}/Archives"

                    // 2. Build da imagem da API
                    sh "docker build --no-cache -t ${API_NAME}:latest -f Dockerfile ."
                    
                    // 3. Parada e remoção segura do container anterior
                    sh "docker stop ${API_NAME} || true && docker rm ${API_NAME} || true"
                    
                    // 4. Execução do novo container com os volumes corretos
                    sh """
                        docker run -d \
                        --name ${API_NAME} \
                        --network proxy_network \
                        --label "com.docker.compose.project=${env.PROJECT_LABEL}" \
                        -p ${API_PORT}:${API_PORT} \
                        -v ${DATA_PATH}/Archives:/app/Archives \
                        -v ${DATA_PATH}/appsettings.Secrets.json:/app/appsettings.Secrets.json:ro \
                        --restart unless-stopped \
                        ${API_NAME}:latest
                    """
                }
            }
        }

        stage('03- Build & Deploy Web') {
            steps {
                script {
                    // Build da imagem do Frontend
                    sh "docker build --no-cache -t ${WEB_NAME}:latest -f Dockerfile.web ."
                    
                    // Parada e remoção do container anterior
                    sh "docker stop ${WEB_NAME} || true && docker rm ${WEB_NAME} || true"
                    
                    // Execução do container Web
                    sh """
                        docker run -d \
                        --name ${WEB_NAME} \
                        --network proxy_network \
                        --label "com.docker.compose.project=${env.PROJECT_LABEL}" \
                        -p ${WEB_PORT}:${WEB_PORT} \
                        --restart unless-stopped \
                        ${WEB_NAME}:latest
                    """
                }
            }
        }
    }

    post {
        always {
            cleanWs()
        }
        success {
            echo "Deploy da stack Financial Analyzer realizado com sucesso, Hal."
        }
        failure {
            echo "Falha no deploy da esteira Jenkins. Verifique os logs do Docker."
        }
    }
}