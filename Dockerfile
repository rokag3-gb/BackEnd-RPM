FROM golang:1.21 as chisel

RUN git clone --depth 1 -b main https://github.com/canonical/chisel /opt/chisel
WORKDIR /opt/chisel
RUN go build ./cmd/chisel

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0-noble AS build

COPY --from=chisel /opt/chisel/chisel /usr/bin/
RUN mkdir /rootfs \
    && chisel cut --release "ubuntu-24.04" --root /rootfs \
        libicu74_libs libssl3t64_libs 

WORKDIR /source

COPY . .
RUN dotnet restore "RPM.sln"
WORKDIR /source/RPM.Api
RUN dotnet publish -c Release -o /app --self-contained false

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=Asia/Seoul
USER root

COPY --from=build /rootfs /
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENTRYPOINT ["dotnet", "RPM.Api.dll"]