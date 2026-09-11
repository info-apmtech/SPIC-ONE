# SPIC ONE API — build context is the repository root:
#   docker build -f SpicAPI/Dockerfile -t spicone-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between builds.
COPY SPIC.Core/SPIC.Core.csproj SPIC.Core/
COPY Spic.Infrastructure/Spic.Infrastructure.csproj Spic.Infrastructure/
COPY SpicAPI/SpicAPI.csproj SpicAPI/
RUN dotnet restore SpicAPI/SpicAPI.csproj

COPY SPIC.Core/ SPIC.Core/
COPY Spic.Infrastructure/ Spic.Infrastructure/
COPY SpicAPI/ SpicAPI/
RUN dotnet publish SpicAPI/SpicAPI.csproj -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Native libraries:
#   libfontconfig1            QuestPDF / SkiaSharp text rendering
#   libtesseract5, liblept5   Tesseract OCR used by dealer document upload
# The Tesseract .NET wrapper looks for versioned file names on Linux,
# so the distro libraries are linked under the names it expects.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 libtesseract5 liblept5 \
 && rm -rf /var/lib/apt/lists/* \
 && ln -sf "$(ls /usr/lib/x86_64-linux-gnu/libtesseract.so.5* | head -n1)" /usr/lib/x86_64-linux-gnu/libtesseract50.so \
 && ln -sf "$(ls /usr/lib/x86_64-linux-gnu/liblept.so.5* | head -n1)" /usr/lib/x86_64-linux-gnu/libleptonica-1.82.0.so

WORKDIR /app
COPY --from=build /app/publish .
COPY SpicAPI/tessdata ./tessdata

# Mount points for Azure Files: uploads, logistics uploads, data-protection keys.
RUN mkdir -p /app/Uploads /app/wwwroot/uploads /app/keys \
 && chown -R app:app /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SpicAPI.dll"]
