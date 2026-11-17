# syntax=docker/dockerfile:1.29.0@sha256:572cebbc2b15e6f84c077742952674889cb63cd5e9b80abdffcbe587b6f19183

# Build from the repository root: docker build -f src/Presentation/AT.Worker/Dockerfile -t app-template-worker:local .
#if (dotnet == "net11.0")
#include ../../.template.config/images/net11.0/build.Dockerfile
#else
#include ../../.template.config/images/net10.0/build.Dockerfile
#endif

ARG BUILD_CONFIGURATION=Release

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

WORKDIR /src

# Restore inputs first, so this layer stays cached until a project file or a version pin changes.
COPY ["global.json", "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "Directory.Packages.net10.0.props", "./"]

# Every project the worker references, at its real path.
COPY ["src/Domain/AT.Domain.Core/AppTemplate.Domain.Core.csproj",                                       "src/Domain/AT.Domain.Core/"]
COPY ["src/Domain/AT.Domain/AppTemplate.Domain.csproj",                                                 "src/Domain/AT.Domain/"]
COPY ["src/Application/AT.Application.Core/AppTemplate.Application.Core.csproj",                        "src/Application/AT.Application.Core/"]
#if (localAuth)
COPY ["src/Application/AT.Application.Auth/AppTemplate.Application.Auth.csproj",                        "src/Application/AT.Application.Auth/"]
#endif
COPY ["src/Application/AT.Application/AppTemplate.Application.csproj",                                  "src/Application/AT.Application/"]
COPY ["src/Presentation/AT.Presentation.Core/AppTemplate.Presentation.Core.csproj",                     "src/Presentation/AT.Presentation.Core/"]
COPY ["src/Infrastructure/AT.Infrastructure.Core/AppTemplate.Infrastructure.Core.csproj",             "src/Infrastructure/AT.Infrastructure.Core/"]
COPY ["src/Infrastructure/AT.Infrastructure.Persistence/AppTemplate.Infrastructure.Persistence.csproj", "src/Infrastructure/AT.Infrastructure.Persistence/"]
#if (localAuth)
COPY ["src/Infrastructure/AT.Infrastructure.Auth/AppTemplate.Infrastructure.Auth.csproj",       "src/Infrastructure/AT.Infrastructure.Auth/"]
#endif
COPY ["src/Infrastructure/AT.Infrastructure.Email/AppTemplate.Infrastructure.Email.csproj",             "src/Infrastructure/AT.Infrastructure.Email/"]
#if (files)
COPY ["src/Infrastructure/AT.Infrastructure.Storage/AppTemplate.Infrastructure.Storage.csproj",         "src/Infrastructure/AT.Infrastructure.Storage/"]
#endif
COPY ["src/Presentation/AT.Worker/AppTemplate.Worker.csproj",                         "src/Presentation/AT.Worker/"]

RUN dotnet restore "src/Presentation/AT.Worker/AppTemplate.Worker.csproj"

COPY . .

RUN dotnet publish "src/Presentation/AT.Worker/AppTemplate.Worker.csproj" \
        --configuration "${BUILD_CONFIGURATION}" \
        --no-restore \
        -p:UseAppHost=false \
        -o /app/publish

# The API's runtime image: the identity module needs ASP.NET Core, and the two images share its layers.
#if (dotnet == "net11.0")
#include ../../.template.config/images/net11.0/final.Dockerfile
#else
#include ../../.template.config/images/net10.0/final.Dockerfile
#endif

ARG TARGETARCH
ARG TARGETPLATFORM

LABEL org.opencontainers.image.title="AppTemplate.Worker" \
      org.opencontainers.image.description="Clean Architecture .NET template — worker" \
      org.opencontainers.image.licenses="MIT" \
      io.apptemplate.target-platform="${TARGETPLATFORM}" \
      io.apptemplate.target-arch="${TARGETARCH}"

# No port: the worker serves no HTTP.
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

WORKDIR /app
COPY --from=build /app/publish .
#if (sqlServer)

# SqlClient needs ICU, which the alpine image leaves out and turns off.
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
#endif

# The alpine image defines this user without switching to it.
USER $APP_UID

ENTRYPOINT ["dotnet", "AppTemplate.Worker.dll"]
