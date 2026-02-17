pipeline {
    agent any

    environment {
        LOG_FILE = "pipeline.log"
        GIT_REPO = 'github.com/lauristi/ServerClipboard_API_Solution.git'
        BRANCH = 'master'
        PROJECT_NAME = 'ServerClipboard_API'
        SOLUTION_PATH = 'ServerClipboard_API_Solution'
        PROJECT_PATH_ARCHIVE = 'ServerClipboard_API/ServerClipboard_API.csproj'
        PUBLISH_PATH = 'ServerClipboard_API/bin/Release/net8.0/publish'
        ARTIFACT_PATH = 'ServerClipboard_API/Artifact'
        DEPLOY_PATH = '/var/www/app/ServerClipboardProjects/ServerClipboard_API'
        DOTNET_ROOT = '/opt/dotnet'
    }

    stages {
        stage('clean') {
            steps {
                script {
                    // Remove o diretório existente se ele já existir
                    // Limpar diretório de builds antigos
                    // Limpar diretório de artefatos antigos
                    sh "rm -rf ${env.SOLUTION_PATH}"
                    sh "rm -rf ${env.PUBLISH_PATH}/*"
                    sh "rm -rf ${env.ARTIFACT_PATH}/*"
                }
            }
        }

        stage('01- Checkout') {
            steps {
                script {
                    withCredentials([string(credentialsId: 'JENKINS_TOKEN', variable: 'GITHUB_TOKEN')]) {
                        try {
                            sh """
                                git clone https://${GITHUB_TOKEN}@${GIT_REPO}
                                cd ${env.SOLUTION_PATH}
                                git checkout ${BRANCH}
                            """ 
                        } catch (Exception e) {
                            TratarErro(e)
                        }
                    }
                }
            }
        }

        stage('02- Restore Dependencies') {
            steps {
                script {
                    try {
                        sh """
                            ${env.DOTNET_ROOT}/dotnet --version 
                            ${env.DOTNET_ROOT}/dotnet restore ${env.PROJECT_NAME} 
                        """
                    } catch (Exception e) {
                        TratarErro(e)
                    }
                }
            }
        }

        stage('03- Build') {
            steps {
                script {
                    try {
                        sh """
                            ${env.DOTNET_ROOT}/dotnet build ${env.PROJECT_NAME} --no-restore --configuration Debug 
                        """
                    } catch (Exception e) {
                        TratarErro(e)
                    }
                }
            }
        }

        stage('04- Test') {
            steps {
                script {
                    try {
                        sh """
                            ${env.DOTNET_ROOT}/dotnet test ${env.PROJECT_NAME} --no-build --verbosity normal 
                        """
                    } catch (Exception e) {
                        TratarErro(e)
                    }
                }
            }
        }

        stage('05- Publish') {
            steps {
                script {
                    try {
                        sh """
                            ${env.DOTNET_ROOT}/dotnet publish ${env.PROJECT_PATH_ARCHIVE} -c Release -o ${env.PUBLISH_PATH} 
                        """
                    } catch (Exception e) {
                        TratarErro(e)
                    }
                }
            }
        }

        stage('06- Package Artifacts') {
            steps {
                script {
                    try {
                        sh """
                            mkdir -p ${env.ARTIFACT_PATH}
                            cp -r ${env.PUBLISH_PATH}/* ${env.ARTIFACT_PATH}/ 
                        """
                        archiveArtifacts artifacts: "${env.ARTIFACT_PATH}/**", allowEmptyArchive: true
                    } catch (Exception e) {
                        TratarErro(e)
                    }
                }
            }
        }

        stage('07- Deploy on server') {
            steps {
                script {
                    try {
                        sh """
                            sudo -S cp -r "${env.ARTIFACT_PATH}"/* "${env.DEPLOY_PATH}/" && echo "Copy succeeded" || echo "Copy failed"
                            sudo chown -R www-data:www-data "${env.DEPLOY_PATH}/" && echo "Chown succeeded" || echo "Chown failed"
                        """
                    } catch (Exception e) {
                        TratarErro(e)
                    }
                }
            }
        }
    }

    post {
        always {
            script {
                archiveArtifacts artifacts: "${env.LOG_FILE}", allowEmptyArchive: true
                cleanWs()
            }
        }
    }
}

def TratarErro(Exception e) {
    currentBuild.result = 'FAILURE'
    echo "--------------------------------------------------------------"
    echo "Deploy failed: ${e.message}"
    echo "--------------------------------------------------------------"
    error('Deploy failed')
    echo "--------------------------------------------------------------"
}
