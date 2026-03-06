pipeline {
      agent none

      stages {
          stage('Checkout') {
              agent any
              steps {
                  checkout scm
              }
          }

          stage('Build Backend') {
              agent {
                  docker {
                      image 'mcr.microsoft.com/dotnet/sdk:10.0'
                  }
              }
              steps {
                  sh 'dotnet build WcagAnalyzer.sln'
              }
          }

          stage('Build Frontend') {
              agent {
                  docker {
                      image 'node:22'
                  }
              }
              steps {
                  dir('src/frontend') {
                      sh 'npm install'
                      sh 'npm run build'
                  }
              }
          }
      }
  }