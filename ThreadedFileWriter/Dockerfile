
# =========================
# Build Stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything and restore/publish
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# =========================
# Runtime Stage
# =========================
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# Ensure the target mount directory exists in the container
RUN mkdir -p /log

# Copy published files from build stage
COPY --from=build /app/publish .

# Run the application
# For interactive key press in Docker, run the container with `-it`
ENTRYPOINT ["dotnet", "ThreadedFileWriter.dll"]