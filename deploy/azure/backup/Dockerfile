# Nightly database dump job (Container Apps job caj-spicone-dbbackup-<env>).
# Azure CLI image (Azure Linux 3) plus the PostgreSQL 16 client tools; built and pushed by deploy.ps1
# as spicone-backup:latest. Context is this folder only.
FROM mcr.microsoft.com/azure-cli:2.90.0
RUN tdnf install -y postgresql && tdnf clean all
COPY backup.sh /backup.sh
RUN chmod +x /backup.sh
ENTRYPOINT ["/backup.sh"]
