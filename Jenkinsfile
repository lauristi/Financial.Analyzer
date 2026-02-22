pipeline {
    agent any

    environment {
        API_NAME = 'financial-api'
        WEB_NAME = 'financial-web'
        PROJECT_LABEL = 'financial-analyzer'
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
                    // Build usando o Dockerfile da raiz
                    sh "docker build -t ${API_NAME}:latest -f Dockerfile ."
                    
                    // Limpeza e Execução com Label para o Portainer
                    sh "docker stop ${API_NAME} || true && docker rm ${API_NAME} || true"
                    sh """
                        docker run -d \
                        --name ${API_NAME} \
                        --label "com.docker.compose.project=${env.PROJECT_LABEL}" \
                        -p 5020:5020 \
                        --restart unless-stopped \
                        ${API_NAME}:latest
                    """
                }
            }
        }

        stage('03- Build & Deploy Web') {
            steps {
                script {
                    // Aqui usaremos o mesmo Dockerfile, mas você precisará criar um 
                    // 'Dockerfile.web' na raiz alterando apenas o WORKDIR de publicação.
                    // Por enquanto, vamos focar em subir a API primeiro.
                    echo "Aguardando Dockerfile.web para prosseguir com o deploy da Web."
                }
            }
        }
    }

    post {
        always {
            cleanWs()
        }
    }
}