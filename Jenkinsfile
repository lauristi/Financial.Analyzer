pipeline {
    agent any

    environment {
        ASPNETCORE_ENVIRONMENT = 'Production'
        LOG_FILE = "pipeline.log"
        // Repositório unificado
        GIT_REPO = 'github.com/lauristi/Financial.Analyzer.git'
        BRANCH = 'master'
        
        // Definição da Solução Global
        SOLUTION_NAME = 'Financial.Analyzer'
        SOLUTION_SLN  = 'Financial.Analyzer/Financial.Analyzer.sln'
        
        // Caminhos da API
        API_NAME         = 'financial-api'
        API_PROJECT_FILE = 'Server.Solution/Server.Api/Server.Api.csproj'
        API_PUBLISH_PATH = 'Server.Solution/Server.Api/bin/Release/net8.0/publish'
        API_DEPLOY_PATH = '/var/www/app/ServerProjects/Financial.Analyzer/Server.Api'
        
        // Caminhos do Web (Blazor)
        WEB_NAME         = 'financial-web'
        WEB_PROJECT_FILE = 'Server.Web.Solution/Server.Web/Server.Web.csproj'
        WEB_PUBLISH_PATH = 'Server.Web.Solution/Server.Web/bin/Release/net8.0/publish'
        WEB_DEPLOY_PATH = '/var/www/app/ServerProjects/Financial.Analyzer/Server.Web'

        // Caminhos de Artefatos
        ARTIFACT_ROOT = 'Artifacts'
    }

    stages {
        stage('00- Clean Workspace') {
            steps {
                script {
                    // Limpeza profunda para evitar resquícios de builds anteriores
                    sh "rm -rf ${env.ARTIFACT_ROOT}"
                    cleanWs()
                }
            }
        }

        stage('01- Checkout') {
            steps {
                script {
                    // O Jenkins já lida com o checkout via SCM na interface, 
                    // mas mantendo o seu padrão de log e branch:
                    checkout scm
                }
            }
        }

        stage('02- Restore Dependencies') {
            steps {
                // Restaura a solução inteira (incluindo a Core.Infrastructure automaticamente)
                sh "dotnet restore ${env.SOLUTION_SLN}"
            }
        }

        stage('03- Build Solution') {
            steps {
                // Compilação global: garante que a infraestrutura e dependências estão íntegras
                sh "dotnet build ${env.SOLUTION_SLN} --no-restore --configuration Release"
            }
        }

        stage('04- Unit Tests') {
            steps {
                // Executa os testes do projeto de Testes unificado
                sh "dotnet test Server.Solution/Server.Tests/Server.Tests.csproj --no-build --configuration Release --verbosity normal"
            }
        }

        stage('05- Publish Apps') {
            steps {
                script {
                    // Gera os binários finais da API
                    sh "dotnet publish ${env.API_PROJECT_FILE} -c Release -o ${env.ARTIFACT_ROOT}/api"
                    // Gera os binários finais do Web (Blazor)
                    sh "dotnet publish ${env.WEB_PROJECT_FILE} -c Release -o ${env.ARTIFACT_ROOT}/web"
                }
            }
        }

        stage('06- Deploy to Production') {
            steps {
                script {
                    // Deploy API
                    sh """
                        sudo cp -r ${env.ARTIFACT_ROOT}/api/* ${env.API_DEPLOY_PATH}/
                        sudo chown -R www-data:www-data ${env.API_DEPLOY_PATH}/
                        sudo systemctl restart kestrel-${env.API_NAME}.service
                    """
                    
                    // Deploy Web
                    sh """
                        sudo cp -r ${env.ARTIFACT_ROOT}/web/* ${env.WEB_DEPLOY_PATH}/
                        sudo chown -R www-data:www-data ${env.WEB_DEPLOY_PATH}/
                        sudo systemctl restart kestrel-${env.WEB_NAME}.service
                    """
                }
            }
        }
    }

    post {
        success {
            // Arquiva os binários de ambos os projetos para rastreabilidade
            archiveArtifacts artifacts: "${env.ARTIFACT_ROOT}/**", allowEmptyArchive: true
        }
        always {
            // Garante o log para auditoria conforme seu padrão
            sh "echo 'Pipeline finalizado em: \$(date)' > ${env.LOG_FILE}"
            archiveArtifacts artifacts: "${env.LOG_FILE}", allowEmptyArchive: true
        }
    }
}