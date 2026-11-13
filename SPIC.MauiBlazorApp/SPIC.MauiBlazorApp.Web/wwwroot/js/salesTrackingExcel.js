// MAUI Blazor / Blazor browser-compatible streamed XLSX download.
export async function downloadExcel(filename, dotNetStreamReference) {
  const arrayBuffer = await dotNetStreamReference.arrayBuffer();
  const blob = new Blob([arrayBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  try { link.click(); } finally { link.remove(); setTimeout(() => URL.revokeObjectURL(url), 60000); }
}
