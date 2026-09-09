# SPIC ONE Web (Blazor Server) — build context is the repository root:
#   docker build -f SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/Dockerfile -t spicone-web .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SPIC.Core/SPIC.Core.csproj SPIC.Core/
COPY SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/SPIC.MauiBlazorApp.Shared.csproj SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/
COPY SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/SPIC.MauiBlazorApp.Web.csproj SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/
RUN dotnet restore SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/SPIC.MauiBlazorApp.Web.csproj -r linux-x64

COPY SPIC.Core/ SPIC.Core/
COPY SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/ SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Shared/
COPY SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/ SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/
# linux-x64 publish keeps only that platform's native libraries (SkiaSharp for QuestPDF).
RUN dotnet publish SPIC.MauiBlazorApp/SPIC.MauiBlazorApp.Web/SPIC.MauiBlazorApp.Web.csproj \
    -c Release -r linux-x64 --self-contained false -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# libfontconfig1: QuestPDF / SkiaSharp used by the shared pages for PDF output.
RUN apt-get update \
 && apt-get install -y --no-install-recommends libfontconfig1 \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
# Mount point for the shared data-protection key ring (Azure Files).
RUN mkdir -p /app/keys && chown -R app:app /app

COPY --chown=app:app --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SPIC.MauiBlazorApp.Web.dll"]
