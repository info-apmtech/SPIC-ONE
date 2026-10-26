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
# The base image is Ubuntu 24.04: leptonica 1.82.0 and tesseract 5.3.4, which match
# the Tesseract 5.2.0 wrapper's leptonica-1.82.0 / tesseract50 (5.x C API) bindings.
# The wrapper P/Invokes "libdl" for dlopen, but glibc >= 2.34 ships only libdl.so.2.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 libtesseract5 liblept5 \
 && rm -rf /var/lib/apt/lists/* \
 && ln -sf libdl.so.2 /usr/lib/x86_64-linux-gnu/libdl.so \
 && test -e /usr/lib/x86_64-linux-gnu/libdl.so

WORKDIR /app
# Mount points for Azure Files: uploads, logistics uploads, data-protection keys.
# Created (and owned) before the app is copied so no layer has to re-copy the app.
# The Tesseract wrapper (InteropDotNet) never searches the system library path: it only
# loads <app dir>/x64/<name> if that file exists, so the distro libraries are linked
# there under the names it asks for. test -e fails the build if a link would dangle.
RUN mkdir -p /app/Uploads /app/wwwroot/uploads /app/keys /app/x64 \
 && ln -s /usr/lib/x86_64-linux-gnu/liblept.so.5 /app/x64/libleptonica-1.82.0.so \
 && ln -s /usr/lib/x86_64-linux-gnu/libtesseract.so.5 /app/x64/libtesseract50.so \
 && test -e /app/x64/libleptonica-1.82.0.so && test -e /app/x64/libtesseract50.so \
 && chown -R app:app /app

COPY --chown=app:app --from=build /app/publish .
COPY --chown=app:app SpicAPI/tessdata ./tessdata

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SpicAPI.dll"]
