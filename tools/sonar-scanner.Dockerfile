# syntax=docker/dockerfile:1.29.0@sha256:572cebbc2b15e6f84c077742952674889cb63cd5e9b80abdffcbe587b6f19183

# Build from the repository root; keep the SDK pin in step with src/Presentation/AT.Api/Dockerfile.
#if (dotnet == "net11.0")
#include ../../.template.config/images/net11.0/scanner.Dockerfile
#else
FROM mcr.microsoft.com/dotnet/sdk:10.0.404-noble@sha256:bef136a371963aaa46ec97882845ef6f54dec15728bb4b128b1c2a400ffbc9b4
#endif

RUN apt-get update \
    && apt-get install --yes --no-install-recommends openjdk-21-jre-headless \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /repo
