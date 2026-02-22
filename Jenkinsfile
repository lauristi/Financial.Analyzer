pipeline {
    agent any

    environment {
        API_NAME = 'financial-api'
        WEB_NAME = 'financial-web'
        // Label para agrupar no Portainer
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
                    sh "docker build -t ${API_NAME}:latest -f Api/Server.Api/Dockerfile ."
                    
                    sh "docker stop ${API_NAME} || true && docker rm ${API_NAME} || true"
                    // Adicionada label para o Portainer reconhecer o projeto
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
                    sh "docker build -t ${WEB_NAME}:latest -f Web/Server.Web/Dockerfile ."
                    
                    sh "docker stop ${WEB_NAME} || true && docker rm ${WEB_NAME} || true"
                    // Adicionada label para o Portainer reconhecer o projeto
                    sh """
                        docker run -d \
                        --name ${WEB_NAME} \
                        --label "com.docker.compose.project=${env.PROJECT_LABEL}" \
                        -p 5023:5023 \
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
    }
}