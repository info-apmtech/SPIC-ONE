// Metrics page line charts (Components/Metrics/MetricsChart.razor). Loaded as a module with
// import(), so neither host page needs a script tag; Chart.js itself is the vendored global.
// Every render destroys the chart already on that canvas, so a Blazor re-render never stacks
// two charts on one canvas. Also exposed as window.spic.metricsChart.

const charts = new Map();

// datasets: [{ label, data: number[], color }]
export function render(canvasId, labels, datasets) {
    try {
        dispose(canvasId);
        const canvas = document.getElementById(canvasId);
        if (!canvas || typeof Chart === 'undefined') return false;

        const chart = new Chart(canvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels || [],
                datasets: (datasets || []).map(d => ({
                    label: d.label,
                    data: d.data || [],
                    borderColor: d.color,
                    backgroundColor: d.color,
                    borderWidth: 2,
                    pointRadius: (labels || []).length > 31 ? 0 : 3,
                    pointHoverRadius: 5,
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    tension: 0.25,
                    fill: false
                }))
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        display: (datasets || []).length > 1,
                        position: 'top',
                        align: 'end',
                        labels: { boxWidth: 12, boxHeight: 2, color: '#374151', font: { size: 12 } }
                    },
                    tooltip: {
                        backgroundColor: '#ffffff',
                        titleColor: '#1f2937',
                        bodyColor: '#374151',
                        borderColor: '#e4e7ec',
                        borderWidth: 1,
                        padding: 10
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        border: { color: '#e4e7ec' },
                        ticks: { color: '#6b7684', font: { size: 11 }, maxRotation: 0, autoSkip: true, maxTicksLimit: 8 }
                    },
                    y: {
                        beginAtZero: true,
                        border: { display: false },
                        grid: { color: '#f1f4f8' },
                        ticks: { color: '#6b7684', font: { size: 11 }, precision: 0, maxTicksLimit: 5 }
                    }
                }
            }
        });

        charts.set(canvasId, chart);
        return true;
    } catch (e) {
        return false;
    }
}

export function dispose(canvasId) {
    try {
        const existing = charts.get(canvasId);
        if (existing) existing.destroy();
        charts.delete(canvasId);

        // a chart created on this canvas by an earlier module instance
        const canvas = document.getElementById(canvasId);
        const orphan = canvas && typeof Chart !== 'undefined' ? Chart.getChart(canvas) : null;
        if (orphan) orphan.destroy();
    } catch (e) { /* canvas already gone */ }
}

window.spic = window.spic || {};
window.spic.metricsChart = { render, dispose };
