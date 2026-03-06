pipeline {
      agent any

      stages {
          stage('Checkout') {
              steps {
                  checkout scm
              }
          }

          stage('Build Backend') {
              steps {
                  sh 'dotnet build WcagAnalyzer.sln'
              }
          }

          stage('Build Frontend') {
              tools {
                  nodejs 'NodeJS-22'
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