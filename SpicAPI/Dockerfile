# SPIC ONE API — build context is the repository root:
#   docker build -f SpicAPI/Dockerfile -t spicone-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the package layer is cached between builds.
COPY SPIC.Core/SPIC.Core.csproj SPIC.Core/
COPY Spic.Infrastructure/Spic.Infrastructure.csproj Spic.Infrastructure/
COPY SpicAPI/SpicAPI.csproj SpicAPI/
RUN dotnet restore SpicAPI/SpicAPI.csproj -r linux-x64

COPY SPIC.Core/ SPIC.Core/
COPY Spic.Infrastructure/ Spic.Infrastructure/
COPY SpicAPI/ SpicAPI/
# Publishing for linux-x64 keeps only that platform's native libraries (SkiaSharp,
# PDFium, Tesseract). A portable publish ships every OS/arch and is ~600 MB larger.
RUN dotnet publish SpicAPI/SpicAPI.csproj -c Release -r linux-x64 --self-contained false \
    -o /app/publish /p:UseAppHost=false


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
# Mount points for Azure Files: uploads, logistics uploads, data-protection keys.
# Created (and owned) before the app is copied so no layer has to re-copy the app.
RUN mkdir -p /app/Uploads /app/wwwroot/uploads /app/keys && chown -R app:app /app

COPY --chown=app:app --from=build /app/publish .
COPY --chown=app:app SpicAPI/tessdata ./tessdata

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SpicAPI.dll"]
