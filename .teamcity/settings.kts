import jetbrains.buildServer.configs.kotlin.*
import jetbrains.buildServer.configs.kotlin.buildFeatures.dockerSupport
import jetbrains.buildServer.configs.kotlin.buildSteps.dockerCommand
import jetbrains.buildServer.configs.kotlin.buildSteps.dotnetBuild
import jetbrains.buildServer.configs.kotlin.buildSteps.nuGetInstaller
import jetbrains.buildServer.configs.kotlin.buildSteps.powerShell
import jetbrains.buildServer.configs.kotlin.buildSteps.script
import jetbrains.buildServer.configs.kotlin.projectFeatures.githubIssues
import jetbrains.buildServer.configs.kotlin.triggers.VcsTrigger
import jetbrains.buildServer.configs.kotlin.triggers.vcs

/*
The settings script is an entry point for defining a TeamCity
project hierarchy. The script should contain a single call to the
project() function with a Project instance or an init function as
an argument.

VcsRoots, BuildTypes, Templates, and subprojects can be
registered inside the project using the vcsRoot(), buildType(),
template(), and subProject() methods respectively.

To debug settings scripts in command-line, run the

    mvnDebug org.jetbrains.teamcity:teamcity-configs-maven-plugin:generate

command and attach your debugger to the port 8000.

To debug in IntelliJ Idea, open the 'Maven Projects' tool window (View
-> Tool Windows -> Maven Projects), find the generate task node
(Plugins -> teamcity-configs -> teamcity-configs:generate), the
'Debug' option is available in the context menu for the task.
*/

version = "2022.04"

project {

    buildType(Build)
    buildType(BuildPackAndPush)

    features {
        githubIssues {
            id = "PROJECT_EXT_4"
            displayName = "mtlucas/DynamoStudentManager"
            repositoryURL = "https://github.com/mtlucas/DynamoStudentManager"
        }
    }
}

object Build : BuildType({
    name = "Build Docker"

    buildNumberPattern = "%build.number%"

    params {
        param("DevelopmentVersion", "%build.number%")
        param("ReleaseVersion", "%build.number%")
    }

    vcs {
        root(DslContext.settingsRoot)
    }

    steps {
        powerShell {
            name = "Create Version Number And Artifacts"
            enabled = false
            scriptMode = script {
                content = """
                    ${'$'}Branch = "%teamcity.build.branch%";
                    
                    if(${'$'}Branch.StartsWith('releases/') -or ${'$'}Branch -eq "release")
                    {
                        ${'$'}ReleaseVersion = "%ReleaseVersion%"
                    }
                    else
                    {
                        ${'$'}ReleaseVersion = "%DevelopmentVersion%"
                    }
                    
                    Write-Host ([string]::Format("##teamcity[setParameter name='version.number' value='{0}.{1}']", ${'$'}ReleaseVersion, "%build.counter%"));
                    Write-Host ([string]::Format("##teamcity[buildNumber '{0}.{1}']", ${'$'}ReleaseVersion, "%build.counter%"));
                    
                    if(!${'$'}Branch.StartsWith("releases/") -and ${'$'}Branch -ne "master" -and ${'$'}Branch -ne "release"-and ${'$'}Branch -ne "DarwinOrgSetup")
                    {
                        Write-Host ("##teamcity[setParameter name='ArtifactPaths' value='']");
                    }
                """.trimIndent()
            }
        }
        dockerCommand {
            commandType = build {
                source = file {
                    path = "DynamoStudentManager/Dockerfile"
                }
                namesAndTags = "mtlucas/dynamostudentmanager:latest"
            }
        }
        dockerCommand {
            name = "Docker push"
            commandType = push {
                namesAndTags = "mtlucas/dynamostudentmanager:latest"
            }
            param("dockerfile.path", "DynamoStudentManager/Dockerfile")
        }
        nuGetInstaller {
            enabled = false
            toolPath = "%teamcity.tool.NuGet.CommandLine.DEFAULT%"
            projects = "DynamoStudentManager/DynamoStudentManager.sln"
        }
        dotnetBuild {
            enabled = false
            projects = "DynamoStudentManager/DynamoStudentManager.sln"
            sdk = "6"
        }
    }

    triggers {
        vcs {
        }
    }

    features {
        dockerSupport {
            cleanupPushedImages = true
            loginToRegistry = on {
                dockerRegistryId = "PROJECT_EXT_5"
            }
        }
    }
})

object BuildPackAndPush : BuildType({
    name = "Build, Pack and Push"

    artifactRules = "artifacts/nuget/*.nupkg"

    params {
        param("DevelopmentVersion", "2022.4")
        param("ReleaseVersion", "2022.3")
        param("version.number", "")
    }

    vcs {
        root(DslContext.settingsRoot)

        cleanCheckout = true
        showDependenciesChanges = true
    }

    steps {
        powerShell {
            name = "Create Version Number And Artifacts"
            scriptMode = script {
                content = """
                    ${'$'}Branch = "%teamcity.build.branch%";
                    
                    if(${'$'}Branch.StartsWith('releases/') -or ${'$'}Branch -eq "release")
                    {
                        ${'$'}ReleaseVersion = "%ReleaseVersion%"
                    }
                    else
                    {
                        ${'$'}ReleaseVersion = "%DevelopmentVersion%"
                    }
                    
                    Write-Host ([string]::Format("##teamcity[setParameter name='version.number' value='{0}.{1}']", ${'$'}ReleaseVersion, "%build.counter%"));
                    Write-Host ([string]::Format("##teamcity[buildNumber '{0}.{1}']", ${'$'}ReleaseVersion, "%build.counter%"));
                    
                    if(!${'$'}Branch.StartsWith("releases/") -and ${'$'}Branch -ne "master" -and ${'$'}Branch -ne "release"-and ${'$'}Branch -ne "DarwinOrgSetup")
                    {
                        Write-Host ("##teamcity[setParameter name='ArtifactPaths' value='']");
                    }
                """.trimIndent()
            }
        }
        script {
            name = "Build, Publish, Pack and Push Nuget package"
            workingDir = "./DynamoStudentManager"
            scriptContent = """
                :; chmod +x ./build.cmd
                :; chmod +x ./build.sh
                ./build.sh -Target Push -BuildVersion %build.number% -NugetApiKey %dev.nuget.apikey% -NugetApiUrl %system.nuget.uri%
            """.trimIndent()
        }
    }

    triggers {
        vcs {
            quietPeriodMode = VcsTrigger.QuietPeriodMode.USE_DEFAULT
            branchFilter = """
                +:*
                -:merge-requests/*
            """.trimIndent()
        }
    }
})
